public class Tag
{
    public int Id { get; set; }
    public required string Name { get; set; } = null!;
    public List<ComicTag> Comics { get; set; } = [];
}