namespace LCM.Domain.Entities;

public class LabelType
{
    public int Id { get; set; }
    public string EtiketTipi { get; set; } = string.Empty;
    public ICollection<Template> Templates { get; set; } = new List<Template>();
}