public class ComicDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Synopsis { get; set; }
    public string? CoverImage { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public string AuthorUsername { get; set; } = null!;
    public List<string> Tags { get; set; } = [];
    public List<ChapterSummaryDto> Chapters { get; set; } = [];
}
