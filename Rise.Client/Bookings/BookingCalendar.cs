using System.Collections;
using System.Globalization;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Rise.Shared.Bookings;
using Rise.Shared.Enums;

namespace Rise.Client.Bookings;

public partial class BookingCalendar
{
    [Inject] private IDialogService DialogService { get; set; }
    [Inject] public required IBookingService BookingService { get; set; }
    [Parameter] public string? CurrentBookingId { get; set; }
    
    private string? userId;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            // Retrieve the user's ID (sub claim in Auth0)
            userId = user.FindFirst(c => c.Type == "sub")?.Value;
        }
    }

    private List<CustomCalenderItem> _events { get; set; } = new List<CustomCalenderItem>();

    private async Task ItemClicked(CustomCalenderItem item)
    {
        var result = await DialogService.ShowMessageBox(
            Localizer["MakeBookingTitle"],
            Localizer["MakeBookingMessage"] + " " + item.Start.ToString("dd MMMM", new CultureInfo(Localizer["DateLanguage"])) + " " + item.Text,
            Localizer["MakeBookingYes"],
            Localizer["MakeBookingNo"]
        );

        if (result == true)
            ReserveTimeslot(item);
    }

    private async void ReserveTimeslot(CustomCalenderItem item)
    {
        try
        {
            if (CurrentBookingId != null)
            {
                var booking = new BookingDto.UpdateBooking
                {
                    bookingId = CurrentBookingId,
                    bookingDate = item.Start
                };
                
                await BookingService.UpdateBookingAsync(booking);
                
                item.Available = false;
                var message = Localizer["DlgBookingEditedPt1", item.Start.Day, item.Start.TimeOfDay];

                var markupMessage = new MarkupString(message);
            
                await DialogService.ShowMessageBox(
                    @Localizer["DlgBookingEditedTtl"],
                    markupMessage);
            }
            else
            {
                var booking = new BookingDto.NewBooking
                {
                    bookingDate = item.Start,
                    // timeSlot = TimeSlotEnumExtensions.ToTimeSlot(item.Start.Hour),
                    userId = userId!
                };
                
                await BookingService.CreateBookingAsync(booking);
                
                item.Available = false;
                var message = Localizer["DlgBookingCompletePt1", booking.bookingDate.ToString("d"), booking.bookingDate.TimeOfDay];

                var markupMessage = new MarkupString(message);
            
                await DialogService.ShowMessageBox(
                    @Localizer["DlgBookingCompleteTtl"],
                    markupMessage);
            }
        }
        catch (Exception e)
        {
            ShowErrorDialog(Localizer["DlgBookingErrorTtl"], Localizer["DlgBookingErrorMsg"]);
        }

        StateHasChanged();
    }


    /* if the range is changed get the bookings from that month */
    private async void DateRangeChanged(DateRange dateRange)
    {
        _events = await getNewTimeSlots(dateRange);
        StateHasChanged();
    }

    private async Task<List<CustomCalenderItem>> getNewTimeSlots(DateRange dateRange)
    {
        var freeTimeSlots = await GetFreeCalendarItems(dateRange);

        var allTimeSlots = GenerateCalendarItems(dateRange);


        foreach (var item in allTimeSlots)
        {
            // Check if item exists in the free bookings (by comparing Start and End)
            if (freeTimeSlots.Any(i => i.Start == item.Start && i.End == item.End))
            {
                item.Available = true;
            }
        }

        return allTimeSlots;
    }

    private async Task<List<CustomCalenderItem>> GetFreeCalendarItems(DateRange dateRange)
    {
        var freeTimeslots = await BookingService.GetFreeTimeslotsInDateRange(dateRange.Start, dateRange.End);
        ArrayList freeCalendarItems = [];

        foreach (var timeslot in freeTimeslots)
        {
            freeCalendarItems.Add(
                new CustomCalenderItem
                {
                    Start = GetTimeFromTimeslot(timeslot.BookingDate, timeslot.TimeSlot),
                    End = GetTimeFromTimeslot(timeslot.BookingDate, timeslot.TimeSlot, true),
                    Text = "Occupied"
                });
        }

        return freeCalendarItems.Cast<CustomCalenderItem>().ToList();
    }

    /* booking generator for dateRange */
    List<CustomCalenderItem> GenerateCalendarItems(DateRange dateRange)
    {
        var events = new List<CustomCalenderItem>();
        var startDate = Guard.Against.Null(dateRange.Start);
        var endDate = Guard.Against.Null(dateRange.End);
        
        // Loop over each day in the date range
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Add sample events for each day
            events.Add(new CustomCalenderItem
            {
                Start = GetTimeFromTimeslot(date, TimeSlot.Morning),
                End = GetTimeFromTimeslot(date, TimeSlot.Morning, true),
                Text = Localizer["Forenoon"],
                Available = false
            });

            events.Add(new CustomCalenderItem
            {
                Start = GetTimeFromTimeslot(date, TimeSlot.Afternoon),
                End = GetTimeFromTimeslot(date, TimeSlot.Afternoon, true),
                Text = Localizer["Noon"],
                Available = false
            });

            events.Add(new CustomCalenderItem
            {
                Start = GetTimeFromTimeslot(date, TimeSlot.Evening),
                End = GetTimeFromTimeslot(date, TimeSlot.Evening, true),
                Text = Localizer["Afternoon"],
                Available = false
            });
        }

        return events;
    }

    private DateTime GetTimeFromTimeslot(DateTime date, TimeSlot timeSlot, bool isEnd = false)
    {
        switch (timeSlot)
        {
            case TimeSlot.Morning:
                return isEnd ? date.AddHours(TimeSlot.Morning.GetEndHour()) : date.AddHours(TimeSlot.Morning.GetStartHour());
            case TimeSlot.Afternoon:
                return isEnd ? date.AddHours(TimeSlot.Afternoon.GetEndHour()) : date.AddHours(TimeSlot.Afternoon.GetStartHour());
            case TimeSlot.Evening:
                return isEnd ? date.AddHours(TimeSlot.Evening.GetEndHour()) : date.AddHours(TimeSlot.Evening.GetStartHour());
        }

        return DateTime.MinValue;
    }

    private void ShowErrorDialog(string title, string message)
    {
        var options = new DialogOptions { CloseOnEscapeKey = true };

        DialogService.ShowMessageBox(
            $"{title}",
            $"{message}",
            "ok",
            options: options
        );
    }
}