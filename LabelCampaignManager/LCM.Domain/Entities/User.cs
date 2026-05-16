namespace LCM.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string KullaniciAdi { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SifreHash { get; set; } = string.Empty;
    public int RolId { get; set; }
    public bool AktifMi { get; set; } = true;
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public Role Rol { get; set; } = null!;
    public bool CokluEslestirmeIzni { get; set; } = false;
    public bool HizliEslestirmeIzni { get; set; } = false;
    public ICollection<UserStore> YetkiliStoreler { get; set; } = new List<UserStore>();
}