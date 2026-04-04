using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Wander.Client;
using Wander.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<LocalStorage>();

// API client — base URL from config
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5156";
builder.Services.AddScoped(sp =>
{
    var localStorage = sp.GetRequiredService<LocalStorage>();
    return new WanderApiClient(new HttpClient { BaseAddress = new Uri(apiBase) }, localStorage);
});

builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();

await builder.Build().RunAsync();