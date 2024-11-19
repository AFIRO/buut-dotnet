using Moq;
using Rise.Shared.Boats;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using Microsoft.Extensions.Localization;

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
    }
}
