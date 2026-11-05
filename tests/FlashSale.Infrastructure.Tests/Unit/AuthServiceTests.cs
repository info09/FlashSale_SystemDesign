using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace FlashSale.Infrastructure.Tests.Unit;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object,
            null!, null!, null!, null!, null!, null!, null!, null!);
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
            null!, null!, null!, null!);
        _userRepositoryMock = new Mock<IUserRepository>();
        _authService = new AuthService(_userManagerMock.Object, _signInManagerMock.Object, _userRepositoryMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WhenNewUser_ShouldReturnUser()
    {
        // Arrange
        var userName = "nguyenvana";
        var email = "a@example.com";
        var password = "Password123!";
        _userRepositoryMock
            .Setup(x => x.UserNameExistsAsync(userName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepositoryMock
            .Setup(x => x.EmailExistsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.RegisterAsync(userName, email, password);

        // Assert
        result.Should().NotBeNull();
        result.UserName.Should().Be(userName);
        result.Email.Should().Be(email);
    }

    [Fact]
    public async Task RegisterAsync_WhenDuplicateUserName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userName = "nguyenvanb";
        _userRepositoryMock
            .Setup(x => x.UserNameExistsAsync(userName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _authService.RegisterAsync(userName, "b@example.com", "Password123!");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*da ton tai*");
    }

    [Fact]
    public async Task RegisterAsync_WhenDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userName = "nguyenvanc";
        var email = "c@example.com";
        _userRepositoryMock
            .Setup(x => x.UserNameExistsAsync(userName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepositoryMock
            .Setup(x => x.EmailExistsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _authService.RegisterAsync(userName, email, "Password123!");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*da ton tai*");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenValidCredentials_ShouldReturnUser()
    {
        // Arrange
        var userName = "nguyenvand";
        var password = "Password123!";
        var appUser = new ApplicationUser(userName, "d@example.com");
        _userManagerMock
            .Setup(x => x.FindByNameAsync(userName))
            .ReturnsAsync(appUser);
        _signInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(appUser, password, false))
            .ReturnsAsync(SignInResult.Success);
        _userRepositoryMock
            .Setup(x => x.GetByUserNameAsync(userName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User(userName, "d@example.com"));

        // Act
        var result = await _authService.AuthenticateAsync(userName, password);

        // Assert
        result.Should().NotBeNull();
        result!.UserName.Should().Be(userName);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenUserNotFound_ShouldReturnNull()
    {
        // Arrange
        _userManagerMock
            .Setup(x => x.FindByNameAsync("nonexistent"))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _authService.AuthenticateAsync("nonexistent", "Password123!");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenWrongPassword_ShouldReturnNull()
    {
        // Arrange
        var userName = "nguyenvane";
        var appUser = new ApplicationUser(userName, "e@example.com");
        _userManagerMock
            .Setup(x => x.FindByNameAsync(userName))
            .ReturnsAsync(appUser);
        _signInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(appUser, "WrongPassword!", false))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await _authService.AuthenticateAsync(userName, "WrongPassword!");

        // Assert
        result.Should().BeNull();
    }
}
