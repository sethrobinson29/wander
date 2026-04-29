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

    // ── Users ──────────────────────────────────────────────────────────────────

    public async Task<MyProfileResponse?> GetMyProfileAsync()
    {
        await AttachTokenAsync();
        return await http.GetFromJsonAsync<MyProfileResponse>("users/me");
    }

    public async Task<HttpResponseMessage> UpdateProfileAsync(UpdateProfileRequest req)
    {
        await AttachTokenAsync();
        return await http.PutAsJsonAsync("users/me/profile", req);
    }

    public async Task<HttpResponseMessage> UpdateSecurityAsync(UpdateSecurityRequest req)
    {
        await AttachTokenAsync();
        return await http.PutAsJsonAsync("users/me/security", req);
    }

    public async Task<HttpResponseMessage> UpdatePrivacyAsync(UpdatePrivacyRequest req)
    {
        await AttachTokenAsync();
        return await http.PutAsJsonAsync("users/me/privacy", req);
    }

    public async Task<List<UserSearchResult>> SearchUsersAsync(string q)
    {
        await AttachTokenAsync();
        return await http.GetFromJsonAsync<List<UserSearchResult>>(
            $"users/search?q={Uri.EscapeDataString(q)}") ?? [];
    }

    public async Task<PublicProfileResponse?> GetUserProfileAsync(string username)
    {
        await AttachTokenAsync();
        return await http.GetFromJsonAsync<PublicProfileResponse>($"users/{Uri.EscapeDataString(username)}");
    }

    public async Task<HttpResponseMessage> FollowUserAsync(string username)
    {
        await AttachTokenAsync();
        return await http.PostAsync($"users/{Uri.EscapeDataString(username)}/follow", null);
    }

    public async Task<HttpResponseMessage> UnfollowUserAsync(string username)
    {
        await AttachTokenAsync();
        return await http.DeleteAsync($"users/{Uri.EscapeDataString(username)}/follow");
    }

    public async Task<ActivityPageResponse?> GetUserActivityAsync(string username, int page = 1)
    {
        await AttachTokenAsync();
        return await http.GetFromJsonAsync<ActivityPageResponse>(
            $"users/{Uri.EscapeDataString(username)}/activity?page={page}&pageSize=20");
    }

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

    public async Task<HttpResponseMessage> LikeDeckAsync(Guid id)
    {
        await AttachTokenAsync();
        return await http.PostAsync($"decks/{id}/like", null);
    }

    public async Task<HttpResponseMessage> UnlikeDeckAsync(Guid id)
    {
        await AttachTokenAsync();
        return await http.DeleteAsync($"decks/{id}/like");
    }

    public async Task<List<DeckSummary>> SearchDecksAsync(string q, string type = "name", Format? format = null)
    {
        await AttachTokenAsync();
        var url = $"decks/search?q={Uri.EscapeDataString(q)}&type={type}";
        if (format.HasValue) url += $"&format={(int)format.Value}";
        return await http.GetFromJsonAsync<List<DeckSummary>>(url) ?? [];
    }

    public async Task<List<CommentResponse>> GetCommentsAsync(Guid deckId)
    {
        await AttachTokenAsync();
        return await http.GetFromJsonAsync<List<CommentResponse>>($"decks/{deckId}/comments") ?? [];
    }

    public async Task<HttpResponseMessage> PostCommentAsync(Guid deckId, string body)
    {
        await AttachTokenAsync();
        return await http.PostAsJsonAsync($"decks/{deckId}/comments", new PostCommentRequest(body));
    }

    public async Task<HttpResponseMessage> PostReplyAsync(Guid parentCommentId, string body)
    {
        await AttachTokenAsync();
        return await http.PostAsJsonAsync($"comments/{parentCommentId}/replies",
            new PostCommentRequest(body));
    }

    public async Task<HttpResponseMessage> DeleteCommentAsync(Guid commentId)
    {
        await AttachTokenAsync();
        return await http.DeleteAsync($"comments/{commentId}");
    }

    public async Task<HttpResponseMessage> SetDeckCoverAsync(Guid id, Guid? printingId)
    {
        await AttachTokenAsync();
        return await http.PatchAsJsonAsync($"decks/{id}/cover", new { printingId });
    }

    // ── Notifications ───────────────────────────────────────────────────────────────
    public async Task<NotificationListResponse?> GetNotificationsAsync(int page = 1)
    {
        await AttachTokenAsync();
        return await http.GetFromJsonAsync<NotificationListResponse>(
            $"notifications?page={page}&pageSize=20");
    }

    public async Task<HttpResponseMessage> MarkNotificationReadAsync(Guid id)
    {
        await AttachTokenAsync();
        return await http.PutAsync($"notifications/{id}/read", null);
    }

    public async Task<HttpResponseMessage> MarkAllNotificationsReadAsync()
    {
        await AttachTokenAsync();
        return await http.PutAsync("notifications/read-all", null);
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