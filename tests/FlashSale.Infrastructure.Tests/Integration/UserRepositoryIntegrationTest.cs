using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Xunit;

namespace FlashSale.Infrastructure.Tests.Integration;

public class UserRepositoryIntegrationTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private PostgreSQLPrimaryContext _context = null!;
    private UserManager<ApplicationUser> _userManager = null!;
    private UserRepository _userRepository = null!;

    public UserRepositoryIntegrationTest()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("flashsale")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        var connectionString = _postgresContainer.GetConnectionString() + ";Include Error Detail=true";
        var options = new DbContextOptionsBuilder<PostgreSQLPrimaryContext>()
            .UseNpgsql(connectionString)
            .Options;
        _context = new PostgreSQLPrimaryContext(options);
        await _context.Database.EnsureCreatedAsync();

        var store = new UserStore<ApplicationUser, IdentityRole<Guid>, PostgreSQLPrimaryContext, Guid>(_context);
        _userManager = new UserManager<ApplicationUser>(
            store,
            new OptionsWrapper<IdentityOptions>(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Enumerable.Empty<IUserValidator<ApplicationUser>>(),
            Enumerable.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null,
            new Logger<UserManager<ApplicationUser>>(new LoggerFactory())
        );
        _userRepository = new UserRepository(_userManager);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task CreateWithPasswordAsync_WhenNewUser_ShouldCreateUserAndGetByUserName()
    {
        // Arrange
        var user = new User("nguyenvana", "nguyenvan@example.com");

        // Act
        await _userRepository.CreateWithPasswordAsync(user, "Password123!", CancellationToken.None);
        var result = await _userRepository.GetByUserNameAsync("nguyenvana", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.UserName.Should().Be("nguyenvana");
        result.Email.Should().Be("nguyenvan@example.com");
        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateWithPasswordAsync_WhenDuplicateUserName_ShouldThrowDbUpdateException()
    {
        // Arrange
        await _userRepository.CreateWithPasswordAsync(new User("nguyenvanb", "b@example.com"), "Password123!", CancellationToken.None);

        // Act
        var act = () => _userRepository.CreateWithPasswordAsync(new User("nguyenvanb", "b2@example.com"), "Password123!", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task UserNameExistsAsync_WhenExists_ShouldReturnTrue()
    {
        // Arrange
        await _userRepository.CreateWithPasswordAsync(new User("nguyenvanc", "c@example.com"), "Password123!", CancellationToken.None);

        // Act
        var result = await _userRepository.UserNameExistsAsync("nguyenvanc", CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_WhenExists_ShouldReturnTrue()
    {
        // Arrange
        await _userRepository.CreateWithPasswordAsync(new User("nguyenvand", "d@example.com"), "Password123!", CancellationToken.None);

        // Act
        var result = await _userRepository.EmailExistsAsync("d@example.com", CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckPasswordAsync_WhenPasswordCorrect_ShouldReturnTrue()
    {
        // Arrange
        var appUser = new ApplicationUser("nguyenvane", "e@example.com");
        await _userManager.CreateAsync(appUser, "Password123!");

        // Act
        var result = await _userManager.CheckPasswordAsync(appUser, "Password123!");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckPasswordAsync_WhenPasswordWrong_ShouldReturnFalse()
    {
        // Arrange
        var appUser = new ApplicationUser("nguyenvanaf", "f@example.com");
        await _userManager.CreateAsync(appUser, "Password123!");

        // Act
        var result = await _userManager.CheckPasswordAsync(appUser, "WrongPassword!");

        // Assert
        result.Should().BeFalse();
    }
}
