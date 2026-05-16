namespace LCM.Domain.Entities;

public class Campaign
{
    public int Id { get; set; }
    public long Sku { get; set; }
    public string Barkod { get; set; } = string.Empty;
    public int SablonId { get; set; }
    public string? Baslik { get; set; }
    public string? AltBaslik { get; set; }
    public string? KampanyaNotu { get; set; }
    public string? Subheadline { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public string Durum { get; set; } = "Aktif";
    public int EkleyenKullaniciId { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public Template Sablon { get; set; } = null!;
    public User EkleyenKullanici { get; set; } = null!;
    public string? BuyQuantityText { get; set; }
    public string? PayQuantityText { get; set; }
    public string? Headline { get; set; }
    public string? MinBasketText { get; set; }
    public string? DetailText { get; set; }
    public string? CampaignDescription { get; set; }
    public bool IsLocalProduction { get; set; }
    public string? OriginCountry { get; set; }
    public string? UnitPrice { get; set; }
    public string? PriceUpdateDate { get; set; }
    public string? DateRange { get; set; }
    public ICollection<CampaignStore> CampaignStores { get; set; } = new List<CampaignStore>();
    // Onay süreci alanları
    public int? CurrentOwnerKullaniciId { get; set; }
    public User? CurrentOwner { get; set; }
    public string? OnayYorumu { get; set; }
    public ICollection<CampaignLog> Loglar { get; set; } = new List<CampaignLog>();
}