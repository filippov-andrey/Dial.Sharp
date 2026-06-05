using Microsoft.Agents.AI;

namespace Dial.Sharp;

/// <summary>Microsoft Agent Framework (<c>ChatClientAgent</c>) over Dial <see cref="IChatClient"/>.</summary>
public class DialAgentFrameworkTests
{
    [Fact]
    public async Task ChatClientAgent_RunAsync_UsesDialIChatClient()
    {
        const string input = """
            {
              "messages":[
                {"role":"system","content":"You are a helpful assistant."},
                {"role":"user","content":"What is AI?"}
              ],
              "model":"gpt-4o-mini"
            }
            """;

        using VerbatimHttpHandler handler = new(input, DialTestHost.ChatCompletionJson());
        using IChatClient chatClient = DialTestHost.CreateChatClient(handler);

        ChatClientAgent agent = chatClient.AsAIAgent(
            instructions: "You are a helpful assistant.",
            name: "DialAgent");

        AgentResponse response = await agent.RunAsync("What is AI?");

        Assert.Equal("AI is machine intelligence.", response.Text);
    }
}
