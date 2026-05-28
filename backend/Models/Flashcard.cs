namespace FlashcardsApi.Models;

public class Flashcard
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // SM-2 spaced repetition fields
    public DateTime DueDate { get; set; } = DateTime.UtcNow;
    public int Interval { get; set; } = 1;
    public float Ef { get; set; } = 2.5f;
    public int Repetitions { get; set; } = 0;
    public DateTime? LastReviewed { get; set; }

    public Collection Collection { get; set; } = null!;
}
