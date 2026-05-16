namespace LCM.Domain.Entities;

public class EslEslestirme
{
    public int Id { get; set; }
    public string EslBarkod { get; set; } = string.Empty;
    public int KampanyaId { get; set; }
    public Campaign Kampanya { get; set; } = null!;
    public int StoreId { get; set; }
    public Store Store { get; set; } = null!;
    public int KullaniciId { get; set; }
    public User Kullanici { get; set; } = null!;
    public DateTime EslestirmeTarihi { get; set; } = DateTime.Now;
    public bool Override { get; set; } = false;
    public string IslemTipi { get; set; } = "Tekli";
    public bool BasariliMi { get; set; } = true;
    public string? HataMesaji { get; set; }
    public string? GonderilenJson { get; set; }

}