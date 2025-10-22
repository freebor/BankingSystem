using Moq;
using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using BankingSystem.Models.Dto;
using Microsoft.Extensions.Caching.Distributed;
using BankingSystem.Services;
using System.Text.Json;
using System.Text; // adjust as needed

namespace BankingSystem.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockUserRepo = new Mock<IUserRepository>();
            _mockCache = new Mock<IDistributedCache>();
            _userService = new UserService(_mockUserRepo.Object, _mockCache.Object);
        }

        #region CreateUserAsync Tests

        [Fact]
        public async Task CreateUserAsync_ShouldCreateUser_AndCacheIt()
        {
            // Arrange
            var registerDto = new RegisterUserDto
            {
                UserName = "testuser",
                Email = "test@example.com",
                Password = "password123"
            };

            var expectedUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow
            };

            _mockUserRepo.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
                .ReturnsAsync(expectedUser);

            _mockCache.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockCache.Setup(x => x.RemoveAsync(
                "all_users",
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _userService.CreateUserAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(registerDto.UserName, result.UserName);
            Assert.Equal(registerDto.Email, result.Email);
            _mockUserRepo.Verify(x => x.CreateUserAsync(It.IsAny<User>()), Times.Once);
            _mockCache.Verify(x => x.SetAsync(
                It.Is<string>(s => s.Contains("user:")),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(x => x.RemoveAsync("all_users", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldHashPassword()
        {
            // Arrange
            var registerDto = new RegisterUserDto
            {
                UserName = "testuser",
                Email = "test@example.com",
                Password = "password123"
            };

            User capturedUser = null;
            _mockUserRepo.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .ReturnsAsync((User u) => u);

            // Act
            await _userService.CreateUserAsync(registerDto);

            // Assert
            Assert.NotNull(capturedUser);
            Assert.NotEqual(registerDto.Password, capturedUser.PasswordHash);
            Assert.False(string.IsNullOrEmpty(capturedUser.PasswordHash));
        }

        #endregion

        #region GetUserByIdAsync Tests

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnCachedUser_WhenCacheExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cachedUser = new User
            {
                Id = userId,
                UserName = "testuser",
                Email = "test@example.com"
            };

            var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedUser));
            _mockCache.Setup(x => x.GetAsync($"user:{userId}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal(cachedUser.UserName, result.UserName);
            _mockUserRepo.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldQueryRepository_WhenCacheIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                UserName = "testuser",
                Email = "test@example.com"
            };

            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            _mockUserRepo.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            _mockUserRepo.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _mockCache.Verify(x => x.SetAsync(
                It.Is<string>(s => s.Contains("user:")),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnNull_WhenUserNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            _mockUserRepo.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User)null);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.Null(result);
            _mockUserRepo.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        #endregion

        #region GetUserByEmailAsync Tests

        [Fact]
        public async Task GetUserByEmailAsync_ShouldReturnCachedUser_WhenCacheExists()
        {
            // Arrange
            var email = "test@example.com";
            var cachedUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = "testuser",
                Email = email
            };

            var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedUser));
            _mockCache.Setup(x => x.GetAsync($"user_email:{email.ToLower()}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _userService.GetUserByEmailAsync(email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(email, result.Email);
            _mockUserRepo.Verify(x => x.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetUserByEmailAsync_ShouldQueryRepository_WhenCacheIsEmpty()
        {
            // Arrange
            var email = "test@example.com";
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "testuser",
                Email = email
            };

            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            _mockUserRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.GetUserByEmailAsync(email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(email, result.Email);
            _mockUserRepo.Verify(x => x.GetByEmailAsync(email), Times.Once);
        }

        #endregion

        #region GetAllUserAsync Tests

        [Fact]
        public async Task GetAllUserAsync_ShouldReturnCachedUsers_WhenCacheExists()
        {
            // Arrange
            var users = new List<User>
                {
                    new User { Id = Guid.NewGuid(), UserName = "user1", Email = "user1@test.com" },
                    new User { Id = Guid.NewGuid(), UserName = "user2", Email = "user2@test.com" }
                };

            var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(users));
            _mockCache.Setup(x => x.GetAsync("all_users", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _userService.GetAllUserAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockUserRepo.Verify(x => x.GetAllAsync(), Times.Never);
        }

        [Fact]
        public async Task GetAllUserAsync_ShouldQueryRepository_WhenCacheIsEmpty()
        {
            // Arrange
            var users = new List<User>
                {
                    new User { Id = Guid.NewGuid(), UserName = "user1", Email = "user1@test.com" },
                    new User { Id = Guid.NewGuid(), UserName = "user2", Email = "user2@test.com" }
                };

            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            _mockUserRepo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(users);

            // Act
            var result = await _userService.GetAllUserAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockUserRepo.Verify(x => x.GetAllAsync(), Times.Once);
        }

        #endregion

        #region ValidateCredentialsAsync Tests

        [Fact]
        public async Task ValidateCredentialsAsync_ShouldReturnTrue_WhenCredentialsAreValid()
        {
            // Arrange
            var email = "test@example.com";
            var password = "password123";
            var hashedPassword = _userService.HashPassword(password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = hashedPassword.ToString()
            };

            _mockUserRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.ValidateCredentialsAsync(email, password);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateCredentialsAsync_ShouldReturnFalse_WhenPasswordIsInvalid()
        {
            // Arrange
            var email = "test@example.com";
            var correctPassword = "password123";
            var wrongPassword = "wrongpassword";
            var hashedPassword = _userService.HashPassword(correctPassword);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = hashedPassword.ToString()
            };

            _mockUserRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.ValidateCredentialsAsync(email, wrongPassword);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateCredentialsAsync_ShouldReturnFalse_WhenUserNotFound()
        {
            // Arrange
            var email = "test@example.com";
            var password = "password123";

            _mockUserRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync((User)null);

            // Act
            var result = await _userService.ValidateCredentialsAsync(email, password);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region HashPassword Tests

        [Fact]
        public void HashPassword_ShouldReturnHashedString()
        {
            // Arrange
            var password = "password123";

            // Act
            var result = _userService.HashPassword(password);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<string>(result);
            Assert.NotEqual(password, result.ToString());
        }

        [Fact]
        public void HashPassword_ShouldReturnSameHash_ForSamePassword()
        {
            // Arrange
            var password = "password123";

            // Act
            var hash1 = _userService.HashPassword(password);
            var hash2 = _userService.HashPassword(password);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void HashPassword_ShouldReturnDifferentHash_ForDifferentPasswords()
        {
            // Arrange
            var password1 = "password123";
            var password2 = "password456";

            // Act
            var hash1 = _userService.HashPassword(password1);
            var hash2 = _userService.HashPassword(password2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        #endregion
    }
}
