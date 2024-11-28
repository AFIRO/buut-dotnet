using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using NSubstitute.ExceptionExtensions;
using Rise.Client.Auth;
using Rise.Shared.Users;
using Xunit.Abstractions;
using Shouldly;


namespace Rise.Client.Users;

public class UsersShould : TestContext
{
    public UsersShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddScoped<IUserService, FakeUserService>();
        Services.AddLocalization();

    }

    [Fact]
    public void ShouldReturnUsers()
    {
        var fakeAuthProvider = new FakeAuthenticationProvider("auth0|123456", "Admin");
        Services.AddSingleton<AuthenticationStateProvider>(fakeAuthProvider);
        var cut = RenderComponent<Users>();
        var allUsersTableRows = cut.FindAll("table.table:nth-of-type(1) tbody tr");
        allUsersTableRows.Count.ShouldBe(5);   
    }
}