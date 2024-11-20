using Moq;
using Rise.Shared.Boats;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using Microsoft.Extensions.Localization;
using System;

namespace Rise.Client.Tests
{
    public class BoatsTest : TestContext
    {
        private Mock<IBoatService> _boatServiceMock;
        private Mock<IStringLocalizer<Boats.Boat>> _localizerMock;

        public BoatsTest()
        {
            // Maak een mock van IBoatService met Moq
            _boatServiceMock = new Mock<IBoatService>();
            _localizerMock = new Mock<IStringLocalizer<Boats.Boat>>();

            // Voeg de mock toe aan de dependency injection container
            Services.AddSingleton(_boatServiceMock.Object);
            Services.AddSingleton(_localizerMock.Object);
        }

        [Fact]
        public async Task Should_Display_Title()
        {
            // Arrange
             _localizerMock.Setup(l => l["Title"]).Returns(new LocalizedString("Title", "Botenlijst"));     

            // Act
            var component = RenderComponent<Boats.Boat>();

            // Assert
            component.Find("h1").MarkupMatches("<h1 class=\"text-white\">Botenlijst</h3>");            
        }

        [Fact]
        public async Task Should_Display_Loading_While_Fetching_Data()
        {
            // Arrange
            // Stel in dat GetAllAsync null retourneert om de loading status te triggeren
            _boatServiceMock.Setup(service => service.GetAllAsync()).Returns(Task.FromResult<IEnumerable<BoatDto.ViewBoat>>(null));
            _localizerMock.Setup(l => l["Loading"]).Returns(new LocalizedString("Loading", "Boten aan het ophalen..."));


            // Act
            var component = RenderComponent<Boats.Boat>();

            // Assert
            component.Find("span").MarkupMatches("<span>Boten aan het ophalen...</span>");
        }

        [Fact]
        public async Task Should_Display_Table_Header_After_Data_Is_Fetched()
        {
            // Arrange
            var boats = new List<BoatDto.ViewBoat>
            {
                new BoatDto.ViewBoat { name = "Boat 1", countBookings = 10},
                new BoatDto.ViewBoat { name = "Boat 2", countBookings = 5}
            };
            _localizerMock.Setup(l => l["Name"]).Returns(new LocalizedString("Name", "Naam"));
            _localizerMock.Setup(l => l["CountBookings"]).Returns(new LocalizedString("CountBookings", "Aantal Vaarten"));
            _localizerMock.Setup(l => l["Comments"]).Returns(new LocalizedString("Comments", "Opmerkingen"));

            // Stel in dat GetAllAsync de lijst van boten retourneert
            _boatServiceMock.Setup(service => service.GetAllAsync()).Returns(Task.FromResult<IEnumerable<BoatDto.ViewBoat>>(boats));

            // Act
            var component = RenderComponent<Boats.Boat>();

            // Simuleer wachten tot de data geladen is
            await Task.Delay(500); // In real apps, use async lifecycle methods to wait.

            // Assert
            var headerItems = component.FindAll("th");
            headerItems[0].InnerHtml.ShouldContain("Naam");
            headerItems[1].InnerHtml.ShouldContain("Aantal Vaarten");
            headerItems[2].InnerHtml.ShouldContain("Opmerkingen");
        }

        [Fact]
        public async Task Should_Display_Boats_After_Data_Is_Fetched()
        {
            // Arrange
            var boats = new List<BoatDto.ViewBoat>
            {
                new BoatDto.ViewBoat { name = "Boat 1", countBookings = 10},
                new BoatDto.ViewBoat { name = "Boat 2", countBookings = 5}
            };

            // Stel in dat GetAllAsync de lijst van boten retourneert
            _boatServiceMock.Setup(service => service.GetAllAsync()).Returns(Task.FromResult<IEnumerable<BoatDto.ViewBoat>>(boats));

            // Act
            var component = RenderComponent<Boats.Boat>();

            // Simuleer wachten tot de data geladen is
            await Task.Delay(500); // In real apps, use async lifecycle methods to wait.

            // Assert
            var boatItems = component.FindAll("tr");

            boatItems.Count.ShouldBe(3); // 1 voor de headers, 2 voor de boten
            boatItems[1].InnerHtml.ShouldContain("Boat 1");
            boatItems[1].InnerHtml.ShouldContain("10");

            boatItems[2].InnerHtml.ShouldContain("Boat 2");
            boatItems[2].InnerHtml.ShouldContain("5");
        }

        #region CreateBoat

