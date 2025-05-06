using System.Net.Http;
using System.Text.Json;

namespace ProjectX.Services.ngrok;

public class NgrokService(HttpClient httpClient)
{
    public async Task<string> GetNgrokUrlAsync()
    {
        try
        {
            var response = await httpClient.GetAsync("http://localhost:4040/api/tunnels");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<NgrokTunnels>(content);
            var publicUrl = json?.Tunnels?.FirstOrDefault(t => t.Proto == "https")?.PublicUrl;
            return publicUrl ?? throw new Exception("No HTTPS tunnel found.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ngrok Error: {ex.Message}");
            return "https://localhost:7069"; 
        }
    }
}

public class NgrokTunnels
{
    public List<NgrokTunnel> Tunnels { get; set; } = new();
}

public class NgrokTunnel
{
    public string PublicUrl { get; set; } = string.Empty;
    public string Proto { get; set; } = string.Empty;
}