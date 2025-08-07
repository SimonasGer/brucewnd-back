using Microsoft.EntityFrameworkCore;

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
        app.MapPost("/comics", async (CreateComicDto dto, AppDbContext db, HttpContext http) =>
        {
            // TEMP: author is hardcoded. Replace with JWT auth later.
            var author = await db.Users.FirstOrDefaultAsync(); // replace with actual user
            if (author == null) return Results.Unauthorized();

            var comic = new Comic
            {
                Name = dto.Name,
                Synopsis = dto.Synopsis,
                CoverImage = dto.CoverImage,
                IsPublished = dto.IsPublished,
                CreatedAt = DateTime.UtcNow,
                Author = author,
            };

            db.Comics.Add(comic);
            await db.SaveChangesAsync();

            return Results.Created($"/comics/{comic.Id}", new ComicListDto
            {
                Id = comic.Id,
                Name = comic.Name,
                CoverImage = comic.CoverImage
            });
        });

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
    }
}
