using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using Microsoft.Extensions.Localization;
using System;
using Rise.Shared.Bookings;
using Rise.Client.Components.Table;
using Rise.Shared.Batteries;

namespace Rise.Client.Tests
{
    public class BatteryTest : TestContext
    {
        private Mock<IBatteryService> _batteryServiceMock;
        private Mock<IStringLocalizer<GenericTable>> _localizerMock;
        private Mock<IStringLocalizer<Batteries.Battery>> _batteryLocalizerMock;
        private Mock<MudBlazor.IDialogService> _dialogServiceMock;

        public BatteryTest()
        {
            // Maak een mock van IBatteryService met Moq
            _batteryServiceMock = new Mock<IBatteryService>();
            _localizerMock = new Mock<IStringLocalizer<GenericTable>>();
            _batteryLocalizerMock = new Mock<IStringLocalizer<Batteries.Battery>>();
            _dialogServiceMock = new Mock<MudBlazor.IDialogService>();

            // Voeg de mock toe aan de dependency injection container
            Services.AddSingleton(_batteryServiceMock.Object);
            Services.AddSingleton(_localizerMock.Object);
            Services.AddSingleton(_batteryLocalizerMock.Object);
            Services.AddSingleton(_dialogServiceMock.Object);
        }

        [Fact]
        public async Task Should_Display_Title()
        {
            // Arrange
            _batteryLocalizerMock.Setup(l => l["Title"]).Returns(new LocalizedString("Title", "Batterijen"));

            // Act
            var component = RenderComponent<Batteries.Battery>();

            // Assert
            component.Find("h1").MarkupMatches("<h1>Batterijen</h1>");
        }

        [Fact]
        public async Task Should_Display_Table_Header_After_Data_Is_Fetched()
        {
            // Arrange
            var batteries = new List<BatteryDto.ViewBattery>
            {
                new BatteryDto.ViewBattery { name = "Battery 1", countBookings = 10},
                new BatteryDto.ViewBattery { name = "Battery 2", countBookings = 5}
            };
            _batteryLocalizerMock.Setup(l => l["Name"]).Returns(new LocalizedString("Name", "Naam"));
            _batteryLocalizerMock.Setup(l => l["CountBookings"]).Returns(new LocalizedString("CountBookings", "Aantal Vaarten"));
            _batteryLocalizerMock.Setup(l => l["Comments"]).Returns(new LocalizedString("Comments", "Opmerkingen"));

            // Stel in dat GetAllAsync de lijst van batterijen retourneert
            _batteryServiceMock.Setup(service => service.GetAllAsync()).Returns(Task.FromResult<IEnumerable<BatteryDto.ViewBattery>>(batteries));

            // Act
            var component = RenderComponent<Batteries.Battery>();

            // Simuleer wachten tot de data geladen is
            await Task.Delay(500);

            // Assert
            var headerItems = component.FindAll("th");
            headerItems[0].InnerHtml.ShouldContain("Naam");
            headerItems[1].InnerHtml.ShouldContain("Aantal Vaarten");
            headerItems[2].InnerHtml.ShouldContain("Opmerkingen");
        }

        [Fact]
        public async Task Should_Display_Batteries_After_Data_Is_Fetched()
        {
            // Arrange
            var batteries = new List<BatteryDto.ViewBattery>
            {
                new BatteryDto.ViewBattery { name = "Battery 1", countBookings = 10},
                new BatteryDto.ViewBattery { name = "Battery 2", countBookings = 5}
            };

            // Stel in dat GetAllAsync de lijst van boten retourneert
            _batteryServiceMock.Setup(service => service.GetAllAsync()).Returns(Task.FromResult<IEnumerable<BatteryDto.ViewBattery>>(batteries));

            // Act
            var component = RenderComponent<Batteries.Battery>();

            // Simuleer wachten tot de data geladen is
            await Task.Delay(500);

            // Assert
            var boatItems = component.FindAll("tr");

            boatItems.Count.ShouldBe(3); // 1 voor de headers, 2 voor de batterijen
            boatItems[1].InnerHtml.ShouldContain("Battery 1");
            boatItems[1].InnerHtml.ShouldContain("10");

            boatItems[2].InnerHtml.ShouldContain("Battery 2");
            boatItems[2].InnerHtml.ShouldContain("5");
        }

        #region CreateBattery

