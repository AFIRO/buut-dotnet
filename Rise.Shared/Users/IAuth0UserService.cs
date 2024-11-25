namespace Rise.Shared.Users;

public interface IAuth0UserService
{
    Task<IEnumerable<UserDto.Auth0User>> GetAllUsersAsync();
    Task<UserDto.Auth0User> GetUserByIdAsync(String id);
    Task<UserDto.RegistrationUser> RegisterUserAuth0(UserDto.RegistrationUser user);
    Task<bool> UpdateUserAuth0(UserDto.UpdateUser user);
    Task<bool> AssignRoleToUser(UserDto.UpdateUser user);
    Task<bool> IsEmailTakenAsync(String email);
    Task<bool> SoftDeleteAuth0UserAsync(string userid);
}