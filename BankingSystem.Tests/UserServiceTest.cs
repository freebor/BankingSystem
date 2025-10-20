using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using FluentAssertions;
using BCrypt.Net;
using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using BankingSystem.Services;            // adjust if your service namespace differs
using BankingSystem.Services.Interface; // adjust as needed

using Xunit;
using Moq;
using BankingSystem.Services;
using BankingSystem.Repository.Interface;
using BankingSystem.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BankingSystem.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly IUserService _userService;

        public UserServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();

            // Assuming your UserService constructor is: UserService(IUserRepository userRepository)
            // If it differs, update this line accordingly.
            _userService = new UserService(_userRepoMock.Object);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldHashPassword_AndCallRepository()
        {
            // Arrange
            var plainPassword = "P@ssw0rd!";
            var incoming = new User
            {
                UserName = "freedom",
                Email = "freedom@example.com",
                PasswordHash = plainPassword // service should hash this field before saving
            };

            User savedUser = null!;
            _userRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) =>
                {
                    savedUser = u;
                    return u;
                });

            // Act
            var result = await _userService.CreateUserAsync(incoming);

            // Assert
            _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);

            // Ensure returned user is not null and has an Id
            result.Should().NotBeNull();
            result.Id.Should().NotBe(Guid.Empty);

            // PasswordHash on savedUser should be hashed and not equal to plaintext
            savedUser.Should().NotBeNull();
            savedUser.PasswordHash.Should().NotBe(plainPassword);

            // Verify the hash actually matches the plaintext using BCrypt
            BCrypt.Net.BCrypt.Verify(plainPassword, savedUser.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnUser_WhenExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var expected = new User
            {
                Id = id,
                UserName = "freedom",
                Email = "freedom@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret")
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(expected);

            // Act
            var actual = await _userService.GetByIdAsync(id);

            // Assert
            actual.Should().NotBeNull();
            actual!.Id.Should().Be(id);
            actual.Email.Should().Be(expected.Email);
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnUser_WhenExists()
        {
            // Arrange
            var email = "freedom@example.com";
            var expected = new User
            {
                Id = Guid.NewGuid(),
                UserName = "freedom",
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret")
            };

            _userRepoMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(expected);

            // Act
            var actual = await _userService.GetByEmailAsync(email);

            // Assert
            actual.Should().NotBeNull();
            actual!.Email.Should().Be(email);
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldCallRepositoryDelete_WhenUserExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            _userRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new User { Id = id });
            _userRepoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _userService.DeleteUserAsync(id);

            // Assert
            result.Should().BeTrue();
            _userRepoMock.Verify(r => r.DeleteAsync(id), Times.Once);
        }
    }
}
