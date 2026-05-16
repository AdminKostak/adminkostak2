namespace LCM.Web.ViewModels;

public class FieldMappingViewModel
{
    public int SablonId { get; set; }
    public string SablonAdi { get; set; } = string.Empty;
    public string EtiketTipi { get; set; } = string.Empty;
    public List<string> SablonAlanlari { get; set; } = new();
    public List<EslFieldItem> EslAlanlari { get; set; } = new();
    public List<MappingItem> MevcutEslesmeler { get; set; } = new();
}

public class EslFieldItem
{
    public int Id { get; set; }
    public string VariableName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
}

public class MappingItem
{
    public string BizimAlanAdi { get; set; } = string.Empty;
    public int? EslFieldId { get; set; }
    public string? EslVariableName { get; set; }
}

public class FieldMappingSaveViewModel
{
    public int SablonId { get; set; }
    public List<MappingItem> Eslesmeler { get; set; } = new();
}