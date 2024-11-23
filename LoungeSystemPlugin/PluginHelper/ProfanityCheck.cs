using System.Net.Http.Headers;
using Newtonsoft.Json;


namespace LoungeSystemPlugin.PluginHelper;

public static class ProfanityCheck
{
    private static readonly HttpClient HttpClient = new HttpClient();
    
    public static async Task<bool> CheckString(string profanity)
    {
        var messageContent = new
        {
            message = profanity
            
        };
        var messageContentAsJson = JsonConvert.SerializeObject(messageContent);
        
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://vector.profanity.dev/"),
            //Content = new StringContent("{\"message\":\"Test Message\"}")
            Content = new StringContent(messageContentAsJson)
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/json")
                }
            }
        };
        using var response = await HttpClient.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
        
        var responseBody = await response.Content.ReadAsStringAsync();

        var deserializedResult = JsonConvert.DeserializeObject<ProfanityResult>(responseBody);
        
        if (ReferenceEquals(deserializedResult, null))
            throw new NullReferenceException();
        
        return deserializedResult.IsProfanity;
    }
}

public record ProfanityResult(
    bool IsProfanity,
    double Score
);