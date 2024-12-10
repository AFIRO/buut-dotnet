using Rise.Shared.Boats;

public interface IEquipmentService<TView, TNew, TUpdate>
{
    Task<TView> CreateAsync(TNew equipment);
    Task<IEnumerable<TView>?> GetAllAsync();
    Task<bool> UpdateAsync(TUpdate equipment);
    Task<bool> DeleteAsync(string equipmentId);

}