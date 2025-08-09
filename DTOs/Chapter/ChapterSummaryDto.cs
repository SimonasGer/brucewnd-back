public sealed class ChapterSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int Number { get; set; }          // <-- matches frontend
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
}
