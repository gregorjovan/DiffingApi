using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DiffingApi.IntegrationTests;

public sealed class DiffEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PutLeft_WithValidBase64_ReturnsCreated()
    {
        var id = CreateUniqueId();

        var response = await PutLeftAsync(id, "AAAAAA==");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PutRight_WithValidBase64_ReturnsCreated()
    {
        var id = CreateUniqueId();

        var response = await PutRightAsync(id, "AAAAAA==");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenBothSidesExistAndContentsAreIdentical_ReturnsEquals()
    {
        var id = CreateUniqueId();

        var leftResponse = await PutLeftAsync(id, "AAAAAA==");
        var rightResponse = await PutRightAsync(id, "AAAAAA==");
        var getResponse = await _client.GetAsync($"/v1/diff/{id}");

        Assert.Equal(HttpStatusCode.Created, leftResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, rightResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var responseJson = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            "Equals",
            responseJson.RootElement.GetProperty("diffResultType").GetString());
    }

    [Fact]
    public async Task Get_WhenBothSidesExistAndLengthsDiffer_ReturnsSizeDoNotMatch()
    {
        var id = CreateUniqueId();

        var leftResponse = await PutLeftAsync(id, "AAAAAA==");
        var rightResponse = await PutRightAsync(id, "AAA=");
        var getResponse = await _client.GetAsync($"/v1/diff/{id}");

        Assert.Equal(HttpStatusCode.Created, leftResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, rightResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var responseJson = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            "SizeDoNotMatch",
            responseJson.RootElement.GetProperty("diffResultType").GetString());
    }

    private Task<HttpResponseMessage> PutLeftAsync(string id, string data)
    {
        return _client.PutAsJsonAsync($"/v1/diff/{id}/left", new { data });
    }

    private Task<HttpResponseMessage> PutRightAsync(string id, string data)
    {
        return _client.PutAsJsonAsync($"/v1/diff/{id}/right", new { data });
    }

    private static string CreateUniqueId()
    {
        return Random.Shared.Next(1, int.MaxValue).ToString();
    }
}
