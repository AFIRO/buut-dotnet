using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.Boats;
using Rise.Domain.Bookings;
using Microsoft.IdentityModel.Tokens;
using Rise.Shared.Services;

/// <summary>
/// Service for managing boat-related operations.
/// </summary>
public class BoatService : IEquipmentService<BoatDto.ViewBoat, BoatDto.NewBoat>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IValidationService _validationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoatService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="validationService">The validation service.</param>
    public BoatService(ApplicationDbContext dbContext, IValidationService validationService)
    {
        _validationService = validationService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Retrieves all boats asynchronously.
    /// </summary>
    /// <returns>A collection of boat view DTOs, or null if no boats are found.</returns>
    public async Task<IEnumerable<BoatDto.ViewBoat>?> GetAllAsync()
    {
        var query = await _dbContext.Boats.ToListAsync();
        return query.IsNullOrEmpty() ? null : query.Select(MapToDto);
    }

    /// <summary>
    /// Creates a new boat asynchronously.
    /// </summary>
    /// <param name="boat">The new boat DTO.</param>
    /// <returns>The created boat view DTO.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a boat with the same name already exists.</exception>
    public async Task<BoatDto.ViewBoat> CreateAsync(BoatDto.NewBoat boat)
    {
        if (await _validationService.BoatExists(boat.name))
        {
            throw new InvalidOperationException("There is already a boat with this name");
        }

        var newBoat = new Boat(
            name: boat.name
        );

        var dbBoat = _dbContext.Boats.Add(newBoat);
        await _dbContext.SaveChangesAsync();

        return MapToDto(dbBoat.Entity);
    }

    /// <summary>
    /// Maps a boat entity to a boat view DTO.
    /// </summary>
    /// <param name="boat">The boat entity to map.</param>
    /// <returns>The mapped boat view DTO.</returns>
    private BoatDto.ViewBoat MapToDto(Boat boat)
    {
        return new BoatDto.ViewBoat
        {
            name = boat.Name,
            countBookings = boat.CountBookings,
            listComments = boat.ListComments
        };
    }
}