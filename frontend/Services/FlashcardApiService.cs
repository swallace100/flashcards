using System.Net.Http.Json;
using FlashcardsApp.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace FlashcardsApp.Services;

public class FlashcardApiService(HttpClient http)
{
    public Task<List<Collection>?> GetCollectionsAsync() =>
        http.GetFromJsonAsync<List<Collection>>("collections");

    public Task<Collection?> GetCollectionAsync(int id) =>
        http.GetFromJsonAsync<Collection>($"collections/{id}");

    public Task<CollectionStats?> GetStatsAsync(int collectionId) =>
        http.GetFromJsonAsync<CollectionStats>($"collections/{collectionId}/stats");

    public Task<Flashcard?> GetNextCardAsync(int collectionId) =>
        http.GetFromJsonAsync<Flashcard?>($"flashcards/next?collectionId={collectionId}");

    public Task<HttpResponseMessage> ReviewCardAsync(int cardId, int difficultyId) =>
        http.PutAsJsonAsync($"flashcards/{cardId}", new { difficultyId });

    public Task<HttpResponseMessage> EditCardAsync(int cardId, string front, string back, string? notes) =>
        http.PutAsJsonAsync($"flashcards/{cardId}/edit", new { front, back, notes });

    public Task<HttpResponseMessage> CreateCollectionAsync(string name, string? description) =>
        http.PostAsJsonAsync("collections", new { name, description });

    public Task<HttpResponseMessage> DeleteCollectionAsync(int id) =>
        http.DeleteAsync($"collections/{id}");

    public async Task<(int imported, string? error)> ImportCardsAsync(int collectionId, IBrowserFile file)
    {
        using var content = new MultipartFormDataContent();
        var stream = file.OpenReadStream(maxAllowedSize: 10_000_000);
        content.Add(new StreamContent(stream), "file", file.Name);

        var response = await http.PostAsync($"collections/{collectionId}/import", content);
        if (!response.IsSuccessStatusCode)
            return (0, $"Upload failed ({(int)response.StatusCode}).");

        var result = await response.Content.ReadFromJsonAsync<ImportResult>();
        return (result?.Imported ?? 0, null);
    }

    private record ImportResult(int Imported);
}