        [Fact]
        public async Task Should_Display_Add_Boat_Button_Initially()
        {
            // Arrange
            _boatServiceMock.Setup(service => service.GetAllAsync()).ReturnsAsync(new List<BoatDto.ViewBoat>());

            // Act
            var component = RenderComponent<Boats.Boat>();

            // Assert
            component.Find("button.btn-primary").ShouldNotBeNull();
            component.FindAll("input").ShouldBeEmpty();
            component.FindAll("button.btn-success").ShouldBeEmpty();
            component.FindAll("button.btn-danger").ShouldBeEmpty();
        }


        [Fact]
        public async Task Should_Show_Form_When_Add_Boat_Clicked()
        {
            // Arrange
            _boatServiceMock.Setup(service => service.GetAllAsync()).ReturnsAsync(new List<BoatDto.ViewBoat>());

            // Act
            var component = RenderComponent<Boats.Boat>();
            component.Find("button.btn-primary").Click(); // Show the form

            // Assert
            component.Find("input").ShouldNotBeNull();
            component.Find("button.btn-success").ShouldNotBeNull();
            component.Find("button.btn-danger").ShouldNotBeNull();
        }

        [Fact]
        public async Task Should_Hide_Form_When_Cancel_Clicked()
        {
            // Arrange
            _boatServiceMock.Setup(service => service.GetAllAsync()).ReturnsAsync(new List<BoatDto.ViewBoat>());

            // Act
            var component = RenderComponent<Boats.Boat>();
            component.Find("button.btn-primary").Click(); // Show the form
            component.Find("button.btn-danger").Click(); // Hide the form

            // Assert
            component.Find("button.btn-primary").ShouldNotBeNull();
            component.FindAll("input").ShouldBeEmpty();
            component.FindAll("button.btn-success").ShouldBeEmpty();
            component.FindAll("button.btn-danger").ShouldBeEmpty();
        }



        [Fact]
        public async Task Should_Add_Boat_And_Display_In_List()
        {
            // Arrange
            var boats = new List<BoatDto.ViewBoat>();
            _boatServiceMock.Setup(service => service.GetAllAsync()).ReturnsAsync(boats);
            _boatServiceMock.Setup(service => service.CreateBoatAsync(It.IsAny<BoatDto.NewBoat>()))
                .ReturnsAsync((BoatDto.NewBoat newBoat) => new BoatDto.ViewBoat { name = newBoat.name })
                .Callback<BoatDto.NewBoat>(b => boats.Add(new BoatDto.ViewBoat { name = b.name }));

            // Act
            var component = RenderComponent<Boats.Boat>();
            component.Find("button.btn-primary").Click(); // Show the form
            component.Find("input").Change("New Boat");
            component.Find("button.btn-success").Click(); // Submit

            // Assert
            await Task.Delay(500); // Wait for async operations
            component.FindAll("tr").Count.ShouldBe(2); // 1 header + 1 added boat
            component.FindAll("tr")[1].InnerHtml.ShouldContain("New Boat");
        }

        [Fact]
        public async Task Should_Show_Error_When_Adding_Existing_Boat()
        {
            // Arrange
            var boats = new List<BoatDto.ViewBoat>
            {
                new BoatDto.ViewBoat { name = "Existing Boat" }
            };
            _boatServiceMock.Setup(service => service.GetAllAsync()).ReturnsAsync(boats);
            _boatServiceMock.Setup(service => service.CreateBoatAsync(It.Is<BoatDto.NewBoat>(b => b.name == "Existing Boat")))
                .ThrowsAsync(new Exception("Boat already exists."));

            // Act
            var component = RenderComponent<Boats.Boat>();
            component.Find("button.btn-primary").Click(); // Show the form
            component.Find("input").Change("Existing Boat");
            component.Find("button.btn-success").Click(); // Submit

            // Assert
            await Task.Delay(500); // Wait for async operations
            component.Find("div.validation-message").MarkupMatches("<div class=\"validation-message\">Boat already exists.</div>");
        }

        [Fact]
        public async Task Should_Show_Error_When_Submitting_Empty_Input()
        {
            // Arrange
            _boatServiceMock.Setup(service => service.GetAllAsync()).ReturnsAsync(new List<BoatDto.ViewBoat>());

            // Act
            var component = RenderComponent<Boats.Boat>();
            component.Find("button.btn-primary").Click(); // Show the form
            component.Find("button.btn-success").Click(); // Submit without input

            // Assert
            await Task.Delay(500); // Wait for async operations
            component.Find("div.validation-message").MarkupMatches("<div class=\"validation-message\">Name is required.</div>");
        }


        #endregion

    }
}
