using System.Text.Json.Nodes;
using Amazon.SecretsManager.Extensions.Caching;
namespace MttBackend;


public class AwsSecrets
{

    private readonly SecretsManagerCache _cache = new();
    
    public async Task<string?> GetSecret(string secretName, string key)
    {
        try
        {
            string jsonString = await _cache.GetSecretString(secretName);
            var jsonObj = JsonNode.Parse(jsonString)?.AsObject();
            return jsonObj?[key]?.AsValue().ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load secret {secretName}, {key}: {ex.Message}");
            throw;
        }
        
    }
}