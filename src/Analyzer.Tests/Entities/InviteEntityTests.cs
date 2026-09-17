using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;

namespace Analyzer.Tests.Entities;

public class InviteEntityTests
{
    [Fact]
    public void Invite_Constructor_ValidParams_HasPendingStatusAndCorrectCode()
    {
        // Arrange & Act
        var email = "test@domain.com";
        var invite = new Invite(email, 7, Guid.NewGuid(), Role.Developer);

        // Assert
        Assert.Equal(InviteStatus.Pending, invite.Status);
        Assert.NotNull(invite.Code);
        Assert.True(invite.ExpirationDate > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void InviteStatus_WhenDatePassed_AutomaticallyReturnsExpired()
    {
        // Arrange
        var invite = new Invite("test@domain.com", -1, Guid.NewGuid(), Role.Developer);

        // Act & Assert
        Assert.Equal(InviteStatus.Expired, invite.Status);
    }

    [Fact]
    public void ValidateCanBeConsumedBy_WithWrongEmail_ThrowsArgumentException()
    {
        // Arrange
        var invite = new Invite("admin@company.com", 7, Guid.NewGuid(), Role.Admin);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            invite.ValidateCanBeConsumedBy("hacker@company.com"));
            
        Assert.Contains("не предназначен", ex.Message);
    }

    [Fact]
    public void Consume_ValidUserId_ChangesStatusAndSetsActivatedBy()
    {
        // Arrange
        var invite = new Invite("dev@company.com", 7, Guid.NewGuid(), Role.Developer);
        var userId = Guid.NewGuid();

        // Act
        invite.Consume(userId);

        // Assert
        Assert.Equal(InviteStatus.Activated, invite.Status);
        Assert.Equal(userId, invite.ActivatedByUserId);
    }

    [Fact]
    public void Consume_WhenAlreadyActivated_ThrowsInvalidOperationException()
    {
        // Arrange
        var invite = new Invite("sre@company.com", 7, Guid.NewGuid(), Role.SRE);
        invite.Consume(Guid.NewGuid());

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => invite.Consume(Guid.NewGuid()));
        Assert.Contains("не находится в статусе ожидания", ex.Message);
    }

    [Fact]
    public void GetDetails_ReturnsCorrectDataFromInvite()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var role = Role.Architect;
        var invite = new Invite("arch@test.com", 7, teamId, role);

        // Act
        var details = invite.GetDetails();

        // Assert
        Assert.Equal(role, details.Role);
        Assert.Equal(teamId, details.TeamId);
    }

    [Fact]
    public void Revoke_WhenPending_ChangesToRevoked()
    {
        // Arrange
        var invite = new Invite("test@company.com", 7, Guid.NewGuid(), Role.Developer);

        // Act
        invite.Revoke();

        // Assert
        Assert.Equal(InviteStatus.Revoked, invite.Status);
    }

    [Fact]
    public void Revoke_WhenAlreadyActivated_ThrowsInvalidOperationException()
    {
        // Arrange
        var invite = new Invite("test@company.com", 7, Guid.NewGuid(), Role.Developer);
        invite.Consume(Guid.NewGuid());

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => invite.Revoke());
        Assert.Contains("Нельзя отозвать уже принятое", ex.Message);
    }

    [Fact]
    public void ValidateCanBeConsumedBy_WhenExpired_ThrowsInvalidOperationException()
    {
        // Arrange
        var invite = new Invite("old@test.com", -5, Guid.NewGuid(), Role.Developer);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            invite.ValidateCanBeConsumedBy("old@test.com"));
    }
}