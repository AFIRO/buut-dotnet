using Rise.Domain.Bookings;
using Rise.Shared.Enums;
using Rise.Domain.Users;

namespace Rise.Persistence;

/// <summary>
/// Responsible for seeding the database with initial data.
/// </summary>
public class Seeder
{
    private readonly ApplicationDbContext dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="Seeder"/> class with a specified <see cref="ApplicationDbContext"/>.
    /// </summary>
    /// <param name="dbContext">The database context used for seeding.</param>
    public Seeder(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    /// <summary>
    /// Seeds the database with initial data if it has not been seeded already.
    /// </summary>
    public void Seed()
    {
        if (!BoatsHasAlreadyBeenSeeded())
        {
            DropBookings();
            SeedBoats();
        }

        if (!BatteriesHasAlreadyBeenSeeded())
        {
            DropBookings();
            SeedBatteries();
        }
        if (!UsersHasAlreadyBeenSeeded())
            SeedUsers();
        if (!BookingsHasAlreadyBeenSeeded())
            SeedBookings();
    }
    
    /// <summary>
    /// Checks if the database has already been seeded with users.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the database already contains user entries; otherwise, <c>false</c>.
    /// </returns>
    private bool UsersHasAlreadyBeenSeeded()
    {
        return dbContext.Users.Any();
    }

    private bool BookingsHasAlreadyBeenSeeded()
    {
        return dbContext.Bookings.Any();
    }

    private bool BoatsHasAlreadyBeenSeeded()
    {
        return dbContext.Boats.Any();
    }

    private bool BatteriesHasAlreadyBeenSeeded()
    {
        return dbContext.Batteries.Any();
    }

    private bool DropUsers()
    {
        dbContext.Users.RemoveRange(dbContext.Users.AsEnumerable());
        return true;
    }

    private bool DropBookings()
    {
        dbContext.Bookings.RemoveRange(dbContext.Bookings.AsEnumerable());
        return true;
    }

    /// <summary>
    /// Seeds the database with 2 user entities.
    /// </summary>
    private void SeedUsers()
    {
        var roleAdmin = new Role(RolesEnum.Admin);
        var roleUser = new Role(RolesEnum.User);
        var roleGodparent = new Role(RolesEnum.Godparent);
        var rolePending = new Role(RolesEnum.Pending);
        var userAdmin = new User("auth0|6713ad524e8a8907fbf0d57f", "Admin", "Gebruiker", "admin@hogent.be",
            new DateTime(1980, 01, 01), new Address("Afrikalaan", "5"), "+32478457845");
        // userAdmin.AddRole(new Role(RolesEnum.Admin));
        // dbContext.Users.Add(userAdmin);
        var userGodparent = new User("auth0|6713ad784fda04f4b9ae2165", "GodParent", "Gebruiker", "godparent@hogent.be",
            new DateTime(1986, 09, 27), new Address("Bataviabrug", "35"), "+32478471869");
        // userUser.AddRole(new Role());
        // dbContext.Users.Add(userUser);
        var userUser = new User("auth0|6713ad614fda04f4b9ae2156", "User", "Gebruiker", "user@hogent.be",
            new DateTime(1990, 05, 16), new Address("Deckerstraat", "4"), "+32474771836");
        // userGodparent.AddRole(new Role(RolesEnum.Godparent));
        // dbContext.Users.Add(userGodparent);
        var userPending = new User("auth0|6713adbf2d2a7c11375ac64c", "Pending", "Gebruiker", "pending@hogent.be",
            new DateTime(1990, 05, 16), new Address("Deckerstraat", "4"), "+32474771836");
        // userPending.AddRole(new Role(RolesEnum.Pending));

        userAdmin.Roles.Add(roleAdmin);
        userAdmin.Roles.Add(roleUser);
        userUser.Roles.Add(roleUser);
        userGodparent.Roles.Add(roleGodparent);
        userGodparent.Roles.Add(roleUser);
        userPending.Roles.Add(rolePending);
        dbContext.Users.AddRange(userAdmin, userUser, userGodparent, userPending);
        dbContext.Roles.AddRange(roleAdmin, roleUser, roleGodparent, rolePending);
        dbContext.SaveChanges();
    }

    private void SeedBookings()
    {
        // // temp seed bookings (Andries)
        var bookings = new List<Booking>
        {
            new Booking(new DateTime(2023, 01, 01), "auth0|6713ad614fda04f4b9ae2156", TimeSlot.Morning),
            new Booking(new DateTime(2023, 01, 02), "auth0|6713ad614fda04f4b9ae2156", TimeSlot.Evening),
            new Booking(new DateTime(2023, 01, 03), "auth0|6713ad614fda04f4b9ae2156", TimeSlot.Afternoon),
            new Booking(new DateTime(2023, 01, 04), "auth0|6713ad614fda04f4b9ae2156", TimeSlot.Afternoon),
            new Booking(new DateTime(2023, 01, 05), "auth0|6713ad614fda04f4b9ae2156", TimeSlot.Evening),
            new Booking(new DateTime(2023, 01, 06), "auth0|6713ad614fda04f4b9ae2156", TimeSlot.Evening),
            new Booking(new DateTime(2023, 01, 07), "auth0|6713ad614fda04f4b9ae2156", TimeSlot.Morning),
            new Booking(new DateTime(2023, 01, 08), "auth0|6713ad614fda04f4b9ae2156", TimeSlot.Afternoon),
            new Booking(new DateTime(2023, 01, 09), "auth0|6713ad614fda04f4b9ae2156", TimeSlot.Evening),
            new Booking(new DateTime(2023, 01, 10), "auth0|6713ad614fda04f4b9ae2156", TimeSlot.Morning)
        };

        foreach(var booking in bookings){
            dbContext.Bookings.Add(booking);
        }
        dbContext.SaveChanges();
        
        var booking1 = new Booking(new DateTime(2025, 01, 01), "auth0|6713ad614fda04f4b9ae2156", TimeSlot.Morning);
        dbContext.Bookings.Add(booking1);
        var bookingBattery = new Booking(new DateTime(2023, 01, 01), "auth0|6713ad614fda04f4b9ae2156", TimeSlot.Morning);
        bookingBattery.AddBattery(dbContext.Batteries.First());
        dbContext.Bookings.Add(bookingBattery);
        var bookingBoat = new Booking(new DateTime(2022, 01, 01), "auth0|6713ad614fda04f4b9ae2156", TimeSlot.Morning);
        bookingBoat.AddBoat(dbContext.Boats.First());
        dbContext.Bookings.Add(bookingBoat);
        var bookingAll = new Booking(new DateTime(2021, 01, 01), "auth0|6713ad614fda04f4b9ae2156", TimeSlot.Morning);
        bookingAll.AddBattery(dbContext.Batteries.OrderBy(battery => battery.Name).Last());
        bookingAll.AddBoat(dbContext.Boats.OrderBy(boat => boat.Name).Last());
        dbContext.Bookings.Add(bookingAll);
        dbContext.SaveChanges();
    }

    private void SeedBoats()
    {
        var boats = new List<Boat>
        {
            new Boat("Leith"),
            new Boat("Lubeck"),
            new Boat("Limba")
        };

        boats.ForEach(boat => dbContext.Boats.Add(boat));
        dbContext.SaveChanges();        
    }

    private void SeedBatteries()
    {
        for (int i = 1; i <= 10; i++)
        {
            dbContext.Batteries.Add(new Battery("Battery" + i));
        }
        dbContext.SaveChanges();
    }
}