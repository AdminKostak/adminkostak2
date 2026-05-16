namespace LCM.Web.ViewModels;

public class DigitalLabelSpecViewModel
{
    public int Id { get; set; }
    public string EtiketAdi { get; set; } = string.Empty;
    public string? Inch { get; set; }
    public string? Olculer { get; set; }
    public string? DPI { get; set; }
    public string? TahminiPilOmru { get; set; }
    public string? DayanabildigiSicaklik { get; set; }
    public string? ActiveDisplayArea { get; set; }
    public string? Dimensions { get; set; }
    public string? PageSwitch { get; set; }
    public string? ViewingAngle { get; set; }
    public int EtiketTipId { get; set; }

    // Renk seçimleri
    public List<string> DesteklenenRenkler { get; set; } = new();
    public List<string> LedDesteklenenRenkler { get; set; } = new();
}