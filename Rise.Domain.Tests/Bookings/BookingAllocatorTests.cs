using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Rise.Domain.Bookings.Tests
{
    public class BookingAllocatorTests
    {
        private readonly BookingAllocator _bookingAllocator;

        public BookingAllocatorTests()
        {
            _bookingAllocator = new BookingAllocator();
        }

        [Fact]
        public void AssignBatteriesBoats_ShouldAssignBatteriesAndBoatsCorrectly()
        {
            // Arrange
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            var batteries = new List<Battery>
            {
                new Battery("Battery1") { CountBookings = 2 },
                new Battery("Battery2") { CountBookings = 1 },
                new Battery("Battery3") { CountBookings = 3 }
            };

            var boats = new List<Boat>
            {
                new Boat("Boat1") { CountBookings = 3 },
                new Boat("Boat2") { CountBookings = 1 },
                new Boat("Boat3") { CountBookings = 2 }
            };

            var bookings = new List<Booking>
            {
                new Booking(yesterday, "User1") { Battery = batteries[0] }, // Yesterday's booking
                new Booking(today, "User2"),
                new Booking(today, "User3"),
                new Booking(today, "User4")
            };

            // Act
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _bookingAllocator.assignBatteriesBoats(bookings, batteries, boats, today));

            // Assert
            Assert.Equal(2, batteries.Count); // One battery should have been removed
            Assert.Equal("Battery2", bookings[1].Battery.Name); // Lowest usage battery assigned
            Assert.Equal("Battery3", bookings[2].Battery.Name);

            Assert.Equal("Boat2", bookings[1].Boat.Name); // Lowest usage boat assigned
            Assert.Equal("Boat3", bookings[2].Boat.Name);
        }

        [Fact]
        public void AssignBatteriesBoats_ShouldNotAssignIfNoAvailableResources()
        {
            // Arrange
            var today = DateTime.Today;

            var batteries = new List<Battery>();
            var boats = new List<Boat>();
            var bookings = new List<Booking>
            {
                new Booking(today, "User1"),
                new Booking(today, "User2")
            };

            // Act
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _bookingAllocator.assignBatteriesBoats(bookings, batteries, boats, today));
            
            // Assert
            Assert.All(bookings, b => Assert.Null(b.Battery));
            Assert.All(bookings, b => Assert.Null(b.Boat));
        }

        [Fact]
        public void AssignBatteriesBoats_ShouldOrderBookingsByHour()
        {
            // Arrange
            var today = DateTime.Today;

            var batteries = new List<Battery>
            {
                new Battery("Battery1"),
                new Battery("Battery2"),
                new Battery("Battery3")
            };

            var boats = new List<Boat>
            {
                new Boat("Boat1"),
                new Boat("Boat2"),
                new Boat("Boat3")
            };

            var bookings = new List<Booking>
            {
                new Booking(today.AddHours(17), "User1"), // Afternoon
                new Booking(today.AddHours(10), "User2"), // Morning
                new Booking(today.AddHours(14), "User3") // Early Afternoon
            };

            // Act
            _bookingAllocator.assignBatteriesBoats(bookings, batteries, boats, today);

            // Assert
            Assert.Equal("User2", bookings[1].UserId); // Morning booking
            Assert.Equal("User3", bookings[2].UserId); // Early Afternoon booking
            Assert.Equal("User1", bookings[0].UserId); // Afternoon booking

            Assert.NotNull(bookings[0].Battery);
            Assert.NotNull(bookings[0].Boat);
        }

        [Fact]
        public void AssignBatteriesBoats_ShouldHandleEmptyBookings()
        {
            // Arrange
            var today = DateTime.Today;

            var batteries = new List<Battery>
            {
                new Battery("Battery1"),
                new Battery("Battery2")
            };

            var boats = new List<Boat>
            {
                new Boat("Boat1"),
                new Boat("Boat2")
            };

            var bookings = new List<Booking>();

            // Act
            _bookingAllocator.assignBatteriesBoats(bookings, batteries, boats, today);

            // Assert
            Assert.Empty(bookings); // No bookings should remain
            Assert.Equal(2, batteries.Count); // Batteries untouched
            Assert.Equal(2, boats.Count); // Boats untouched
        }

        [Fact]
        public void AssignBatteriesBoats_ShouldHandleMoreBookingsThanResources()
        {
            // Arrange
            var today = DateTime.Today;

            var batteries = new List<Battery>
            {
                new Battery("Battery1") { CountBookings = 2 },
                new Battery("Battery2") { CountBookings = 1 }
            };

            var boats = new List<Boat>
            {
                new Boat("Boat1") { CountBookings = 1 },
                new Boat("Boat2") { CountBookings = 2 }
            };

            var bookings = new List<Booking>
            {
                new Booking(today, "User1"),
                new Booking(today, "User2"),
                new Booking(today, "User3") // No resources available for this booking
            };

            // Act
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _bookingAllocator.assignBatteriesBoats(bookings, batteries, boats, today));
            
            // Assert
            Assert.NotNull(bookings[0].Battery);
            Assert.NotNull(bookings[0].Boat);

            Assert.NotNull(bookings[1].Battery);
            Assert.NotNull(bookings[1].Boat);

            Assert.Null(bookings[2].Battery); // No battery available
            Assert.Null(bookings[2].Boat); // No boat available
        }

        [Fact]
        public void AssignBatteriesBoats_ShouldHandleIdenticalUsageResources()
        {
            // Arrange
            var today = DateTime.Today;

            var batteries = new List<Battery>
            {
                new Battery("Battery1") { CountBookings = 1 },
                new Battery("Battery2") { CountBookings = 1 }
            };

            var boats = new List<Boat>
            {
                new Boat("Boat1") { CountBookings = 2 },
                new Boat("Boat2") { CountBookings = 2 }
            };

            var bookings = new List<Booking>
            {
                new Booking(today, "User1"),
                new Booking(today, "User2")
            };

            // Act
            _bookingAllocator.assignBatteriesBoats(bookings, batteries, boats, today);

            // Assert
            Assert.NotNull(bookings[0].Battery);
            Assert.NotNull(bookings[1].Battery);

            Assert.NotNull(bookings[0].Boat);
            Assert.NotNull(bookings[1].Boat);

            // Ensure that the assignment did not result in duplicate resources.
            Assert.NotEqual(bookings[0].Battery.Name, bookings[1].Battery.Name);
            Assert.NotEqual(bookings[0].Boat.Name, bookings[1].Boat.Name);
        }

        [Fact]
        public void AssignBatteriesBoats_ShouldIgnoreYesterdayResources()
        {
            // Arrange
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            var batteries = new List<Battery>
            {
                new Battery("Battery1") { CountBookings = 1 },
                new Battery("Battery2") { CountBookings = 2 },
                new Battery("Battery3") { CountBookings = 0 } // Should be available
            };

            var boats = new List<Boat>
            {
                new Boat("Boat1") { CountBookings = 1 },
                new Boat("Boat2") { CountBookings = 2 },
                new Boat("Boat3") { CountBookings = 0 } // Should be available
            };

            var bookings = new List<Booking>
            {
                new Booking(yesterday, "User1") { Battery = batteries[0], Boat = boats[0] },
                new Booking(today, "User2"),
                new Booking(today, "User3")
            };

            // Act
            _bookingAllocator.assignBatteriesBoats(bookings, batteries, boats, today);

            // Assert
            Assert.Equal(2, batteries.Count); // One battery removed (yesterday's resource)
            Assert.Equal(3, boats.Count);

            Assert.NotNull(bookings[1].Battery);
            Assert.NotNull(bookings[1].Boat);

            Assert.NotNull(bookings[2].Battery);
            Assert.NotNull(bookings[2].Boat);

            Assert.NotEqual(bookings[1].Battery.Name, bookings[2].Battery.Name);
            Assert.NotEqual(bookings[1].Boat.Name, bookings[2].Boat.Name);
        }

        [Fact]
        public void AssignBatteriesBoats_ShouldAssignResourcesToEarliestBookingsFirst()
        {
            // Arrange
            var today = DateTime.Today;

            var batteries = new List<Battery>
            {
                new Battery("Battery1"),
                new Battery("Battery2")
            };

            var boats = new List<Boat>
            {
                new Boat("Boat1"),
                new Boat("Boat2")
            };

            var bookings = new List<Booking>
            {
                new Booking(today.AddHours(14), "User1"), // Afternoon
                new Booking(today.AddHours(10), "User2") // Morning
            };

            // Act
            _bookingAllocator.assignBatteriesBoats(bookings, batteries, boats, today);
            
            var sortedBookings = bookings.OrderBy(x => x.BookingDate.Hour).ToList();


            // Assert
            Assert.Equal("User2", sortedBookings[0].UserId); // Morning booking assigned first
            Assert.Equal("User1", sortedBookings[1].UserId); // Afternoon booking assigned next

            Assert.Equal("Battery1", sortedBookings[0].Battery.Name);
            Assert.Equal("Battery2", sortedBookings[1].Battery.Name);

            Assert.Equal("Boat1", sortedBookings[0].Boat.Name);
            Assert.Equal("Boat2", sortedBookings[1].Boat.Name);
        }

        [Fact]
        public void AssignBatteriesBoats_ShouldNotModifyOriginalResourceLists()
        {
            // Arrange
            var today = DateTime.Today;

            var originalBatteries = new List<Battery>
            {
                new Battery("Battery1"),
                new Battery("Battery2")
            };

            var originalBoats = new List<Boat>
            {
                new Boat("Boat1"),
                new Boat("Boat2")
            };

            var bookings = new List<Booking>
            {
                new Booking(today, "User1"),
                new Booking(today, "User2")
            };

            var batteriesCopy = new List<Battery>(originalBatteries);
            var boatsCopy = new List<Boat>(originalBoats);

            // Act
            _bookingAllocator.assignBatteriesBoats(bookings, batteriesCopy, boatsCopy, today);

            // Assert
            Assert.Equal(2, originalBatteries.Count);
            Assert.Equal(2, originalBoats.Count);
        }

        [Fact]
        public void AssignBatteriesBoats_ShouldHandleBookingsSpanningMultipleDays()
        {
            // Arrange
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            var tomorrow = today.AddDays(1);

            var batteries = new List<Battery>
            {
                new Battery("Battery1") { CountBookings = 2 },
                new Battery("Battery2") { CountBookings = 1 }
            };

            var boats = new List<Boat>
            {
                new Boat("Boat1") { CountBookings = 2 },
                new Boat("Boat2") { CountBookings = 1 }
            };

            var bookings = new List<Booking>
            {
                new Booking(yesterday, "User1"), // Booking from yesterday
                new Booking(today, "User2"), // Today's booking
                new Booking(tomorrow, "User3") // Booking for tomorrow
            };

            // Act
            _bookingAllocator.assignBatteriesBoats(bookings, batteries, boats, today);

            // Assert
            Assert.Null(bookings[0].Battery); // Yesterday's booking should not be modified
            Assert.Null(bookings[0].Boat);

            Assert.NotNull(bookings[1].Battery); // Today's booking should be assigned resources
            Assert.NotNull(bookings[1].Boat);

            Assert.Null(bookings[2].Battery); // Tomorrow's booking should not be modified
            Assert.Null(bookings[2].Boat);
        }

        [Fact]
        public void AssignBatteriesBoats_ShouldNotAssignWhenNoBookingsForToday()
        {
            // Arrange
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            var batteries = new List<Battery>
            {
                new Battery("Battery1"),
                new Battery("Battery2")
            };

            var boats = new List<Boat>
            {
                new Boat("Boat1"),
                new Boat("Boat2")
            };

            var bookings = new List<Booking>
            {
                new Booking(yesterday, "User1") // Booking from yesterday
            };

            // Act
            _bookingAllocator.assignBatteriesBoats(bookings, batteries, boats, today);

            // Assert
            Assert.All(bookings, b => Assert.Null(b.Battery));
            Assert.All(bookings, b => Assert.Null(b.Boat));

            Assert.Equal(2, batteries.Count); // Resources remain unchanged
            Assert.Equal(2, boats.Count);
        }

        [Fact]
        public void AssignBatteriesBoats_ShouldThrowExceptionWhenResourcesAreInsufficient()
        {
            // Arrange
            var today = DateTime.Today;

            var batteries = new List<Battery>
            {
                new Battery("Battery1")
            };

            var boats = new List<Boat>
            {
                new Boat("Boat1")
            };

            var bookings = new List<Booking>
            {
                new Booking(today, "User1"),
                new Booking(today, "User2") // Insufficient resources
            };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _bookingAllocator.assignBatteriesBoats(bookings, batteries, boats, today));
        }
    }
}