namespace FlashcardsApp.Models;

public class Flashcard
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime DueDate { get; set; }
    public int Interval { get; set; }
    public float Ef { get; set; }
    public int Repetitions { get; set; }
    public DateTime? LastReviewed { get; set; }
}
