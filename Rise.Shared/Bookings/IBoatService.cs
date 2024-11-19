
using Rise.Shared.Boats;

public interface IBoatService
{
    Task<IEnumerable<BoatDto.ViewBoat>?> GetAllAsync();
}