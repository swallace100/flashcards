using FlashcardsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlashcardsApi.Data;

public class FlashcardsDbContext(DbContextOptions<FlashcardsDbContext> options) : DbContext(options)
{
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<Flashcard> Flashcards => Set<Flashcard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Collection>(e =>
        {
            e.ToTable("collections");
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.Name).HasColumnName("name").HasMaxLength(100);
            e.Property(c => c.Description).HasColumnName("description").HasMaxLength(255);
            e.Property(c => c.CreatedDate).HasColumnName("created_date");
        });

        modelBuilder.Entity<Flashcard>(e =>
        {
            e.ToTable("flashcards");
            e.Property(f => f.Id).HasColumnName("id");
            e.Property(f => f.CollectionId).HasColumnName("collection_id");
            e.Property(f => f.Front).HasColumnName("front").HasMaxLength(2000);
            e.Property(f => f.Back).HasColumnName("back").HasMaxLength(2000);
            e.Property(f => f.Notes).HasColumnName("notes").HasMaxLength(2000);
            e.Property(f => f.CreatedDate).HasColumnName("created_date");
            e.Property(f => f.DueDate).HasColumnName("due_date");
            e.Property(f => f.Interval).HasColumnName("interval");
            e.Property(f => f.Ef).HasColumnName("ef");
            e.Property(f => f.Repetitions).HasColumnName("repetitions");
            e.Property(f => f.LastReviewed).HasColumnName("last_reviewed");
        });
    }
}
