public class CreateComicDto
{
    public string Name { get; set; } = null!;
    public string Synopsis { get; set; } = "";
    public string? CoverImage { get; set; }
    public bool IsPublished { get; set; } = false;
    public IEnumerable<string> Tags { get; set; } = [];
}
