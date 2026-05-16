using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LCM.Web.ViewModels;

public class TemplateCreateViewModel
{
    [Required(ErrorMessage = "Şablon adı zorunludur.")]
    public string SablonAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    [Required(ErrorMessage = "Etiket tipi seçiniz.")]
    public int EtiketTipId { get; set; }

    [Required(ErrorMessage = "Şablon fotoğrafı zorunludur.")]
    public IFormFile? SablonFoto { get; set; }

    // Aktif alanlar
    public bool AlanBaslik { get; set; }
    public bool AlanAltBaslik { get; set; }
    public bool AlanKampanyaNotu { get; set; }
    public bool AlanSubheadline { get; set; }
    public bool AlanOriginalPrice { get; set; }
    public bool AlanDiscountedPrice { get; set; }
    public int? CaptchaCevap { get; set; }
    public bool AlanBuyQuantityText { get; set; }
    public bool AlanPayQuantityText { get; set; }
    public bool AlanDateRange { get; set; }
    public string? LayoutKodu { get; set; }
    public bool AlanHeadline { get; set; }
    public bool AlanMinBasketText { get; set; }
    public bool AlanDetailText { get; set; }
    public bool AlanCampaignDescription { get; set; }
    public bool AlanIsLocalProduction { get; set; }
    public bool AlanOriginCountry { get; set; }
    public bool AlanUnitPrice { get; set; }
    public bool AlanPriceUpdateDate { get; set; }
}

public class TemplateEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Şablon adı zorunludur.")]
    public string SablonAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    [Required(ErrorMessage = "Etiket tipi seçiniz.")]
    public int EtiketTipId { get; set; }

    public string? MevcutFotoYolu { get; set; }
    public IFormFile? SablonFoto { get; set; }

    public bool AlanBaslik { get; set; }
    public bool AlanAltBaslik { get; set; }
    public bool AlanKampanyaNotu { get; set; }
    public bool AlanSubheadline { get; set; }
    public bool AlanOriginalPrice { get; set; }
    public bool AlanDiscountedPrice { get; set; }
    public int? CaptchaCevap { get; set; }
    public bool AlanBuyQuantityText { get; set; }
    public bool AlanPayQuantityText { get; set; }
    public bool AlanDateRange { get; set; }
    public string? LayoutKodu { get; set; }
    public bool AlanHeadline { get; set; }
    public bool AlanMinBasketText { get; set; }
    public bool AlanDetailText { get; set; }
    public bool AlanCampaignDescription { get; set; }
    public bool AlanIsLocalProduction { get; set; }
    public bool AlanOriginCountry { get; set; }
    public bool AlanUnitPrice { get; set; }
    public bool AlanPriceUpdateDate { get; set; }

}