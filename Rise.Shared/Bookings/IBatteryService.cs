using Rise.Shared.Bookings;
using Rise.Shared.Users;

namespace Rise.Shared.Batteries
{
    /// <summary>
    /// Defines the contract for battery-related operations.
    /// </summary>
    public interface IBatteryService
        : IEquipmentService<BatteryDto.ViewBattery, BatteryDto.NewBattery>
    {
        
        /// <summary>
        /// Changes the holder of the battery to the given godparent
        /// </summary>
        /// <param name="godparentId">The ID of the <see cref="User"/> who is the godparent.</param>
        /// <param name="batteryId">The ID of the <see cref="Battery"/> the godparent wants to claim.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the <see cref="UserDto.UserContactDetails"/> of batteries holder,
        /// or <c>null</c> if the battery is not found or the given battery is not a child of the godparent.
        /// </returns>
        Task<UserDto.UserContactDetails?> ClaimBatteryAsGodparentAsync(string godparentId, string batteryId);

        /// <summary>
        /// Retrieves a battery where the specified user is the godparent.
        /// </summary>
        /// <param name="godparentId">The ID of the user who is the godparent.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the battery details,
        /// or <c>null</c> if no battery is found for the specified godparent.
        /// </returns>
        Task<BatteryDto.ViewBatteryBuutAgent?> GetBatteryByGodparentUserIdAsync(string godparentId);

        /// <summary>
        /// Retrieves contact information of the holder of a battery of which the specified user is the godparent.
        /// </summary>
        /// <param name="godparentId">The ID of the user who is the godparent.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the contact information of the holder,
        /// or <c>null</c> if no holder is found for the specified godparent's battery or the godparent does not have a battery.
        /// </returns>
        Task<UserDto.UserContactDetails?> GetBatteryHolderByGodparentUserIdAsync(string godparentId);
    }
}
