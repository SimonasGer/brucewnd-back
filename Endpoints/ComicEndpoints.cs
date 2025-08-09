using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;

public static class ComicEndpoints
{
    public static void MapComicEndpoints(this IEndpointRouteBuilder app)
    {
        // Helpers
        static bool IsAdmin(ClaimsPrincipal p) => p.IsInRole("admin");
        static string? GetUserId(ClaimsPrincipal p) =>
            p.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            p.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        // ========= LIST (public: only published; admin: all) =========
        app.MapGet("/comics", async (AppDbContext db, ClaimsPrincipal user) =>
        {
            var q = db.Comics.AsQueryable();
            if (!user.IsInRole("admin")) q = q.Where(c => c.IsPublished);

            var comics = await q
                .Select(c => new ComicListDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    CoverImage = c.CoverImage
                })
                .ToListAsync();

            return Results.Ok(comics);
        });

        app.MapGet("/comics/{name}", async (string name, AppDbContext db, ClaimsPrincipal user) =>
        {
            var isAdmin = user.IsInRole("admin");

            var comic = await db.Comics
                .Include(c => c.Author)
                .Include(c => c.Chapters)
                .FirstOrDefaultAsync(c => c.Name == name);

            if (comic is null) return Results.NotFound();

            // Hide unpublished comics from public
            if (!comic.IsPublished && !isAdmin) return Results.Forbid();

            // Public sees only published chapters
            var chQuery = comic.Chapters.AsQueryable();
            if (!isAdmin) chQuery = chQuery.Where(ch => ch.IsPublished);

            var dto = new ComicDto
            {
                Id = comic.Id,
                Name = comic.Name,
                Synopsis = comic.Synopsis,
                CoverImage = comic.CoverImage,
                CreatedAt = comic.CreatedAt,
                IsPublished = comic.IsPublished,
                AuthorUsername = comic.Author.Username,
                Tags = [], // fill if you wire tags
                Chapters = chQuery
                    .OrderBy(ch => ch.ChapterNumber)
                    .Select(ch => new ChapterSummaryDto
                    {
                        Id = ch.Id,
                        Title = ch.Title,
                        Number = ch.ChapterNumber,      // <-- mapped to Number
                        IsPublished = ch.IsPublished,
                        CreatedAt = ch.CreatedAt
                    })
                    .ToList()
            };

            return Results.Ok(dto);
        });


