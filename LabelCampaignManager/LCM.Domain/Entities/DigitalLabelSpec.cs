namespace LCM.Domain.Entities;

public class DigitalLabelSpec
{
    public int Id { get; set; }
    public string EtiketAdi { get; set; } = string.Empty;
    public string? Inch { get; set; }
    public string? Olculer { get; set; }
    public string? DPI { get; set; }
    public string? TahminiPilOmru { get; set; }
    public string? DesteklenenRenkler { get; set; }
    public string? LedDesteklenenRenkler { get; set; }
    public string? DayanabildigiSicaklik { get; set; }
    public string? ActiveDisplayArea { get; set; }
    public string? Dimensions { get; set; }
    public string? PageSwitch { get; set; }
    public string? ViewingAngle { get; set; }
    public int EtiketTipId { get; set; }
    public LabelType EtiketTip { get; set; } = null!;
}