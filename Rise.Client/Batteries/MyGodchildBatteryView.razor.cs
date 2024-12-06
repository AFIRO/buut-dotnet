using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Rise.Shared.Batteries;
using Rise.Shared.Bookings;
using Rise.Shared.Enums;
using Rise.Shared.Users;

namespace Rise.Client.Batteries;

public partial class MyGodchildBatteryView
{
    [Inject] private IDialogService DialogService { get; set; }
    [Inject] public IBatteryService BatteryService { get; set; }
    [Inject] AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    private string? userIdAuth0;
    private bool _isLoading = false;
    private bool _hasTimeout = false;
    private bool _isInError = false;
    private string _errorMessage = "";
    private BatteryDto.ViewBatteryBuutAgent? battery;
    private UserDto.UserContactDetails? holderDetails;
    private String streetName = "";
    private int _timeoutDelayInSeconds = 5;

    private CancellationTokenSource? _cancellationTokenSource;
    
    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        _hasTimeout = false;
        _isInError = false;

        try{
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // Set a timeout of 10 seconds for data loading
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_timeoutDelayInSeconds), token);
            var dataLoadTask = LoadDataAsync(token);

            if (await Task.WhenAny(dataLoadTask, timeoutTask) == timeoutTask)
            {
                // Timeout occurred
                _hasTimeout = true;
            }

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            userIdAuth0 = authState.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        }
        catch (Exception ex){
            _isInError = true;
            _errorMessage = $"Error occurred during initialization: {ex.Message}";
        }
        
        finally
        {
            // end loading and partial refresh
            _isLoading = false;
            StateHasChanged();
            
        }

        _cancellationTokenSource?.Dispose();
        _isLoading = false;
    }

    private async Task LoadDataAsync(CancellationToken token)
    {
    
        try
        {
            // Get the current user's authentication state
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            userIdAuth0 = authState.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(userIdAuth0))
            {
                throw new Exception("User authentication ID is missing.");
            }
            // getting the battery of the godparent
            battery = await BatteryService.GetBatteryByGodparentUserIdAsync(userIdAuth0);
    
            // getting the batteries current holder
            holderDetails = await BatteryService.GetBatteryHolderByGodparentUserIdAsync(userIdAuth0);
    
            streetName = StreetEnumExtensions.GetStreetName(holderDetails.Address.Street);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error occurred during Loading of data: {ex.Message}";
            // if an error occurs, set a timedOut to display a user-friendly message in the UI
            _isInError = true;
            StateHasChanged();
        }
    }

    private async Task ReloadPage()
    {
        _isLoading = true;
        ResetFlags();

        await OnInitializedAsync();
    }

    private async Task ClaimBattery(string userIdAuth0, string batteryId)
    {
        try
        {
            holderDetails = await BatteryService.ClaimBatteryAsGodparentAsync(userIdAuth0, batteryId);
            StateHasChanged(); // Update the UI with the new holder details
        }
        catch (Exception ex)
        {
            // Set a user-friendly message or flag for displaying in the UI
            _errorMessage = $"Error occurred during Claiming of the battery: {ex.Message}";
            _hasTimeout = true;
            StateHasChanged();
        }
    }

    private void ResetFlags()
    {
        _hasTimeout = false;
        _isInError = false;
        _errorMessage = "";
    }


}