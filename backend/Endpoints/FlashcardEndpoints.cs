using FlashcardsApi.Data;
using FlashcardsApi.Models;
using FlashcardsApi.Services;
using Microsoft.EntityFrameworkCore;

namespace FlashcardsApi.Endpoints;

public static class FlashcardEndpoints
{
    public static void Map(WebApplication app)
    {
        // Next card for review: due cards first (ordered by due date), then new cards (ordered by id)
        app.MapGet("/flashcards/next", async (int collectionId, FlashcardsDbContext db) =>
        {
            var now = DateTime.UtcNow;

            var card = await db.Flashcards
                           .Where(flashcard => flashcard.CollectionId == collectionId
                                    && flashcard.DueDate <= now
                                    && flashcard.Repetitions > 0)
                           .OrderBy(flashcard => flashcard.DueDate)
                           .FirstOrDefaultAsync()
                       ?? await db.Flashcards
                           .Where(flashcard => flashcard.CollectionId == collectionId
                                    && flashcard.Repetitions == 0
                                    && flashcard.DueDate <= now)
                           .OrderBy(flashcard => flashcard.Id)
                           .FirstOrDefaultAsync();

            return card is null ? Results.NoContent() : Results.Ok(card);
        });

        // Get a single flashcard by ID
        app.MapGet("/flashcards/{id}", async (int id, FlashcardsDbContext db) =>
            await db.Flashcards.FindAsync(id) is Flashcard card ? Results.Ok(card) : Results.NotFound());

        // Create a new flashcard
        app.MapPost("/flashcards", async (Flashcard flashcard, FlashcardsDbContext db) =>
        {
            db.Flashcards.Add(flashcard);
            await db.SaveChangesAsync();
            return Results.Created($"/flashcards/{flashcard.Id}", flashcard);
        });

        // Submit a review rating for a card; recalculates interval, EF, and next due date via SM-2
        app.MapPut("/flashcards/{id}", async (int id, ReviewRequest req, FlashcardsDbContext db) =>
        {
            var card = await db.Flashcards.FindAsync(id);
            if (card is null) return Results.NotFound();

            var (interval, ef, repetitions, dueDate) =
                Sm2Service.Calculate(req.DifficultyId, card.Interval, card.Ef, card.Repetitions);

            card.Interval = interval;
            card.Ef = ef;
            card.Repetitions = repetitions;
            card.DueDate = dueDate;
            card.LastReviewed = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(card);
        });

        // Edit card content
        app.MapPut("/flashcards/{id}/edit", async (int id, EditRequest req, FlashcardsDbContext db) =>
        {
            var card = await db.Flashcards.FindAsync(id);
            if (card is null) return Results.NotFound();

            card.Front = req.Front;
            card.Back = req.Back;
            card.Notes = req.Notes;

            await db.SaveChangesAsync();
            return Results.Ok(card);
        });

        // Delete a flashcard
        app.MapDelete("/flashcards/{id}", async (int id, FlashcardsDbContext db) =>
        {
            var card = await db.Flashcards.FindAsync(id);
            if (card is null) return Results.NotFound();
            db.Flashcards.Remove(card);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}

record ReviewRequest(int DifficultyId);
record EditRequest(string Front, string Back, string? Notes);
