namespace SmartMarketplace.Configuration;

public class AIConfig
{
    public string DefaultProvider { get; set; } = "Gemini";
    public GeminiConfig Gemini { get; set; } = new();
    public MistralConfig Mistral { get; set; } = new();
    public DeepSeekConfig DeepSeek { get; set; } = new();
}

public class GeminiConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public string Model { get; set; } = "gemini-1.5-flash";
}

public class LlamaConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api-inference.huggingface.co/models";
    public string Model { get; set; } = "meta-llama/Meta-Llama-3-8B-Instruct";
}

public class MistralConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.mistral.ai/v1";
    public string Model { get; set; } = "mistral-small-2503";
}

public class DeepSeekConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openrouter.ai/v1";
    public string Model { get; set; } = "deepseek/deepseek-r1:free";
}
