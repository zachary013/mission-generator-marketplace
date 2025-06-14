namespace SmartMarketplace.Configuration;

public class AIConfig
{
    public string DefaultProvider { get; set; } = "Grok";
    public GrokConfig Grok { get; set; } = new();
    public OpenAIConfig OpenAI { get; set; } = new();
    public MistralConfig Mistral { get; set; } = new();
}

public class GrokConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.x.ai/v1";
    public string Model { get; set; } = "grok-beta";
}

public class OpenAIConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o";
}

public class MistralConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.mistral.ai/v1";
    public string Model { get; set; } = "mistral-small-latest";
}
