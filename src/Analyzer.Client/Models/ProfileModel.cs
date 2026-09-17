using System.ComponentModel.DataAnnotations;

public class UpdateProfileViewModel
{
    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Имя должно быть от 3 до 50 символов")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат Email")]
    public string Email { get; set; } = string.Empty;
    public Guid? AvatarId { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Введите текущий пароль")]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите новый пароль")]
    [MinLength(8, ErrorMessage = "Пароль должен быть не короче 8 символов")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите новый пароль")]
    [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;
}