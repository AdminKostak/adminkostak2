namespace LCM.Domain.Entities;

public class Font
{
    public int Id { get; set; }
    public string FontAdi { get; set; } = "";        // örn: "Poppins", "Montserrat"
    public string DosyaAdi { get; set; } = "";       // örn: "Poppins-Bold.ttf"
    public int FontWeight { get; set; } = 400;       // 100,200,...,900
    public bool Italic { get; set; } = false;
    public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
}