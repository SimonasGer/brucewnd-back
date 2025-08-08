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
        // GET ALL COMICS (list view)
        app.MapGet("/comics", async (AppDbContext db) =>
        {
            var comics = await db.Comics
                .Include(c => c.Author)
                .Select(c => new ComicListDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    CoverImage = c.CoverImage
                })
                .ToListAsync();

            return Results.Ok(comics);
        });

        // GET SINGLE COMIC BY ID (detailed view)
        app.MapGet("/comics/{id}", async (int id, AppDbContext db) =>
        {
            var comic = await db.Comics
                .Include(c => c.Author)
                .Include(c => c.Chapters)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comic is null)
                return Results.NotFound();

            var dto = new ComicDto
            {
                Id = comic.Id,
                Name = comic.Name,
                Synopsis = comic.Synopsis,
                CoverImage = comic.CoverImage,
                CreatedAt = comic.CreatedAt,
                IsPublished = comic.IsPublished,
                AuthorUsername = comic.Author.Username,
                Tags = [], // tags not implemented yet
                Chapters = [.. comic.Chapters
                    .OrderBy(ch => ch.ChapterNumber)
                    .Select(ch => new ChapterSummaryDto
                    {
                        Id = ch.Id,
                        Title = ch.Title,
                        ChapterNumber = ch.ChapterNumber,
                        CreatedAt = ch.CreatedAt
                    })]
            };

            return Results.Ok(dto);
        });

        // CREATE COMIC
        app.MapPost("/comics", async (CreateComicDto dto, AppDbContext db, ClaimsPrincipal principal) =>
        {
            // Resolve author from JWT (as in previous message)
            var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim)) return Results.Unauthorized();
            if (!int.TryParse(idClaim, out var userId)) return Results.Unauthorized(); // or Guid.TryParse

            var authorExists = await db.Users.AnyAsync(u => u.Id == userId);
            if (!authorExists) return Results.Unauthorized();

            // Normalize tag names
            var incomingNames = (dto.Tags ?? Array.Empty<string>())
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.ToLowerInvariant()) // store lower; display can be prettified client-side
                .Distinct()
                .ToList();

            // Load existing tags
            var existing = await db.Tags
                .Where(t => incomingNames.Contains(t.Name.ToLower()))
                .ToListAsync();

            var existingNames = existing.Select(t => t.Name.ToLower()).ToHashSet();

            // Create missing tags
            var toCreateNames = incomingNames.Where(n => !existingNames.Contains(n)).ToList();
            var newTags = toCreateNames.Select(n => new Tag { Name = n }).ToList();
            if (newTags.Count > 0) db.Tags.AddRange(newTags);

            // Build lookup of all tags we’ll link
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

            return Results.Created($"/comics/{comic.Id}", new
            {
                comic.Id,
                comic.Name,
                comic.CoverImage,
                Tags = comic.Tags.Select(ct => ct.Tag.Name).ToList()
            });
        })
        .RequireAuthorization();



        // UPDATE COMIC
        app.MapPut("/comics/{id}", async (int id, UpdateComicDto dto, AppDbContext db) =>
        {
            var comic = await db.Comics.FindAsync(id);
            if (comic is null) return Results.NotFound();

            comic.Name = dto.Name;
            comic.Synopsis = dto.Synopsis;
            comic.CoverImage = dto.CoverImage;
            comic.IsPublished = dto.IsPublished;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // DELETE COMIC
        app.MapDelete("/comics/{id}", async (int id, AppDbContext db) =>
        {
            var comic = await db.Comics.FindAsync(id);
            if (comic is null) return Results.NotFound();

            db.Comics.Remove(comic);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
        app.MapPost("/uploads/presign", (string fileName, string contentType) =>
        {
            var bucket = "brucewnd-comics";
            var regionHost = "s3.eu-north-1.amazonaws.com";
            var key = $"covers/{Guid.NewGuid():N}{Path.GetExtension(fileName)}";

            var s3 = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.EUNorth1);
            var req = new Amazon.S3.Model.GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = key,
                Verb = Amazon.S3.HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(5),
                ContentType = contentType
            };
            var uploadUrl = s3.GetPreSignedURL(req);
            var publicUrl = $"https://{bucket}.{regionHost}/{key}";
            return Results.Ok(new { uploadUrl, publicUrl });
        });

    }
}