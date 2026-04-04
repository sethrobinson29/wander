using System.Net.Http.Headers;
using System.Net.Http.Json;
using Wander.Client.Models;

namespace Wander.Client.Services;

public class WanderApiClient(HttpClient http, LocalStorage localStorage)
{
    // ── Auth ──────────────────────────────────────────────────────────────────

    public Task<AuthResponse?> RegisterAsync(string username, string email, string password) =>
        http.PostAsJsonAsync("auth/register",
            new { username, email, password }).ContinueWith(t => t.Result.Content.ReadFromJsonAsync<AuthResponse?>().Result);

    public Task<HttpResponseMessage> RegisterRawAsync(string username, string email, string password) =>
        http.PostAsJsonAsync("auth/register", new { username, email, password });

    public Task<HttpResponseMessage> LoginRawAsync(string email, string password) =>
        http.PostAsJsonAsync("auth/login", new { email, password });

    // ── Cards ─────────────────────────────────────────────────────────────────

    public async Task<List<CardSearchResult>> SearchCardsAsync(string q)
    {
        await AttachTokenAsync();
        return await http.GetFromJsonAsync<List<CardSearchResult>>($"cards/search?q={Uri.EscapeDataString(q)}")
               ?? [];
    }

    // ── Decks ─────────────────────────────────────────────────────────────────

    public async Task<List<DeckSummary>> GetPublicDecksAsync(Format? format = null)
    {
        var url = "decks/public";
        if (format.HasValue) url += $"?format={(int)format.Value}";
        return await http.GetFromJsonAsync<List<DeckSummary>>(url) ?? [];
    }

    public async Task<List<DeckSummary>> GetMyDecksAsync()
    {
        await AttachTokenAsync();
        return await http.GetFromJsonAsync<List<DeckSummary>>("decks/mine") ?? [];
    }

    public async Task<DeckDetail?> GetDeckAsync(Guid id)
    {
        await AttachTokenAsync();
        return await http.GetFromJsonAsync<DeckDetail>($"decks/{id}");
    }

    public async Task<HttpResponseMessage> CreateDeckAsync(CreateDeckRequest req)
    {
        await AttachTokenAsync();
        return await http.PostAsJsonAsync("decks", req);
    }

    public async Task<HttpResponseMessage> UpdateDeckAsync(Guid id, UpdateDeckRequest req)
    {
        await AttachTokenAsync();
        return await http.PutAsJsonAsync($"decks/{id}", req);
    }

    public async Task<HttpResponseMessage> SetCardsAsync(Guid id, List<DeckCardRequest> cards)
    {
        await AttachTokenAsync();
        return await http.PutAsJsonAsync($"decks/{id}/cards", cards);
    }

    public async Task<HttpResponseMessage> BulkImportAsync(Guid id, string decklist)
    {
        await AttachTokenAsync();
        return await http.PostAsJsonAsync($"decks/{id}/import", new BulkImportRequest(decklist));
    }

    public async Task<HttpResponseMessage> DeleteDeckAsync(Guid id)
    {
        await AttachTokenAsync();
        return await http.DeleteAsync($"decks/{id}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task AttachTokenAsync()
    {
        var token = await localStorage.GetAsync("accessToken");
        http.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }
}