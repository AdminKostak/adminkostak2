namespace LCM.Domain.Entities;

public class EslField
{
    public int Id { get; set; }
    public string VariableName { get; set; } = string.Empty; // sku, itemName, price1...
    public string DataType { get; set; } = string.Empty;     // varchar, decimal, int...
    public bool IsRequired { get; set; }
    public string? Aciklama { get; set; }
    public ICollection<FieldMapping> Mappings { get; set; } = new List<FieldMapping>();
}