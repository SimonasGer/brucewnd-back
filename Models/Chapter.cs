public class Chapter
{
    public int Id { get; set; }
    public int ComicId { get; set; }
    public Comic Comic { get; set; } = null!;
    public required string Title { get; set; } = null!;
    public required int ChapterNumber { get; set; }
    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
