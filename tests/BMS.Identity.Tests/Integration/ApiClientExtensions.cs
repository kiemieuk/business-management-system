using System.Net.Http.Headers;
using System.Net.Http.Json;
using BMS.Identity.Application.Contracts;

namespace BMS.Identity.Tests.Integration;

internal static class ApiClientExtensions
{
    public static async Task<LoginResponse> LoginAsync(this HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/identity/auth/login", new LoginRequest(username, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    public static async Task<HttpClient> AuthenticateAsync(this HttpClient client, string username, string password)
    {
        var login = await client.LoginAsync(username, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }
}
