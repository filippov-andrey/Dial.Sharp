using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Dial.Sharp;

internal sealed partial class VerbatimHttpHandler(string expectedInput, string expectedOutput) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Assert.NotNull(request.Content);
        var actualInput = await request.Content.ReadAsStringAsync(cancellationToken);
        AssertContainsNormalized(expectedInput, actualInput);
        return new HttpResponseMessage { Content = new StringContent(expectedOutput) };
    }

    internal static void AssertContainsNormalized(string expected, string actual)
    {
        expected = RemoveWhiteSpace(expected)!;
        actual = RemoveWhiteSpace(actual)!;

        if (JsonNode.Parse(expected) is JsonObject expectedNode && JsonNode.Parse(actual) is JsonObject actualNode)
        {
            Assert.True(JsonContains(expectedNode, actualNode));
            return;
        }

        Assert.Equal(expected, actual);
    }

    private static bool JsonContains(JsonNode expected, JsonNode actual)
    {
        if (JsonNode.DeepEquals(expected, actual))
        {
            return true;
        }

        switch (expected)
        {
            case JsonObject expectedObj when actual is JsonObject actualObj:
                {
                    foreach (KeyValuePair<string, JsonNode?> property in expectedObj)
                    {
                        if (!actualObj.TryGetPropertyValue(property.Key, out JsonNode? actualValue) ||
                            property.Value is null ||
                            !JsonContains(property.Value, actualValue!))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            case JsonArray expectedArray when actual is JsonArray actualArray:
                {
                    if (expectedArray.Count > actualArray.Count)
                    {
                        return false;
                    }

                    return !expectedArray.Where((t, i) => !JsonContains(t!, actualArray[i]!)).Any();
                }
            default:
                return false;
        }
    }

    private static string? RemoveWhiteSpace(string? text) =>
        text is null
            ? null
            : MyRegex().Replace(text.Replace("\\r", "").Replace("\\n", "").Replace("\\t", ""), string.Empty);

    [GeneratedRegex(@"\s*")]
    private static partial Regex MyRegex();
}