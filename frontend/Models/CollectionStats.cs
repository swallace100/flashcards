namespace FlashcardsApp.Models;

public class CollectionStats
{
    public int TotalCards { get; set; }
    public int DueToday { get; set; }
    public int TotalReviews { get; set; }
    public int NewCards { get; set; }
    public int Mastered { get; set; }
    public DateTime? LastReviewed { get; set; }
}
