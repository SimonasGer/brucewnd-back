public class UpdateComicDto
{
    public string Name { get; set; } = null!;
    public string Synopsis { get; set; } = "";
    public string? CoverImage { get; set; }
    public bool IsPublished { get; set; }
    public List<string>? Tags { get; set; }
}
