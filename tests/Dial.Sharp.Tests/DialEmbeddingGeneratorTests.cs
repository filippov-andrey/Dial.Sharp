using System.ClientModel;
using System.ClientModel.Primitives;
using OpenAI;

namespace Dial.Sharp;

public class DialEmbeddingGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_ReturnsEmbeddings()
    {
        const string input = """
            {"input":["hello"],"model":"embed"}
            """;

        const string output = """
            {
              "object":"list",
              "model":"embed",
              "data":[{"object":"embedding","index":0,"embedding":[0.1,0.2]}],
              "usage":{"prompt_tokens":1,"total_tokens":1}
            }
            """;

        using VerbatimHttpHandler handler = new(input, output);
        using HttpClient httpClient = new(handler);
        Uri endpoint = new("http://localhost/openai/deployments/embed?api-version=2023-12-01-preview");
        var embeddingClient = new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions
        {
            Endpoint = endpoint,
            Transport = new HttpClientPipelineTransport(httpClient),
        }).GetEmbeddingClient("embed");

        var generator = embeddingClient.AsIEmbeddingGenerator();
        var result = await generator.GenerateAsync(["hello"]);
        Assert.Single(result);
    }
}
