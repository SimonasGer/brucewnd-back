public class Comic
{
    public int Id { get; set; }
    public required string Name { get; set; } = null!;
    public string Synopsis { get; set; } = "";
    public List<Chapter> Chapters { get; set; } = [];
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public string? CoverImage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPublished { get; set; } = false;
    public List<ComicTag> Tags { get; set; } = [];
}
