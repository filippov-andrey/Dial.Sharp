using OpenAI.Audio;

namespace Dial.Sharp;

public class DialSpeechToTextClientTests
{
    [Fact]
    public void GetISpeechToTextClient_ProducesDialMetadata()
    {
        using VerbatimHttpHandler handler = new("{}", "{}");
        ISpeechToTextClient client = DialTestHost.CreateSpeechToTextClient(handler);

        SpeechToTextClientMetadata? metadata = client.GetService<SpeechToTextClientMetadata>();
        Assert.Equal("dial", metadata?.ProviderName);
        Assert.Equal(
            new Uri($"https://dial.example.com/openai/deployments/{DialTestHost.AudioDeployment}?api-version=2024-10-21"),
            metadata?.ProviderUri);
        Assert.Equal(DialTestHost.AudioDeployment, metadata?.DefaultModelId);
        Assert.NotNull(client.GetService<AudioClient>());
    }

    [Fact]
    public async Task GetTextAsync_SendsCustomContentAttachmentAndReturnsTranscription()
    {
        const string input = """
            {
              "model":"qwen3-asr",
              "stream":false,
              "messages":[{
                "role":"user",
                "content":"Transcribe this audio.",
                "custom_content":{
                  "attachments":[{
                    "type":"audio/mpeg",
                    "title":"audio.mp3",
                    "data":"AQID"
                  }]
                }
              }]
            }
            """;

        const string output = """
            {
              "choices":[{
                "message":{
                  "content":"Hello world",
                  "custom_content":{"stages":[{"name":"Language: English"}]}
                }
              }]
            }
            """;

        using VerbatimHttpHandler handler = new(input, output);
        ISpeechToTextClient client = DialTestHost.CreateSpeechToTextClient(handler);

        using MemoryStream audio = new([0x01, 0x02, 0x03]);
        SpeechToTextResponse response = await client.GetTextAsync(audio);

        Assert.Equal("Hello world", response.Text);
        Assert.Equal(DialTestHost.AudioDeployment, response.ModelId);
        Assert.IsType<AudioTranscription>(response.RawRepresentation);
    }

    [Fact]
    public async Task GetTextAsync_UsesPromptFromOptions()
    {
        const string input = """
            {
              "model":"qwen3-asr",
              "stream":false,
              "messages":[{
                "role":"user",
                "content":"Describe the speaker.",
                "custom_content":{
                  "attachments":[{
                    "type":"audio/mpeg",
                    "title":"audio.mp3",
                    "data":"AQID"
                  }]
                }
              }]
            }
            """;

        const string output = """
            {
              "choices":[{"message":{"content":"A calm voice."}}]
            }
            """;

        using VerbatimHttpHandler handler = new(input, output);
        ISpeechToTextClient client = DialTestHost.CreateSpeechToTextClient(handler);

        using MemoryStream audio = new([0x01, 0x02, 0x03]);
        SpeechToTextResponse response = await client.GetTextAsync(audio, new SpeechToTextOptions
        {
            RawRepresentationFactory = _ => new AudioTranscriptionOptions { Prompt = "Describe the speaker." },
        });

        Assert.Equal("A calm voice.", response.Text);
    }
}
