using System.Text.Json;

namespace EFCore.Performance.Tests.Support;

internal static class ApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<T> GetRequiredAsync<T>(HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.IsSuccessStatusCode,
            $"{(int)response.StatusCode} {url}{Environment.NewLine}{body}");

        var value = JsonSerializer.Deserialize<T>(body, Json);
        Assert.NotNull(value);
        return value;
    }
}
