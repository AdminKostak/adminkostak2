namespace LCM.Domain.Entities;

public class TemplateField
{
    public int Id { get; set; }
    public int SablonId { get; set; }
    public string AlanAdi { get; set; } = string.Empty;
    public bool AktifMi { get; set; } = true;
    public Template Sablon { get; set; } = null!;
}