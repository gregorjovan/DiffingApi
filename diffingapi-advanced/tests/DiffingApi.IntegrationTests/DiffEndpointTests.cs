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
    public async Task PutRight_WhenDataIsNull_ReturnsBadRequest()
    {
        var id = CreateUniqueId();

        var response = await PutRightRawAsync(id, """{"data":null}""");

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

    [Fact]
    public async Task GetStatus_WhenOnlyLeftExists_ReturnsNotFound()
    {
        var id = CreateUniqueId();

        var putResponse = await PutLeftAsync(id, "AAAAAA==");
        var statusResponse = await _client.GetAsync($"/v1/diff/{id}/status");

        Assert.Equal(HttpStatusCode.Created, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, statusResponse.StatusCode);
    }

    [Fact]
    public async Task GetStatus_WhenBothSidesExist_EventuallyReturnsCompletedResult()
    {
        var id = CreateUniqueId();

        var leftResponse = await PutLeftAsync(id, "AAAA");
        var rightResponse = await PutRightAsync(id, "AAEA");

        Assert.Equal(HttpStatusCode.Created, leftResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, rightResponse.StatusCode);

        using var json = await ReadCompletedStatusAsync(id);

        Assert.Equal("Completed", json.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            "ContentDoNotMatch",
            json.RootElement.GetProperty("diffResultType").GetString());

        var diffs = json.RootElement.GetProperty("diffs");

        Assert.Equal(1, diffs.GetArrayLength());
        Assert.Equal(1, diffs[0].GetProperty("offset").GetInt32());
        Assert.Equal(1, diffs[0].GetProperty("length").GetInt32());
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

    private Task<HttpResponseMessage> PutRightRawAsync(string id, string json)
    {
        return _client.PutAsync(
            $"/v1/diff/{id}/right",
            new StringContent(json, Encoding.UTF8, "application/json"));
    }

    private async Task<JsonDocument> ReadCompletedStatusAsync(string id)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var response = await _client.GetAsync($"/v1/diff/{id}/status");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            if (json.RootElement.GetProperty("status").GetString() == "Completed")
            {
                return json;
            }

            json.Dispose();
            await Task.Delay(100);
        }

        throw new TimeoutException("Diff status did not reach Completed.");
    }

    private static string CreateUniqueId()
    {
        return Random.Shared.Next(1, int.MaxValue).ToString();
    }
}
