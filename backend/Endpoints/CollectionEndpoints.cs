using ClosedXML.Excel;
using FlashcardsApi.Data;
using FlashcardsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlashcardsApi.Endpoints;

public static class CollectionEndpoints
{
    public static void Map(WebApplication app)
    {
        // Get all collections, sorted alphabetically
        app.MapGet("/collections", async (FlashcardsDbContext db) =>
            await db.Collections.OrderBy(collection => collection.Name).ToListAsync());

        // Get a single collection by ID
        app.MapGet("/collections/{id}", async (int id, FlashcardsDbContext db) =>
            await db.Collections.FindAsync(id) is Collection collection ? Results.Ok(collection) : Results.NotFound());

        // Get review stats for a collection (due today, total cards, new cards, mastered, etc.)
        app.MapGet("/collections/{id}/stats", async (int id, FlashcardsDbContext db) =>
        {
            if (!await db.Collections.AnyAsync(collection => collection.Id == id))
                return Results.NotFound();

            var cards = await db.Flashcards
                .Where(flashcard => flashcard.CollectionId == id)
                .Select(flashcard => new
                {
                    flashcard.Repetitions,
                    flashcard.DueDate,
                    flashcard.Interval,
                    flashcard.LastReviewed
                })
                .ToListAsync();

            var now = DateTime.UtcNow;
            return Results.Ok(new
            {
                totalCards = cards.Count,
                dueToday = cards.Count(card => card.DueDate <= now && card.Repetitions > 0),
                totalReviews = cards.Sum(card => card.Repetitions),
                newCards = cards.Count(card => card.Repetitions == 0),
                mastered = cards.Count(card => card.Interval > 30),
                lastReviewed = cards.Max(card => card.LastReviewed)
            });
        });

        // Create a new collection
        app.MapPost("/collections", async (Collection collection, FlashcardsDbContext db) =>
        {
            db.Collections.Add(collection);
            await db.SaveChangesAsync();
            return Results.Created($"/collections/{collection.Id}", collection);
        });

        // Delete a collection and all its flashcards (cascade)
        app.MapDelete("/collections/{id}", async (int id, FlashcardsDbContext db) =>
        {
            var collection = await db.Collections.FindAsync(id);
            if (collection is null) return Results.NotFound();
            db.Collections.Remove(collection);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Import flashcards from an .xlsx file (multipart form); expected columns: front, back, notes (optional)
        app.MapPost("/collections/{id}/import", async (int id, HttpRequest request, FlashcardsDbContext db) =>
        {
            if (!await db.Collections.AnyAsync(collection => collection.Id == id))
                return Results.NotFound();

            if (!request.HasFormContentType || request.Form.Files.Count == 0)
                return Results.BadRequest("Upload an .xlsx file as multipart/form-data.");

            var file = request.Form.Files[0];
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

            // Read header row to find column indices
            var headerRow = sheet.Row(1);
            int? frontCol = null, backCol = null, notesCol = null;
            foreach (var cell in headerRow.CellsUsed())
            {
                var header = cell.GetString().Trim().ToLowerInvariant();
                switch (header)
                {
                    case "front": frontCol = cell.Address.ColumnNumber; break;
                    case "back": backCol = cell.Address.ColumnNumber; break;
                    case "notes": notesCol = cell.Address.ColumnNumber; break;
                }
            }

            if (frontCol is null || backCol is null)
                return Results.BadRequest("Spreadsheet must have 'front' and 'back' columns.");

            var cards = new List<Flashcard>();
            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                var front = row.Cell(frontCol.Value).GetString().Trim();
                var back = row.Cell(backCol.Value).GetString().Trim();
                if (string.IsNullOrEmpty(front) || string.IsNullOrEmpty(back)) continue;

                cards.Add(new Flashcard
                {
                    CollectionId = id,
                    Front = front,
                    Back = back,
                    Notes = notesCol.HasValue ? row.Cell(notesCol.Value).GetString().Trim() : null
                });
            }

            db.Flashcards.AddRange(cards);
            await db.SaveChangesAsync();
            return Results.Ok(new { imported = cards.Count });
        });
    }
}
