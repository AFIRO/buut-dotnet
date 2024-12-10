using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rise.Shared.Batteries;
using Rise.Shared.Bookings;
using Rise.Shared.Users;

namespace Rise.Client.Batteries;

public class BatteryService : IBatteryService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public BatteryService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this._jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        // Add the immutable converters for System.Collections.Immutable types
        this._jsonSerializerOptions.Converters.Add(new ImmutableListJsonConverter<RoleDto>());
        this._jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<UserDto.UserContactDetails?> ClaimBatteryAsGodparentAsync(string godparentId, string batteryId)
    {
        var jsonResponse = await _httpClient.PostAsync($"battery/godparent/{godparentId}/{batteryId}/claim", null);
        var jsonResponseStream = await jsonResponse.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<UserDto.UserContactDetails>(jsonResponseStream, _jsonSerializerOptions);
    }

    public async Task<BatteryDto.ViewBattery> CreateAsync(BatteryDto.NewBattery battery)
    {
        var response = await _httpClient.PostAsJsonAsync("battery", battery);

        if (!response.IsSuccessStatusCode)
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

    public async Task<UserDto.UserContactDetails?> GetBatteryHolderByGodparentUserIdAsync(string godparentId)
    {
        try
        {
            var jsonResponse = await _httpClient.GetStringAsync($"battery/godparent/holder");
            return JsonSerializer.Deserialize<UserDto.UserContactDetails>(jsonResponse, _jsonSerializerOptions);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BatteryDto.ViewBatteryBuutAgent?> GetBatteryByGodparentUserIdAsync(string godparentId)
    {
        var jsonResponse = await _httpClient.GetStringAsync($"battery/godparent/info");
        return JsonSerializer.Deserialize<BatteryDto.ViewBatteryBuutAgent>(jsonResponse, _jsonSerializerOptions);
    }

    public async Task<bool> UpdateAsync(BatteryDto.UpdateBattery battery)
    {
        var response = await _httpClient.PutAsJsonAsync($"battery/{battery.id}", battery);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(errorMessage);
        }

        return true;
    }

    public async Task<bool> DeleteAsync(string equipmentId)
    {
        var response = await _httpClient.DeleteAsync($"battery/{equipmentId}");
        return response.IsSuccessStatusCode;
    }
}