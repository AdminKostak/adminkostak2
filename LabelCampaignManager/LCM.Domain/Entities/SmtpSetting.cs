namespace LCM.Domain.Entities;

public class SmtpSetting
{
    public int Id { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string KullaniciAdi { get; set; } = string.Empty;
    public string Sifre { get; set; } = string.Empty;
    public string GonderenAdi { get; set; } = string.Empty;
    public string GonderenEmail { get; set; } = string.Empty;
    public bool SslAktif { get; set; } = true;
}