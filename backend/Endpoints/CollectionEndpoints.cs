using ClosedXML.Excel;
using FlashcardsApi.Data;
using FlashcardsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlashcardsApi.Endpoints;

public static class CollectionEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/collections", async (FlashcardsDbContext db) =>
            await db.Collections.OrderBy(c => c.Name).ToListAsync());

        app.MapGet("/collections/{id}", async (int id, FlashcardsDbContext db) =>
            await db.Collections.FindAsync(id) is Collection c ? Results.Ok(c) : Results.NotFound());

        app.MapGet("/collections/{id}/stats", async (int id, FlashcardsDbContext db) =>
        {
            if (!await db.Collections.AnyAsync(c => c.Id == id))
                return Results.NotFound();

            var cards = await db.Flashcards
                .Where(f => f.CollectionId == id)
                .Select(f => new { f.Repetitions, f.DueDate, f.Interval, f.LastReviewed })
                .ToListAsync();

            var now = DateTime.UtcNow;
            return Results.Ok(new
            {
                totalCards   = cards.Count,
                dueToday     = cards.Count(c => c.DueDate <= now),
                totalReviews = cards.Sum(c => c.Repetitions),
                newCards     = cards.Count(c => c.Repetitions == 0),
                mastered     = cards.Count(c => c.Interval >= 21),
                lastReviewed = cards.Max(c => c.LastReviewed)
            });
        });

        app.MapPost("/collections", async (Collection collection, FlashcardsDbContext db) =>
        {
            db.Collections.Add(collection);
            await db.SaveChangesAsync();
            return Results.Created($"/collections/{collection.Id}", collection);
        });

        app.MapDelete("/collections/{id}", async (int id, FlashcardsDbContext db) =>
        {
            var collection = await db.Collections.FindAsync(id);
            if (collection is null) return Results.NotFound();
            db.Collections.Remove(collection);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // POST /collections/{id}/import  — multipart form with an .xlsx file
        // Expected columns (case-insensitive): front, back, notes
        app.MapPost("/collections/{id}/import", async (int id, HttpRequest request, FlashcardsDbContext db) =>
        {
            if (!await db.Collections.AnyAsync(c => c.Id == id))
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
                    case "back":  backCol  = cell.Address.ColumnNumber; break;
                    case "notes": notesCol = cell.Address.ColumnNumber; break;
                }
            }

            if (frontCol is null || backCol is null)
                return Results.BadRequest("Spreadsheet must have 'front' and 'back' columns.");

            var cards = new List<Flashcard>();
            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                var front = row.Cell(frontCol.Value).GetString().Trim();
                var back  = row.Cell(backCol.Value).GetString().Trim();
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
