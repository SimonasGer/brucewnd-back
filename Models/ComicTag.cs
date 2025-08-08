public class ComicTag
{
    public int ComicId { get; set; }
    public Comic Comic { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
