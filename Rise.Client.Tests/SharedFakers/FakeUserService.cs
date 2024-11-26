using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Linq;
using Rise.Shared.Enums;
using Rise.Shared.Users;

namespace Rise.Client.Users;

public class FakeUserService : IUserService
{
    readonly ImmutableList<RoleDto> roles = [new() { Name = RolesEnum.User }];
    // public Task<bool> CreateUserAsync(UserDto.CreateUser userDetails)
    // {
    //     return Task.FromResult(true);
    // }

    // public Task<bool> DeleteUserAsync(int id)
    // {
    //     return Task.FromResult(true);
    // }

    // public Task<IEnumerable<UserDto.GetUser>> GetAllAsync()
    // {
    //     var users = Enumerable.Range(1, 5).Select(i => new UserDto.GetUser { Id = i, FirstName = $"First {i}", LastName = $"Last {i}", Email = $"test{i}@test.com", Roles = roles, IsDeleted = false });
    //     return Task.FromResult(users);
    // }

    public Task<(Boolean, string?)> CreateUserAsync(UserDto.RegistrationUser userDetails)
    {
        return Task.FromResult((true, "User created successfully"));
    }

    public Task<Boolean> UpdateUserAsync(UserDto.UpdateUser userDetails)
    {
        throw new NotImplementedException();
    }

    public Task<Boolean> SoftDeleteUserAsync(string id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<UserDto.Auth0User>> GetAuth0Users()
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateUserRolesAsync(string userId, ImmutableList<RoleDto> newRoles)
    {
        throw new NotImplementedException();
    }

    public Task<Boolean> IsEmailTakenAsync(string email)
    {
        throw new NotImplementedException();
    }

    Task<IEnumerable<UserDto.UserBase>?> IUserService.GetAllAsync()
    {
        var users = Enumerable.Range(1, 5).Select(i => new UserDto.UserBase 
            (i.ToString(), $"First {i}", $"Last {i}", $"test{i}@test.com", roles) );
        return Task.FromResult(users);
    }

    public Task<UserDto.UserBase?> GetUserAsync()
    {
        var user = new UserDto.UserBase ("1", "First", "Last", "test@test.com", roles );
        return Task.FromResult(user ?? null);
    }

    public Task<UserDto.UserBase?> GetUserByIdAsync(string id)
    {
        var user = new UserDto.UserBase ( id,  $"First {id}", $"Last {id}", $"test{id}@test.com", roles );
        return Task.FromResult(user ?? null);
    }

    public Task<IEnumerable<UserDto.UserBase>> GetFilteredUsersAsync(UserFilter filter)
    {
        throw new NotImplementedException();
    }

    public Task<UserDto.UserDetails?> GetUserDetailsByIdAsync(string id)
    {
        var userDetails = new UserDto.UserDetails(id, $"First {id}", $"Last {id}", $"test{id}@test.com","+32478457845",new AddressDto.GetAdress { Street = StreetEnum.AFRIKALAAN, HouseNumber = "1" },
            roles, DateTime.Now);
        return Task.FromResult(userDetails ?? null);
    }

    // public Task<bool> UpdateUserAsync(int id, UserDto.UpdateUser userDetails)
    // {
    //     return Task.FromResult(true);
    // }
}
