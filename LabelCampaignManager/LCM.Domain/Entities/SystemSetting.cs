namespace LCM.Domain.Entities;

public class SystemSetting
{
    public int Id { get; set; }
    public string AyarAdi { get; set; } = string.Empty;
    public bool AktifMi { get; set; } = false;
}