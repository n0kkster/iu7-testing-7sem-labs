using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;

namespace Analyzer.Tests.Entities;

public class UserEntityTests
{
    #region Factory Validation

    [Theory]
    [InlineData("", "test@test.com", "hash")]
    [InlineData("user", "", "hash")]
    [InlineData("user", "test@test.com", "")]
    [InlineData(" ", " ", " ")]
    public void User_CreateAdmin_WithEmptyArguments_ThrowsArgumentException(string username, string email, string hash)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => User.CreateAdmin(username, email, hash));
    }

    [Fact]
    public void User_CreateAdmin_WithInvalidEmail_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => User.CreateAdmin("user", "invalid_email", "hash"));
    }

    #endregion

    #region Invited User Specific Logic

    [Fact]
    public void User_CreateInvitedUser_WithAdminRole_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            User.CreateInvitedUser("user", "test@test.com", "hash", Role.Admin, Guid.NewGuid()));
    }

    [Fact]
    public void User_CreateInvitedUser_WithEmptyTeamId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            User.CreateInvitedUser("user", "test@test.com", "hash", Role.Developer, Guid.Empty));
    }

    [Fact]
    public void User_CreateInvitedUser_ValidParams_SetsPropertiesCorrectly()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var role = Role.Developer;

        // Act
        var user = User.CreateInvitedUser("dev", "dev@test.com", "hash", role, teamId);

        // Assert
        Assert.Equal(role, user.Role);
        Assert.Equal(teamId, user.TeamId);
        Assert.Equal("dev", user.Username);
    }

    #endregion

    #region Profile Update & Password

    [Fact]
    public void User_UpdateProfile_WithEmptyValues_ThrowsArgumentException()
    {
        // Arrange
        var user = User.CreateAdmin("old_name", "old@test.com", "hash");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => user.UpdateProfile("", "  ", Guid.NewGuid())); 
    }

    [Fact]
    public void User_UpdateProfile_WithInvalidEmail_ThrowsArgumentException()
    {
        // Arrange
        var user = User.CreateAdmin("old_name", "old@test.com", "hash");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => user.UpdateProfile("user", "invalid_email", Guid.NewGuid()));
    }

    [Fact]
    public void User_UpdateProfile_WithValidValues_UpdatesProperties()
    {
        // Arrange
        var user = User.CreateAdmin("old_name", "old@test.com", "hash");

        // Act
        var newAvatarGuid = Guid.NewGuid();
        user.UpdateProfile("new_name", "new@test.com", newAvatarGuid);

        // Assert
        Assert.Equal("new_name", user.Username);
        Assert.Equal("new@test.com", user.Email);
        Assert.Equal(newAvatarGuid, user.AvatarId);
    }

    [Fact]
    public void User_ChangePassword_WithEmptyHash_ThrowsArgumentException()
    {
        // Arrange
        var user = User.CreateAdmin("name", "test@test.com", "old_hash");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => user.ChangePassword("   "));
        Assert.Contains("не может быть пустым", ex.Message);
    }

    #endregion
}