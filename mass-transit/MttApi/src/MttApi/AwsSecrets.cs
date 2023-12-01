using System.Text.Json.Nodes;
using Amazon.SecretsManager.Extensions.Caching;

namespace MttApi;

public class AwsSecrets
{

    private readonly SecretsManagerCache _cache = new();
    private const string SecretName = "application-secrets";

    public async Task<string> GetSecret(string name)
    {
        var jsonString = await _cache.GetSecretString(SecretName);
        var jsonObj = JsonNode.Parse(jsonString).AsObject();
        return jsonObj[name].GetValue<string>();
    }
}
