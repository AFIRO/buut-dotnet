using System.Net.Http.Json;
using System.Text.Json;
using Rise.Shared.Bookings;

namespace Rise.Client.Batteries;
public class BatteryService : IEquipmentService<BatteryDto.ViewBattery, BatteryDto.NewBattery>
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public BatteryService(HttpClient httpClient)
    {        
        _httpClient = httpClient;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };  
    }

    public async Task<BatteryDto.ViewBattery> CreateAsync(BatteryDto.NewBattery battery)
    {
        var response = await _httpClient.PostAsJsonAsync("battery", battery);

        if(!response.IsSuccessStatusCode)
        {
             var errorMessage = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(errorMessage);
        }

        var batteryResponse = await response.Content.ReadFromJsonAsync<BatteryDto.ViewBattery>(_jsonSerializerOptions);
        return batteryResponse;
    }

    public async Task<IEnumerable<BatteryDto.ViewBattery>?> GetAllAsync()
    {
        var jsonResponse = await _httpClient.GetStringAsync("battery");
        return JsonSerializer.Deserialize<IEnumerable<BatteryDto.ViewBattery>>(jsonResponse, _jsonSerializerOptions);
    }
}