using System.Net.Http.Json;
using System.Text.Json;
using Rise.Shared.Boats;
using Rise.Shared.Bookings;

namespace Rise.Client.Boats;
public class BoatService : IEquipmentService<BoatDto.ViewBoat, BoatDto.NewBoat>
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public BoatService(HttpClient httpClient)
    {        
        _httpClient = httpClient;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };  
    }

    public async Task<BoatDto.ViewBoat> CreateAsync(BoatDto.NewBoat boat)
    {
        var response = await _httpClient.PostAsJsonAsync("boat", boat);

        if(!response.IsSuccessStatusCode)
        {
             var errorMessage = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(errorMessage);
        }

        var boatResponse = await response.Content.ReadFromJsonAsync<BoatDto.ViewBoat>(_jsonSerializerOptions);
        return boatResponse;
    }

    public async Task<BoatDto.ViewBoat> CreateBoatAsync(BoatDto.NewBoat boat)
    {
        var response = await _httpClient.PostAsJsonAsync("boat", boat);

        if(!response.IsSuccessStatusCode)
        {
             var errorMessage = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(errorMessage);
        }

        var boatResponse = await response.Content.ReadFromJsonAsync<BoatDto.ViewBoat>(_jsonSerializerOptions);
        return boatResponse;
    }

    public async Task<IEnumerable<BoatDto.ViewBoat>?> GetAllAsync()
    {
        var jsonResponse = await _httpClient.GetStringAsync("boat");
        return JsonSerializer.Deserialize<IEnumerable<BoatDto.ViewBoat>>(jsonResponse, _jsonSerializerOptions);
    }
}