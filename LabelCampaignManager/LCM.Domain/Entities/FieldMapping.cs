namespace LCM.Domain.Entities;

public class FieldMapping
{
    public int Id { get; set; }
    public int SablonId { get; set; }
    public string BizimAlanAdi { get; set; } = string.Empty; // Baslik, OriginalPrice...
    public int EslFieldId { get; set; }
    public Template Sablon { get; set; } = null!;
    public EslField EslField { get; set; } = null!;
}