        // ========= CREATE (auth, author=token owner) =========
        app.MapPost("/comics", async (CreateComicDto dto, AppDbContext db, ClaimsPrincipal principal) =>
        {
            var idClaim = GetUserId(principal);
            if (string.IsNullOrWhiteSpace(idClaim)) return Results.Unauthorized();
            if (!int.TryParse(idClaim, out var userId)) return Results.Unauthorized();

            var authorExists = await db.Users.AnyAsync(u => u.Id == userId);
            if (!authorExists) return Results.Unauthorized();

            var incomingNames = (dto.Tags ?? Array.Empty<string>())
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.ToLowerInvariant())
                .Distinct()
                .ToList();

            var existing = await db.Tags
                .Where(t => incomingNames.Contains(t.Name.ToLower()))
                .ToListAsync();

            var existingNames = existing.Select(t => t.Name.ToLower()).ToHashSet();
            var toCreateNames = incomingNames.Where(n => !existingNames.Contains(n)).ToList();
            var newTags = toCreateNames.Select(n => new Tag { Name = n }).ToList();
            if (newTags.Count > 0) db.Tags.AddRange(newTags);

            var allTags = existing.Concat(newTags).ToDictionary(t => t.Name.ToLower(), t => t);

            var comic = new Comic
            {
                Name = dto.Name,
                Synopsis = dto.Synopsis ?? "",
                CoverImage = dto.CoverImage,
                IsPublished = dto.IsPublished,
                CreatedAt = DateTime.UtcNow,
                AuthorId = userId,
                Tags = incomingNames.Select(n => new ComicTag { Tag = allTags[n] }).ToList()
            };

            db.Comics.Add(comic);
            await db.SaveChangesAsync();

            return Results.Created($"/comics/{comic.Name}", new
            {
                comic.Id,
                comic.Name,
                comic.CoverImage,
                Tags = comic.Tags.Select(ct => ct.Tag.Name).ToList()
            });
        })
        .RequireAuthorization(); // any authenticated user

        // ========= UPDATE (admin only) =========
        app.MapPut("/comics/{id}", async (int id, UpdateComicDto dto, AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();

            var comic = await db.Comics.FindAsync(id);
            if (comic is null) return Results.NotFound();

            comic.Name = dto.Name;
            comic.Synopsis = dto.Synopsis;
            comic.CoverImage = dto.CoverImage;
            comic.IsPublished = dto.IsPublished;

            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        // ========= DELETE BY NAME (admin only; matches front) =========
        app.MapDelete("/comics/{name}", async (string name, AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();

            var comic = await db.Comics.FirstOrDefaultAsync(c => c.Name == name);
            if (comic is null) return Results.NotFound();

            db.Comics.Remove(comic);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        // ========= PUBLISH TOGGLE (admin only; matches front) =========
        app.MapPatch("/comics/{name}/publish", async (
            string name,
            [FromBody] PublishDto body,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();

            var comic = await db.Comics.FirstOrDefaultAsync(c => c.Name == name);
            if (comic is null) return Results.NotFound();

            comic.IsPublished = body.Publish;
            await db.SaveChangesAsync();
            return Results.Ok(new { comic.Name, comic.IsPublished });
        }).RequireAuthorization();

        // ========= CHAPTER CREATE (admin only; matches front) =========
        app.MapPost("/comics/{name}/chapters", async (
            string name,
            [FromBody] CreateChapterDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();

            var comic = await db.Comics
                .Include(c => c.Chapters)
                .FirstOrDefaultAsync(c => c.Name == name);
            if (comic is null) return Results.NotFound();

            // Determine next ChapterNumber if not provided
            int nextNumber = (comic.Chapters.Count == 0)
                ? 1
                : comic.Chapters.Max(ch => ch.ChapterNumber) + 1;

            var ch = new Chapter
            {
                ComicId = comic.Id,
                Title = dto.Title,
                ChapterNumber = dto.Number ?? nextNumber,
                CreatedAt = DateTime.UtcNow
            };

            db.Chapters.Add(ch);
            await db.SaveChangesAsync();

            return Results.Ok(new ChapterSummaryDto
            {
                Id = ch.Id,
                Title = ch.Title,
                Number = ch.ChapterNumber,
                CreatedAt = ch.CreatedAt
            });
        }).RequireAuthorization();

        // ========= CHAPTER MOVE (admin only; swap numbers) =========
        app.MapPatch("/comics/{name}/chapters/{chapterId:int}/move", async (
            string name,
            int chapterId,
            [FromBody] MoveChapterDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();

            var comic = await db.Comics
                .Include(c => c.Chapters)
                .FirstOrDefaultAsync(c => c.Name == name);
            if (comic is null) return Results.NotFound();

            var ordered = comic.Chapters.OrderBy(ch => ch.ChapterNumber).ToList();
            var idx = ordered.FindIndex(ch => ch.Id == chapterId);
            if (idx < 0) return Results.NotFound();

            int targetIdx = dto.Direction?.ToLowerInvariant() switch
            {
                "up" => idx - 1,
                "down" => idx + 1,
                _ => idx
            };
            if (targetIdx < 0 || targetIdx >= ordered.Count) return Results.BadRequest();

            // swap ChapterNumber
            var a = ordered[idx];
            var b = ordered[targetIdx];
            (a.ChapterNumber, b.ChapterNumber) = (b.ChapterNumber, a.ChapterNumber);

            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        // ========= CHAPTER DELETE (admin only) =========
        app.MapDelete("/comics/{name}/chapters/{chapterId:int}", async (
            string name,
            int chapterId,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();

            var comic = await db.Comics.FirstOrDefaultAsync(c => c.Name == name);
            if (comic is null) return Results.NotFound();

            var chapter = await db.Chapters.FirstOrDefaultAsync(ch => ch.Id == chapterId && ch.ComicId == comic.Id);
            if (chapter is null) return Results.NotFound();

            db.Chapters.Remove(chapter);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        // ========= (Optional) legacy delete by id (admin only) =========
        app.MapDelete("/comics/{id:int}", async (int id, AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();

            var comic = await db.Comics.FindAsync(id);
            if (comic is null) return Results.NotFound();

            db.Comics.Remove(comic);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        // ========= S3 presign (auth or admin? choose; here: admin only) =========
        _ = app.MapPost("/uploads/presign", (
            [FromBody] PresignDto reqBody,
            ClaimsPrincipal user) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();

            var bucket = "brucewnd-comics";
            var regionHost = "s3.eu-north-1.amazonaws.com";
            var key = $"covers/{Guid.NewGuid():N}{Path.GetExtension(reqBody.FileName)}";

            var s3 = new AmazonS3Client(Amazon.RegionEndpoint.EUNorth1);
            var req = new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(5),
                ContentType = reqBody.ContentType
            };
            var uploadUrl = s3.GetPreSignedURL(req);
            var publicUrl = $"https://{bucket}.{regionHost}/{key}";
            return Results.Ok(new { uploadUrl, publicUrl });
        }).RequireAuthorization();
    }
}

// ===== DTOs used by new endpoints =====
public sealed class PublishDto
{
    public bool Publish { get; set; }
}
public sealed class CreateChapterDto
{
    public string Title { get; set; } = "";
    public int? Number { get; set; }
}
public sealed class MoveChapterDto
{
    public string? Direction { get; set; } // "up" | "down"
}
public sealed class PresignDto
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "image/jpeg";
}