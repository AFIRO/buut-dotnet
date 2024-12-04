using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using MudBlazor;
using Rise.Client.Components.Table;
using Rise.Shared.Bookings;
using Rise.Shared.Enums;

namespace Rise.Client.Bookings;

public partial class MyBookingsView
{
    [Inject] private IDialogService DialogService { get; set; }
    [Inject] public required IBookingService BookingService { get; set; }
    [Inject] AuthenticationStateProvider AuthenticationStateProvider { get; set; }
    
    
    private IEnumerable<BookingDto.ViewBooking>? _bookings;

    private int _maxBookings;
    private string? userIdAuth0;
    private bool _isLoading = false;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        // Get the current user's authentication state
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        userIdAuth0 = authState.User.Claims.FirstOrDefault(c => c.Type == "sub").Value;
        _bookings = await BookingService.GetAllUserBookings(userIdAuth0) ?? new List<BookingDto.ViewBooking>();
        _isLoading = false;
    }

    private async Task DeleteBooking(string bookingId)
    {
        var result = await DialogService.ShowMessageBox(
            Localizer["CancelBookingTitle"],
            Localizer["CancelBookingMessage"],
            Localizer["ChoiceYes"],
            Localizer["ChoiceNo"]
        );

        if (result == true)
        {
            await BookingService.DeleteBookingAsync(bookingId);
            _bookings = await BookingService.GetAllUserBookings(userIdAuth0);
            StateHasChanged();
        }
    }
    
    private List<TableHeader> Headers => new List<TableHeader>
    {
        new (Localizer["Date"]),
        new (Localizer["TimeSlot"]),
        new (Localizer["Status"], "text-center"),
        new (Localizer["Boat"]),
        new (Localizer["Contact"]),
        new (Localizer["Credits"]),
        new ("") // Empty header for action column
    };
    
    private List<List<RenderFragment>> Data(IEnumerable<BookingDto.ViewBooking> bookings)
    {
        var rows = new List<List<RenderFragment>>();

        if (bookings.IsNullOrEmpty())
        {
            return new List<List<RenderFragment>>();
        }
        
        foreach (var booking in bookings)
        {
            var row = new List<RenderFragment>();
            
            var userCell = TableCellService.UserCell();
            if (booking.battery is BatteryDto.ViewBatteryWithCurrentUser batteryWithCurrentUser)
            {
                userCell = TableCellService.UserCell(
                    batteryWithCurrentUser.currentUser.FirstName,
                    batteryWithCurrentUser.currentUser.LastName,
                    batteryWithCurrentUser.currentUser.PhoneNumber);
            }

            row = new List<RenderFragment>
            {
                TableCellService.DefaultTableCell(booking.bookingDate.ToString("D")),
                TableCellService.DefaultTableCell(Localizer[booking.timeSlot.ToString()]),
                TableCellService.BadgeCell(Localizer[booking.status.ToString()], "badge bg-gradient-" + GetBadgeBackground(booking.status)),
                TableCellService.DefaultTableCell(booking.boat.name),
                userCell,
                TableCellService.DefaultTableCell("Todo"),
                booking.status == BookingStatus.OPEN 
                    ? TableCellService.ActionCell(booking.bookingId, this, EditBooking, DeleteBooking) 
                    : TableCellService.DefaultTableCell("")
            };


            rows.Add(row);
        }

        return rows;
    }
    
    private string GetBadgeBackground(BookingStatus status) => status switch
    {
        BookingStatus.OPEN => "dark",
        BookingStatus.CLOSED => "secondary",
        BookingStatus.COMPLETED => "success",
        BookingStatus.REFUNDED => "warning",
        BookingStatus.CANCELED => "danger",
        _ => "light"
    };

    private async Task EditBooking(string bookingId)
    {
        var result = await DialogService.ShowMessageBox(
            Localizer["EditBookingTitle"],
            Localizer["EditBookingMessage"],
            Localizer["ChoiceYes"],
            Localizer["ChoiceNo"]
        );

        if (result == true)
        {
            await ShowCalendarForEditing(bookingId);
        }
    }

    private async Task ShowCalendarForEditing(string bookingId)
    {
        var dialogParameters = new DialogParameters<BookingCalendar>();
        dialogParameters.Add(p => p.CurrentBookingId, bookingId);
        
        var dialog = await DialogService.ShowAsync<BookingCalendar>(Localizer["EditBookingTitle"], dialogParameters);
        var result = await dialog.Result;
        Console.WriteLine(result.Canceled);
        if (result.Canceled)
        {
            _bookings = await BookingService.GetAllUserBookings(userIdAuth0);
            StateHasChanged();
        }
    }
}