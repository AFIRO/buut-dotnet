
using Rise.Shared.Boats;

public interface IBoatService
{
    Task<BoatDto.ViewBoat> CreateBoatAsync(BoatDto.NewBoat boat);
    Task<IEnumerable<BoatDto.ViewBoat>?> GetAllAsync();
}