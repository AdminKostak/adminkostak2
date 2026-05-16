namespace LCM.Domain.Entities;

public class Layout
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string LayoutKodu { get; set; } = string.Empty;
    public string? LayoutJson { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
}