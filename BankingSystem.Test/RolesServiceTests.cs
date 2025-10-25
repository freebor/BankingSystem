using BankingSystem.Models.Dto;
using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using BankingSystem.Services;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System.Text;
using System.Text.Json;

namespace BankingSystem.Test
{
    public partial class UnitTest1
    {
        public class RolesServiceTests
        {
            private readonly Mock<IRolesRepository> _mockRoleRepo;
            private readonly Mock<IDistributedCache> _mockCache;
            private readonly RoleService _roleService;

            public RolesServiceTests()
            {
                _mockRoleRepo = new Mock<IRolesRepository>();
                _mockCache = new Mock<IDistributedCache>();
                _roleService = new RoleService(_mockRoleRepo.Object, _mockCache.Object);
            }

            #region CreateRole Tests

            [Fact]
            public async Task CreateRole_ShouldCreateRole_AndCacheIt()
            {
                // Arrange
                var roleDto = new RolesDto { RoleName = "Admin" };
                var expectedRole = new Roles
                {
                    RoleId = Guid.NewGuid(),
                    RoleName = roleDto.RoleName
                };

                _mockRoleRepo.Setup(x => x.CreateAsync(It.IsAny<Roles>()))
                    .ReturnsAsync(expectedRole);

                _mockCache.Setup(x => x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                _mockCache.Setup(x => x.RemoveAsync("all_roles", It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                // Act
                var result = await _roleService.CreateRole(roleDto);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(roleDto.RoleName, result.RoleName);
                _mockRoleRepo.Verify(x => x.CreateAsync(It.IsAny<Roles>()), Times.Once);
                _mockCache.Verify(x => x.SetAsync(
                    It.Is<string>(s => s.Contains("role:")),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()), Times.Once);
                _mockCache.Verify(x => x.RemoveAsync("all_roles", It.IsAny<CancellationToken>()), Times.Once);
            }

            #endregion

            #region GetRoleById Tests

            [Fact]
            public async Task GetRoleById_ShouldReturnCachedRole_WhenCacheExists()
            {
                // Arrange
                var roleId = Guid.NewGuid();
                var cachedRole = new Roles
                {
                    RoleId = roleId,
                    RoleName = "Admin"
                };

                var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedRole));
                _mockCache.Setup(x => x.GetAsync($"RoleId{roleId}", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(cachedData);

                // Act
                var result = await _roleService.GetRoleById(roleId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(roleId, result.RoleId);
                Assert.Equal("Admin", result.RoleName);
                _mockRoleRepo.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            }

            [Fact]
            public async Task GetRoleById_ShouldQueryRepository_WhenCacheIsEmpty()
            {
                // Arrange
                var roleId = Guid.NewGuid();
                var role = new Roles
                {
                    RoleId = roleId,
                    RoleName = "Admin"
                };

                _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((byte[])null);

                _mockRoleRepo.Setup(x => x.GetByIdAsync(roleId))
                    .ReturnsAsync(role);

                // Act
                var result = await _roleService.GetRoleById(roleId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(roleId, result.RoleId);
                _mockRoleRepo.Verify(x => x.GetByIdAsync(roleId), Times.Once);
                _mockCache.Verify(x => x.SetAsync(
                    It.Is<string>(s => s.Contains("role:")),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            }

            [Fact]
            public async Task GetRoleById_ShouldReturnNull_WhenRoleNotFound()
            {
                // Arrange
                var roleId = Guid.NewGuid();

                _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((byte[])null);

                _mockRoleRepo.Setup(x => x.GetByIdAsync(roleId))
                    .ReturnsAsync((Roles)null);

                // Act
                var result = await _roleService.GetRoleById(roleId);

                // Assert
                Assert.Null(result);
                _mockRoleRepo.Verify(x => x.GetByIdAsync(roleId), Times.Once);
            }

            #endregion

            #region GetRoleByName Tests

            [Fact]
            public async Task GetRoleByName_ShouldReturnCachedRole_WhenCacheExists()
            {
                // Arrange
                var roleName = "Admin";
                var cachedRole = new Roles
                {
                    RoleId = Guid.NewGuid(),
                    RoleName = roleName
                };

                var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedRole));
                _mockCache.Setup(x => x.GetAsync($"role_name:{roleName.ToLower()}", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(cachedData);

                // Act
                var result = await _roleService.GetRoleByName(roleName);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(roleName, result.RoleName);
                _mockRoleRepo.Verify(x => x.GetByNameAsync(It.IsAny<string>()), Times.Never);
            }

            [Fact]
            public async Task GetRoleByName_ShouldQueryRepository_WhenCacheIsEmpty()
            {
                // Arrange
                var roleName = "Admin";
                var role = new Roles
                {
                    RoleId = Guid.NewGuid(),
                    RoleName = roleName
                };

                _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((byte[])null);

                _mockRoleRepo.Setup(x => x.GetByNameAsync(roleName))
                    .ReturnsAsync(role);

                // Act
                var result = await _roleService.GetRoleByName(roleName);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(roleName, result.RoleName);
                _mockRoleRepo.Verify(x => x.GetByNameAsync(roleName), Times.Once);
            }

            #endregion

            #region GetAllRoles Tests

            [Fact]
            public async Task GetAllRoles_ShouldReturnCachedRoles_WhenCacheExists()
            {
                // Arrange
                var roles = new List<Roles>
            {
                new Roles { RoleId = Guid.NewGuid(), RoleName = "Admin" },
                new Roles { RoleId = Guid.NewGuid(), RoleName = "User" }
            };

                var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(roles));
                _mockCache.Setup(x => x.GetAsync("allRoles", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(cachedData);

                // Act
                var result = await _roleService.GetAllRoles();

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count());
                _mockRoleRepo.Verify(x => x.GetAllAsync(), Times.Never);
            }

            [Fact]
            public async Task GetAllRoles_ShouldQueryRepository_WhenCacheIsEmpty()
            {
                // Arrange
                var roles = new List<Roles>
            {
                new Roles { RoleId = Guid.NewGuid(), RoleName = "Admin" },
                new Roles { RoleId = Guid.NewGuid(), RoleName = "User" }
            };

                _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((byte[])null);

                _mockRoleRepo.Setup(x => x.GetAllAsync())
                    .ReturnsAsync(roles);

                // Act
                var result = await _roleService.GetAllRoles();

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count());
                _mockRoleRepo.Verify(x => x.GetAllAsync(), Times.Once);
            }

            #endregion

            #region DeleteRole Tests

            [Fact]
            public async Task DeleteRole_ShouldDeleteRole_AndInvalidateCache()
            {
                // Arrange
                var roleId = Guid.NewGuid();

                _mockRoleRepo.Setup(x => x.DeleteAsync(roleId))
                    .Returns(Task.CompletedTask);

                _mockCache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                // Act
                await _roleService.DeleteRole(roleId);

                // Assert
                _mockRoleRepo.Verify(x => x.DeleteAsync(roleId), Times.Once);
                _mockCache.Verify(x => x.RemoveAsync($"role:{roleId}", It.IsAny<CancellationToken>()), Times.Once);
                _mockCache.Verify(x => x.RemoveAsync("all_roles", It.IsAny<CancellationToken>()), Times.Once);
            }

            #endregion

            #region AssignRoleToUser Tests

            [Fact]
            public async Task AssignRoleToUser_ShouldAssignRole_AndInvalidateCache()
            {
                // Arrange
                var userId = Guid.NewGuid();
                var roleId = Guid.NewGuid();
                var allRoles = new List<Roles>
            {
                new Roles { RoleId = roleId, RoleName = "Admin" }
            };

                _mockRoleRepo.Setup(x => x.AssignRoleToUserAsync(userId, roleId))
                    .Returns(Task.CompletedTask);

                _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((byte[])null);

                _mockRoleRepo.Setup(x => x.GetAllAsync())
                    .ReturnsAsync(allRoles);

                _mockCache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                // Act
                await _roleService.AssignRoleToUser(userId, roleId);

                // Assert
                _mockRoleRepo.Verify(x => x.AssignRoleToUserAsync(userId, roleId), Times.Once);
                _mockCache.Verify(x => x.RemoveAsync($"user_roles:{userId}", It.IsAny<CancellationToken>()), Times.Once);
            }

            #endregion

            #region RemoveRoleFromUser Tests

            [Fact]
            public async Task RemoveRoleFromUser_ShouldRemoveRole_AndInvalidateCache()
            {
                // Arrange
                var userId = Guid.NewGuid();
                var roleId = Guid.NewGuid();
                var allRoles = new List<Roles>
            {
                new Roles { RoleId = roleId, RoleName = "Admin" }
            };

                _mockRoleRepo.Setup(x => x.RemoveRoleFromUserAsync(userId, roleId))
                    .Returns(Task.CompletedTask);

                _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((byte[])null);

                _mockRoleRepo.Setup(x => x.GetAllAsync())
                    .ReturnsAsync(allRoles);

                _mockCache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                // Act
                await _roleService.RemoveRoleFromUser(userId, roleId);

                // Assert
                _mockRoleRepo.Verify(x => x.RemoveRoleFromUserAsync(userId, roleId), Times.Once);
                _mockCache.Verify(x => x.RemoveAsync($"user_roles:{userId}", It.IsAny<CancellationToken>()), Times.Once);
            }

            #endregion

            #region GetUserRoles Tests

            [Fact]
            public async Task GetUserRoles_ShouldReturnCachedRoles_WhenCacheExists()
            {
                // Arrange
                var userId = Guid.NewGuid();
                var roles = new List<Roles>
            {
                new Roles { RoleId = Guid.NewGuid(), RoleName = "Admin" },
                new Roles { RoleId = Guid.NewGuid(), RoleName = "User" }
            };

                var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(roles));
                _mockCache.Setup(x => x.GetAsync($"user_roles:{userId}", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(cachedData);

                // Act
                var result = await _roleService.GetUserRoles(userId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count());
                _mockRoleRepo.Verify(x => x.GetUserRolesAsync(It.IsAny<Guid>()), Times.Never);
            }

            [Fact]
            public async Task GetUserRoles_ShouldQueryRepository_WhenCacheIsEmpty()
            {
                // Arrange
                var userId = Guid.NewGuid();
                var roles = new List<Roles>
            {
                new Roles { RoleId = Guid.NewGuid(), RoleName = "Admin" },
                new Roles { RoleId = Guid.NewGuid(), RoleName = "User" }
            };

                _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((byte[])null);

                _mockRoleRepo.Setup(x => x.GetUserRolesAsync(userId))
                    .ReturnsAsync(roles);

                // Act
                var result = await _roleService.GetUserRoles(userId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count());
                _mockRoleRepo.Verify(x => x.GetUserRolesAsync(userId), Times.Once);
            }

            #endregion

            #region UserHasRole Tests

            [Fact]
            public async Task UserHasRole_ShouldReturnCachedResult_WhenCacheExists()
            {
                // Arrange
                var userId = Guid.NewGuid();
                var roleName = "Admin";

                var cachedData = Encoding.UTF8.GetBytes("True");
                _mockCache.Setup(x => x.GetAsync($"user_has_role:{userId}:{roleName.ToLower()}", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(cachedData);

                // Act
                var result = await _roleService.UserHasRole(userId, roleName);

                // Assert
                Assert.True(result);
                _mockRoleRepo.Verify(x => x.UserHasRoleAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            }

            [Fact]
            public async Task UserHasRole_ShouldQueryRepository_WhenCacheIsEmpty()
            {
                // Arrange
                var userId = Guid.NewGuid();
                var roleName = "Admin";

                _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((byte[])null);

                _mockRoleRepo.Setup(x => x.UserHasRoleAsync(userId, roleName))
                    .ReturnsAsync(true);

                // Act
                var result = await _roleService.UserHasRole(userId, roleName);

                // Assert
                Assert.True(result);
                _mockRoleRepo.Verify(x => x.UserHasRoleAsync(userId, roleName), Times.Once);
            }

            [Fact]
            public async Task UserHasRole_ShouldReturnFalse_WhenUserDoesNotHaveRole()
            {
                // Arrange
                var userId = Guid.NewGuid();
                var roleName = "Admin";

                _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((byte[])null);

                _mockRoleRepo.Setup(x => x.UserHasRoleAsync(userId, roleName))
                    .ReturnsAsync(false);

                // Act
                var result = await _roleService.UserHasRole(userId, roleName);

                // Assert
                Assert.False(result);
                _mockRoleRepo.Verify(x => x.UserHasRoleAsync(userId, roleName), Times.Once);
            }

            #endregion
        }
    }
}