using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using FlashcardsApp;
using FlashcardsApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Logging.SetMinimumLevel(LogLevel.Warning);

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";
var clientId   = builder.Configuration["AzureAd:ClientId"] ?? "";

// Scope defined under "Expose an API" in the Entra ID app registration.
// The backend validates the JWT aud claim against this same App ID URI.
var apiScope = $"api://{clientId}/access_as_user";

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
});

builder.Services.AddHttpClient<FlashcardApiService>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler(sp =>
        sp.GetRequiredService<AuthorizationMessageHandler>()
          .ConfigureHandler(authorizedUrls: [apiBaseUrl], scopes: [apiScope]));

await builder.Build().RunAsync();
