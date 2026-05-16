using System.ComponentModel.DataAnnotations;

namespace LCM.Web.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    public string KullaniciAdi { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    public string Sifre { get; set; } = string.Empty;

    public bool BeniHatirla { get; set; }
}