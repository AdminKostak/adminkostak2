using System.ComponentModel.DataAnnotations;

namespace LCM.Web.ViewModels;

public class CampaignCreateViewModel
{
    [Required(ErrorMessage = "Şablon seçiniz.")]
    public int SablonId { get; set; }

    public string? Baslik { get; set; }
    public string? AltBaslik { get; set; }
    public string? KampanyaNotu { get; set; }
    public string? Subheadline { get; set; }

    public decimal? OriginalPrice { get; set; }

    public decimal? DiscountedPrice { get; set; }

    public string? BuyQuantityText { get; set; }
    public string? PayQuantityText { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    public DateTime BaslangicTarihi { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    public DateTime BitisTarihi { get; set; } = DateTime.Today.AddDays(7);

    public string Durum { get; set; } = "Aktif";
    public string? Headline { get; set; }
    public string? MinBasketText { get; set; }
    public string? DetailText { get; set; }
    public string? CampaignDescription { get; set; }
    public bool IsLocalProduction { get; set; }
    public string? OriginCountry { get; set; }
    public string? UnitPrice { get; set; }
    public string? PriceUpdateDate { get; set; }

    public List<int> StoreIds { get; set; } = new();


    public int? CaptchaCevap { get; set; }
}

public class CampaignEditViewModel
{
    public int Id { get; set; }
    public long Sku { get; set; }
    public string Barkod { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şablon seçiniz.")]
    public int SablonId { get; set; }

    public string? Baslik { get; set; }
    public string? AltBaslik { get; set; }
    public string? KampanyaNotu { get; set; }
    public string? Subheadline { get; set; }

    public decimal? OriginalPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public string? BuyQuantityText { get; set; }
    public string? PayQuantityText { get; set; }
    public string? DateRange { get; set; }


    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    public DateTime BaslangicTarihi { get; set; }

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    public DateTime BitisTarihi { get; set; }
    public string? Headline { get; set; }
    public string? MinBasketText { get; set; }
    public string? DetailText { get; set; }
    public string? CampaignDescription { get; set; }
    public bool IsLocalProduction { get; set; }
    public string? OriginCountry { get; set; }
    public string? UnitPrice { get; set; }
    public string? PriceUpdateDate { get; set; }
    public string Durum { get; set; } = "Aktif";
    public List<int> StoreIds { get; set; } = new();

}

public class CampaignListViewModel
{
    public int Id { get; set; }
    public long Sku { get; set; }
    public string Barkod { get; set; } = string.Empty;
    public string SablonAdi { get; set; } = string.Empty;
    public int SablonId { get; set; }          // ← YENİ: hover önizleme için
    public string? Baslik { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public string Durum { get; set; } = string.Empty;
    public string EkleyenAdi { get; set; } = string.Empty;
    public int EkleyenKullaniciId { get; set; } // ← YENİ: filtre için
    public DateTime OlusturmaTarihi { get; set; }
    public string? StoreDisplay { get; set; }

    // ← YENİ: Son onay aksiyonu (Onaylandı / Reddedildi / Revize İstendi vb.)
    public string? SonOnayAksiyonu { get; set; }
    // ← YENİ: Son onay yorumu (reddedildi / revize ise gösterilir)
    public string? SonOnayYorumu { get; set; }


}