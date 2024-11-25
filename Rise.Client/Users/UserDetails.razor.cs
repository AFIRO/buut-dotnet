using Microsoft.AspNetCore.Components;
using Rise.Shared.Users;
using Rise.Shared.Enums;
using System.Collections.Immutable;
using Microsoft.JSInterop;
using MudBlazor;
using Microsoft.Extensions.Localization;

namespace Rise.Client.Users;
public partial class UserDetails
{

    [Parameter]
    public string UserId { get; set; }
    [Parameter] public EventCallback OnBackClick { get; set; }
    private UserDto.UserDetails _userDetails;
    private bool _isLoading = false;
    private bool HasPendingRole => _userDetails?.Roles.Any(role => role.Name == RolesEnum.Pending) == true;

    private const string ApproveAction = "approve";
    private const string RejectAction = "reject";

    [Inject] public required IUserService UserService { get; set; }
    [Inject] public required IDialogService DialogService { get; set; }
    [Inject] public required IStringLocalizer<UserDetails> Localizer { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        _userDetails = await UserService.GetUserDetailsByIdAsync(UserId);
        _isLoading = false;
    }

    private async Task GoBack()
    {
        await OnBackClick.InvokeAsync();
    }

    private async Task ShowConfirmationDialog(string actionType)
    {
        string action = Localizer[actionType];

        var parameters = new DialogParameters
    {
        { "ContentText", string.Format(Localizer["ConfirmUserAction"], action, _userDetails.FirstName, _userDetails.LastName) },
        { "ButtonApproveText", string.Format(Localizer["Yes"]) },
        { "ButtonRejectText", string.Format(Localizer["No"]) },
        { "Color", Color.Primary }
    };

        var options = new DialogOptions { CloseOnEscapeKey = true };

        var dialog = DialogService.Show<ConfirmDialog>(string.Format(Localizer["Confirmation"]), parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            if (actionType == ApproveAction)
            {
                await ApproveUser();
            }
            else if (actionType == RejectAction)
            {
                await RejectUser();
            }
        }
    }

    private async Task ApproveUser()
    {
        _isLoading = true;
        ImmutableList<RoleDto> singleRoleList = [new RoleDto { Name = RolesEnum.User }];

        UserDto.UpdateUser userToUpdate = new()
        {
            Id = _userDetails.Id,
            Roles = singleRoleList
        };
        var success = await UserService.UpdateUserAsync(userToUpdate);
        if (success)
        {
            _isLoading = false;
            await GoBack();
        }
        _isLoading = false;
    }

    private async Task RejectUser()
    {
        _isLoading = true;
        // var success = await UserService.DeleteUserAsync(UserId);
        var success = true;
        if (success)
        {
            _isLoading = false;
            await GoBack();
        }
        _isLoading = false;
    }

    
}