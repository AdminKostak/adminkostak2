using System.ComponentModel.DataAnnotations;

namespace LCM.Web.ViewModels;

public class UserCreateViewModel
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur.")]
    public string Soyad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    public string KullaniciAdi { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
    public string Sifre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol seçiniz.")]
    public int RolId { get; set; }
    public int? CaptchaCevap { get; set; }
    public bool CokluEslestirmeIzni { get; set; } = false;
    public bool HizliEslestirmeIzni { get; set; } = false;
    public List<int> YetkiliStoreIdler { get; set; } = new();

}

public class UserEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad zorunludur.")]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur.")]
    public string Soyad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    public string KullaniciAdi { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string Email { get; set; } = string.Empty;

    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
    public string? YeniSifre { get; set; }

    [Required(ErrorMessage = "Rol seçiniz.")]
    public int RolId { get; set; }

    public bool AktifMi { get; set; }
    public int? CaptchaCevap { get; set; }
    public bool CokluEslestirmeIzni { get; set; } = false;
    public bool HizliEslestirmeIzni { get; set; } = false;
    public List<int> YetkiliStoreIdler { get; set; } = new();
}