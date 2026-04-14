using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DiffingApi.IntegrationTests;

public sealed class DiffEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_WhenNoPayloadsExist_ReturnsNotFound()
    {
        var id = CreateUniqueId();

        var response = await _client.GetAsync($"/v1/diff/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

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
    public async Task Get_WhenOnlyLeftExists_ReturnsNotFound()
    {
        var id = CreateUniqueId();

        var putResponse = await PutLeftAsync(id, "AAAAAA==");
        var getResponse = await _client.GetAsync($"/v1/diff/{id}");

        Assert.Equal(HttpStatusCode.Created, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Get_WhenOnlyRightExists_ReturnsNotFound()
    {
        var id = CreateUniqueId();

        var putResponse = await PutRightAsync(id, "AAAAAA==");
        var getResponse = await _client.GetAsync($"/v1/diff/{id}");

        Assert.Equal(HttpStatusCode.Created, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task PutLeft_WhenDataIsNull_ReturnsBadRequest()
    {
        var id = CreateUniqueId();

        var response = await PutLeftRawAsync(id, """{"data":null}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutRight_WhenDataIsInvalidBase64_ReturnsBadRequest()
    {
        var id = CreateUniqueId();

        var response = await PutRightAsync(id, "not-base64");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    [Fact]
    public async Task Get_WhenBothSidesExistAndDifferByOneByte_ReturnsSingleDiffRange()
    {
        var id = CreateUniqueId();

        var leftResponse = await PutLeftAsync(id, "AAAA");
        var rightResponse = await PutRightAsync(id, "AAEA");
        var getResponse = await _client.GetAsync($"/v1/diff/{id}");

        Assert.Equal(HttpStatusCode.Created, leftResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, rightResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var responseJson = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            "ContentDoNotMatch",
            responseJson.RootElement.GetProperty("diffResultType").GetString());

        var diffs = responseJson.RootElement.GetProperty("diffs");

        Assert.Equal(JsonValueKind.Array, diffs.ValueKind);
        Assert.Equal(1, diffs.GetArrayLength());
        Assert.Equal(1, diffs[0].GetProperty("offset").GetInt32());
        Assert.Equal(1, diffs[0].GetProperty("length").GetInt32());
    }

    [Fact]
    public async Task Get_WhenBothSidesExistSameLengthButContentDiffers_ReturnsContentDoNotMatch()
    {
        var id = CreateUniqueId();

        var leftResponse = await PutLeftAsync(id, "AAAA");
        var rightResponse = await PutRightAsync(id, "AAAB");

        Assert.Equal(HttpStatusCode.Created, leftResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, rightResponse.StatusCode);

        var response = await _client.GetAsync($"/v1/diff/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "ContentDoNotMatch",
            json.RootElement.GetProperty("diffResultType").GetString());

        Assert.True(json.RootElement.TryGetProperty("diffs", out _));
    }

    private Task<HttpResponseMessage> PutLeftAsync(string id, string data)
    {
        return _client.PutAsJsonAsync($"/v1/diff/{id}/left", new { data });
    }

    private Task<HttpResponseMessage> PutRightAsync(string id, string data)
    {
        return _client.PutAsJsonAsync($"/v1/diff/{id}/right", new { data });
    }

    private Task<HttpResponseMessage> PutLeftRawAsync(string id, string json)
    {
        return _client.PutAsync(
            $"/v1/diff/{id}/left",
            new StringContent(json, Encoding.UTF8, "application/json"));
    }

    private static string CreateUniqueId()
    {
        return Random.Shared.Next(1, int.MaxValue).ToString();
    }
}
