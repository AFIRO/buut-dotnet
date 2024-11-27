public interface IEquipmentService<TView, TNew>
{
    Task<TView> CreateAsync(TNew equipment);
    Task<IEnumerable<TView>?> GetAllAsync();
}