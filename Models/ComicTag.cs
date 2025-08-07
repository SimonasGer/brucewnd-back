public class ComicTag
{
    public int Id { get; set; }
    public required string Name { get; set; } = null!;

    public List<Comic> Comics { get; set; } = [];
}
