using System.ComponentModel.DataAnnotations;

namespace GamexBusinessPage.Models;

public sealed class ContactFormInputModel
{
    [Required(ErrorMessage = "Podaj imię i nazwisko.")]
    [StringLength(100, ErrorMessage = "Imię i nazwisko może mieć maksymalnie 100 znaków.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Podaj adres e-mail.")]
    [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail.")]
    [StringLength(200, ErrorMessage = "Adres e-mail może mieć maksymalnie 200 znaków.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Podaj poprawny numer telefonu.")]
    [StringLength(30, ErrorMessage = "Numer telefonu może mieć maksymalnie 30 znaków.")]
    public string? Phone { get; set; }

    [StringLength(150, ErrorMessage = "Temat może mieć maksymalnie 150 znaków.")]
    public string? Subject { get; set; }

    [StringLength(100, ErrorMessage = "Nieprawidłowa wartość pola maszyna.")]
    public string? Machine { get; set; }

    [Required(ErrorMessage = "Wpisz treść wiadomości.")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Wiadomość musi mieć od 10 do 4000 znaków.")]
    public string Message { get; set; } = string.Empty;

    public string? Website { get; set; }

    public string? FormToken { get; set; }
}
