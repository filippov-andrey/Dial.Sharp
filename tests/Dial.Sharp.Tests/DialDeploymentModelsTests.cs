using System.Text.Json;

namespace Dial.Sharp;

public class DialDeploymentModelsTests
{
    private const string ChatDeploymentJson = """
        {
          "id": "qwen3.6-27b-awq",
          "model": "qwen3.6-27b-awq",
          "display_name": "Qwen3.6 27B",
          "reference": "qwen3.6-27b-awq",
          "owner": "organization-owner",
          "object": "model",
          "status": "succeeded",
          "created_at": 1672534800,
          "updated_at": 1672534800,
          "features": {
            "rate": false,
            "tokenize": true,
            "tools": true,
            "temperature": true
          },
          "input_attachment_types": ["image/jpeg"],
          "max_input_attachments": 2,
          "defaults": {"chat_template_kwargs": {"enable_thinking": true}},
          "responses_defaults": {},
          "description_keywords": ["Qwen"],
          "max_retry_attempts": 5,
          "interfaces": ["chat"],
          "lifecycle_status": "generally-available",
          "capabilities": {
            "scale_types": ["standard"],
            "chat_completion": true,
            "embeddings": false
          },
          "limits": {
            "max_total_tokens": 131071,
            "max_completion_tokens": 16000
          },
          "pricing": {
            "unit": "token",
            "prompt": "0.000002",
            "completion": "0.000008"
          }
        }
        """;

    [Fact]
    public void Deserialize_ChatDeployment_MapsDialFields()
    {
        var deployment = JsonSerializer.Deserialize<DialDeployment>(
            ChatDeploymentJson,
            DialDeploymentJsonSerializerOptions)!;

        Assert.Equal("qwen3.6-27b-awq", deployment.Id);
        Assert.Equal("Qwen3.6 27B", deployment.DisplayName);
        Assert.Equal(["chat"], deployment.Interfaces!);
        Assert.True(deployment.Features?.Tools);
        Assert.True(deployment.Capabilities?.ChatCompletion);
        Assert.Equal(131071, deployment.Limits?.MaxTotalTokens);
        Assert.Equal("token", deployment.Pricing?.Unit);
        Assert.True(deployment.Defaults.TryGetProperty("chat_template_kwargs", out _));
    }

    [Fact]
    public void Deserialize_DeploymentArray_MapsItems()
    {
        var deployments = JsonSerializer.Deserialize<DialDeployment[]>(
            """[{"id":"gpt-4","object":"model"}]""",
            DialDeploymentJsonSerializerOptions)!;

        Assert.Equal("gpt-4", deployments[0].Id);
    }

    [Fact]
    public void Deserialize_OpenAiDeploymentList_MapsData()
    {
        var list = JsonSerializer.Deserialize<DialDeploymentList>(
            $$"""{"object":"list","data":[{{ChatDeploymentJson}}]}""",
            DialDeploymentJsonSerializerOptions)!;

        Assert.Equal("list", list.Object);
        Assert.Equal("qwen3.6-27b-awq", list.Data[0].Id);
    }

    private static readonly JsonSerializerOptions DialDeploymentJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void Deserialize_OpenAiApplicationList_MapsData()
    {
        var list = JsonSerializer.Deserialize<DialApplicationList>(
            """
            {
              "object":"list",
              "data":[{
                "id":"dial-rag",
                "application":"dial-rag",
                "display_name":"DIAL RAG",
                "object":"application",
                "status":"succeeded",
                "features":{"url_attachments":true,"tools":false},
                "routes":{}
              }]
            }
            """,
            DialDeploymentJsonSerializerOptions)!;

        Assert.Equal("list", list.Object);
        Assert.Equal("dial-rag", list.Data[0].Application);
        Assert.True(list.Data[0].Features?.UrlAttachments);
    }
}
