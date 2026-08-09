using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Admin;
using Ahk.Web.Server.Admin.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// The admin rule that a Neptun code is either empty or unique across users, exercised through
/// <see cref="UsersAdminController"/> over a real <see cref="UserManager{TUser}"/> on the in-memory store.
/// (EF InMemory does not enforce the filtered unique index, so this proves the controller's own guard —
/// the database index is the backstop in production.)
/// </summary>
public sealed class UserNeptunTests : IDisposable
{
    private readonly ApplicationDbContext db;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly UsersAdminController controller;

    public UserNeptunTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"UserNeptunTests-{Guid.NewGuid()}")
            .Options;
        this.db = new ApplicationDbContext(options, new NoCourse());

        var store = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, int>(this.db);
        this.userManager = new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services: null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        this.controller = new UsersAdminController(this.db, this.userManager);
    }

    [Fact]
    public async Task Create_StoresNeptunUppercased()
    {
        var result = await this.controller.Create(NewUser("alice", neptun: "abc123"), default);

        var dto = Assert.IsType<UserDto>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
        Assert.Equal("ABC123", dto.NeptunCode);
        Assert.Equal("ABC123", (await this.userManager.FindByNameAsync("alice"))!.NeptunCode);
    }

    [Fact]
    public async Task Create_DuplicateNeptun_IsRejected()
    {
        await this.controller.Create(NewUser("alice", neptun: "ABC123"), default);

        // Same code, different casing/whitespace — normalization must make them collide.
        var result = await this.controller.Create(NewUser("bob", neptun: " abc123 "), default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Null(await this.userManager.FindByNameAsync("bob")); // not created
    }

    [Fact]
    public async Task Create_BlankNeptun_StoredAsNull_AndMayRepeat()
    {
        await this.controller.Create(NewUser("alice", neptun: ""), default);
        var result = await this.controller.Create(NewUser("bob", neptun: "   "), default);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Null((await this.userManager.FindByNameAsync("alice"))!.NeptunCode);
        Assert.Null((await this.userManager.FindByNameAsync("bob"))!.NeptunCode);
    }

    [Fact]
    public async Task Update_ToNeptunHeldByAnother_IsRejected()
    {
        await this.controller.Create(NewUser("alice", neptun: "ALICE1"), default);
        await this.controller.Create(NewUser("bob", neptun: "BOB1"), default);
        var bob = (await this.userManager.FindByNameAsync("bob"))!;

        var result = await this.controller.Update(bob.Id, new UpdateUserRequest { NeptunCode = "alice1" }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("BOB1", (await this.userManager.FindByNameAsync("bob"))!.NeptunCode); // unchanged
    }

    [Fact]
    public async Task Update_KeepingOwnNeptun_Succeeds()
    {
        await this.controller.Create(NewUser("alice", neptun: "ALICE1"), default);
        var alice = (await this.userManager.FindByNameAsync("alice"))!;

        var result = await this.controller.Update(alice.Id, new UpdateUserRequest { NeptunCode = "alice1" }, default);

        Assert.IsType<UserDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("ALICE1", (await this.userManager.FindByNameAsync("alice"))!.NeptunCode);
    }

    [Fact]
    public async Task Update_ClearingNeptun_StoresNull()
    {
        await this.controller.Create(NewUser("alice", neptun: "ALICE1"), default);
        var alice = (await this.userManager.FindByNameAsync("alice"))!;

        await this.controller.Update(alice.Id, new UpdateUserRequest { NeptunCode = "" }, default);

        Assert.Null((await this.userManager.FindByNameAsync("alice"))!.NeptunCode);
    }

    private static CreateUserRequest NewUser(string userName, string? neptun) => new()
    {
        UserName = userName,
        Password = "Passw0rd!",
        NeptunCode = neptun,
    };

    public void Dispose()
    {
        this.userManager.Dispose();
        this.db.Dispose();
    }

    /// <summary>Course provider that resolves no course; <see cref="ApplicationUser"/> is not course-scoped, so
    /// the value is irrelevant here.</summary>
    private sealed class NoCourse : ICurrentCourseProvider
    {
        public int? CurrentCourseId => null;
    }
}
