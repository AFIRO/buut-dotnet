using Rise.Shared.Users;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rise.Client.Users;

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public UserService(HttpClient _httpClient)
    {
        this._httpClient = _httpClient;
        this._jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
                // Add the immutable converters for System.Collections.Immutable types
        this._jsonSerializerOptions.Converters.Add(new ImmutableListJsonConverter<RoleDto>());
        this._jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<(bool Success, string? Message)> CreateUserAsync(UserDto.RegistrationUser userDetails)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("user", userDetails);
            if (response.IsSuccessStatusCode)
            {
                return (true, "User created successfully");
            }
            else
            {
                var errorContent = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                var message = errorContent != null && errorContent.TryGetValue("message", out var errorMessage)
                    ? errorMessage
                    : "Failed to create user due to an unknown error.";
                return (false, message);
            }
        }
        catch (Exception ex)
        {
            return (false, $"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<bool> SoftDeleteUserAsync(string userid)
    {
        var response = await _httpClient.DeleteAsync($"user/{userid}");
        return response.IsSuccessStatusCode;
    }

    public async Task<IEnumerable<UserDto.UserBase>?> GetAllAsync()
    {
        var jsonResponse = await _httpClient.GetStringAsync("user");
        return JsonSerializer.Deserialize<IEnumerable<UserDto.UserBase>>(jsonResponse, _jsonSerializerOptions);
    }

    public async Task<UserDto.UserBase?> GetUserByIdAsync(string userid)
    {
        var jsonResponse = await _httpClient.GetStringAsync($"user/{userid}");
        return JsonSerializer.Deserialize<UserDto.UserBase>(jsonResponse, _jsonSerializerOptions);
    }

    public async Task<UserDto.UserDetails?> GetUserDetailsByIdAsync(string userid)
    {
        var jsonResponse = await _httpClient.GetStringAsync($"user/{userid}/details");
        return JsonSerializer.Deserialize<UserDto.UserDetails>(jsonResponse, _jsonSerializerOptions);
    }

    public async Task<bool> UpdateUserAsync(UserDto.UpdateUser userDetails)
    {
        // Convert the object to a JSON string
        String jsonString = JsonSerializer.Serialize(userDetails, _jsonSerializerOptions);

        // Print the JSON string
        Console.WriteLine(jsonString);

        var response = await _httpClient.PutAsJsonAsync<UserDto.UpdateUser>($"user", userDetails);
        return response.IsSuccessStatusCode;
    }

    // public async Task<IEnumerable<UserDto.Auth0User>> GetAuth0Users()
    // {
    //     var users = await _httpClient.GetFromJsonAsync<IEnumerable<UserDto.Auth0User>>("user/auth/users");
    //     return users!;
    // }
    public async Task<IEnumerable<UserDto.Auth0User>> GetAuth0Users()
    {
        try
        {
            // Make the HTTP request and get the response
            var response = await _httpClient.GetAsync("user/authUsers");

            // Check if the response indicates success
            if (response.IsSuccessStatusCode)
            {
                // Deserialize and return the users if the request was successful
                var users = await response.Content.ReadFromJsonAsync<IEnumerable<UserDto.Auth0User>>();
                return users ?? Enumerable.Empty<UserDto.Auth0User>(); // Ensure non-null result
            }
            else
            {
                // Handle various HTTP errors based on status code
                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.NotFound:
                        // Handle 404 Not Found case
                        Console.WriteLine("No users found.");
                        break;
                    case System.Net.HttpStatusCode.ServiceUnavailable:
                        // Handle 503 Service Unavailable case
                        Console.WriteLine("Auth0 service is unavailable. Please try again later.");
                        break;
                    case System.Net.HttpStatusCode.InternalServerError:
                        // Handle 500 Internal Server Error case
                        Console.WriteLine("An unexpected server error occurred.");
                        break;
                    default:
                        // Handle other status codes
                        Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                        break;
                }

                // Optionally, return an empty list if there's an error
                return Enumerable.Empty<UserDto.Auth0User>();
            }
        }
        catch (HttpRequestException ex)
        {
            // Handle network-related errors
            Console.WriteLine($"Network error: {ex.Message}");
            return Enumerable.Empty<UserDto.Auth0User>();
        }
        catch (Exception ex)
        {
            // Handle any other unexpected errors
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            return Enumerable.Empty<UserDto.Auth0User>();
        }
    }

    public async Task<bool> IsEmailTakenAsync(String email)
    {
        try
        {
            // Send a GET request to the backend with the email parameter
            var response = await _httpClient.GetAsync($"user/exists?email={Uri.EscapeDataString(email)}");

            // If the response is successful, parse the result as a boolean
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<bool>();
                return result;
            }

            // Handle error or unexpected response status
            Console.WriteLine("Failed to check email availability.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return false;
        }
    }

    public async Task<IEnumerable<UserDto.UserBase>> GetFilteredUsersAsync(UserFilter filter)
    {
        try
        {
            List<string> queryParams = new();
            var properties = typeof(UserFilter).GetProperties();
            foreach (var property in properties)
            {
                var value = property.GetValue(filter);
                if (value != null)
                {
                    string stringValue = value is DateTime dateTime
                    ? dateTime.ToString("O") // Format DateTime as ISO 8601
                    : value.ToString();

                    queryParams.Add($"{property.Name.ToLower()}={Uri.EscapeDataString(stringValue)}");
                }
            }

            var queryString = string.Join("&", queryParams);
            var url = string.IsNullOrEmpty(queryString) ? "user/filtered" : $"user/filtered?{queryString}";

            var response = await _httpClient.GetAsync(url);

            // Read the content as a string
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Deserialize using JsonSerializer and your custom options
                var users = JsonSerializer.Deserialize<IEnumerable<UserDto.UserBase>>(jsonResponse, _jsonSerializerOptions);
                return users ?? Enumerable.Empty<UserDto.UserBase>();
            }
            else
            {
                Console.WriteLine($"Failed to get filtered users. Status code: {response.StatusCode}");
                return Enumerable.Empty<UserDto.UserBase>();
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return Enumerable.Empty<UserDto.UserBase>();
        }
    }

    public Task<bool> UpdateUserRolesAsync(string userId, ImmutableList<RoleDto> newRoles)
    {
        throw new NotImplementedException();
    }
}