        [Fact]
        public async Task Should_Display_Add_Battery_Button_Initially()
        {
            // Arrange
            _batteryServiceMock.Setup(service => service.GetAllAsync()).ReturnsAsync(new List<BatteryDto.ViewBattery>());

            // Act
            var component = RenderComponent<Batteries.Battery>();

            // Assert
            component.Find("#show-form-button").ShouldNotBeNull();
            component.FindAll("input").ShouldBeEmpty();
            component.FindAll("#submit-button").ShouldBeEmpty();
            component.FindAll("#cancel-button").ShouldBeEmpty();
        }


        [Fact]
        public async Task Should_Show_Form_When_Add_Battery_Clicked()
        {
            // Arrange
            _batteryLocalizerMock.Setup(l => l["Add"]).Returns(new LocalizedString("Add", "Add"));
            _batteryServiceMock.Setup(service => service.GetAllAsync()).ReturnsAsync(new List<BatteryDto.ViewBattery>());

            // Act
            var component = RenderComponent<Batteries.Battery>();
            Console.WriteLine(component.Markup);
            component.Find("#show-form-button").Click(); // Show the form

            // Assert
            component.Find("input").ShouldNotBeNull();
            component.Find("#submit-button").ShouldNotBeNull();
            component.Find("#cancel-button").ShouldNotBeNull();
        }

        [Fact]
        public async Task Should_Hide_Form_When_Cancel_Clicked()
        {
            // Arrange
            _batteryServiceMock.Setup(service => service.GetAllAsync()).ReturnsAsync(new List<BatteryDto.ViewBattery>());

            // Act
            var component = RenderComponent<Batteries.Battery>();
            component.Find("#show-form-button").Click(); // Show the form
            component.Find("#cancel-button").Click(); // Hide the form

            // Assert
            component.Find("#show-form-button").ShouldNotBeNull();
            component.FindAll("input").ShouldBeEmpty();
            component.FindAll("#submit-button").ShouldBeEmpty();
            component.FindAll("#cancel-button").ShouldBeEmpty();
        }



        [Fact]
        public async Task Should_Add_Battery_And_Display_In_List()
        {
            // Arrange            
            var batteries = new List<BatteryDto.ViewBattery>();
            _batteryServiceMock.Setup(service => service.GetAllAsync()).ReturnsAsync(batteries);
            _batteryServiceMock.Setup(service => service.CreateAsync(It.IsAny<BatteryDto.NewBattery>()))
                .ReturnsAsync((BatteryDto.NewBattery newBattery) => new BatteryDto.ViewBattery { name = newBattery.name })
                .Callback<BatteryDto.NewBattery>(b => batteries.Add(new BatteryDto.ViewBattery { name = b.name }));

            // Act
            var component = RenderComponent<Batteries.Battery>();
            component.Find("#show-form-button").Click(); // Show the form
            component.Find("input").Change("New Battery");
            component.Find("#submit-button").Click(); // Submit

            // Assert
            await Task.Delay(500); // Wait for async operations
            component.FindAll("tr").Count.ShouldBe(2); // 1 header + 1 added boat
            component.FindAll("tr")[1].InnerHtml.ShouldContain("New Battery");
        }

        [Fact]
        public async Task Should_Show_Error_When_Adding_Existing_Battery()
        {
            // Arrange
            var batteries = new List<BatteryDto.ViewBattery>
            {
                new BatteryDto.ViewBattery { name = "Existing Battery" }
            };
            _batteryServiceMock.Setup(service => service.GetAllAsync()).ReturnsAsync(batteries);
            _batteryServiceMock.Setup(service => service.CreateAsync(It.Is<BatteryDto.NewBattery>(b => b.name == "Existing Battery")))
                .ThrowsAsync(new Exception("Battery already exists."));

            // Act
            var component = RenderComponent<Batteries.Battery>();
            component.Find("#show-form-button").Click(); // Show the form
            component.Find("input").Change("Existing Battery");
            component.Find("#submit-button").Click(); // Submit

            // Assert
            await Task.Delay(500); // Wait for async operations
            component.Find("div.validation-message").MarkupMatches("<div class=\"validation-message\">Battery already exists.</div>");
        }

        [Fact]
        public async Task Should_Show_Error_When_Submitting_Empty_Input()
        {
            // Arrange
            _batteryServiceMock.Setup(service => service.GetAllAsync()).ReturnsAsync(new List<BatteryDto.ViewBattery>());

            // Act
            var component = RenderComponent<Batteries.Battery>();
            component.Find("#show-form-button").Click(); // Show the form
            component.Find("#submit-button").Click(); // Submit without input

            // Assert
            await Task.Delay(500); // Wait for async operations
            component.Find("div.validation-message").MarkupMatches("<div class=\"validation-message\">Name is required.</div>");
        }


        #endregion

    }
}
