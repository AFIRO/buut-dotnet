using System.Text.Json;
using Rise.Shared.Boats;
using Rise.Shared.Bookings;

namespace Rise.Client.Boats;
public class BoatService : IBoatService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public BoatService(HttpClient httpClient)
    {
        Console.WriteLine("BoatService constructor gevonden");
        _httpClient = httpClient;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };        
        Console.WriteLine("BoatService is aangemaakt");
    }

    public async Task<IEnumerable<BoatDto.ViewBoat>?> GetAllAsync()
    {
        var jsonResponse = await _httpClient.GetStringAsync("boat");
        return JsonSerializer.Deserialize<IEnumerable<BoatDto.ViewBoat>>(jsonResponse, _jsonSerializerOptions);
    }
}