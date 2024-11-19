using Microsoft.AspNetCore.Components;
using Rise.Shared.Boats;

namespace Rise.Client.Boats;
public partial class Boat
{
    [Inject] public required IBoatService BoatService { get; set; }

    private IEnumerable<BoatDto.ViewBoat>? _boats;

    protected override async Task OnInitializedAsync()
    {
        _boats = await BoatService.GetAllAsync();
    }
}