namespace Analyzer.Shared.DTO.V2;

public record LoginResultDto(
    string? Token,
    bool RequiresTwoFactor = false,
    string? Message = null
);

public record VerifyTwoFactorDto(string Username, string Code);

public record RequestUnlockDto(string Username);

public record ConfirmUnlockDto(string Username, string Code, string? NewPassword = null);

public record ToggleTwoFactorDto(bool Enabled);