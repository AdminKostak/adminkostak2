namespace LCM.Domain.Entities;

public class Template
{
    public int Id { get; set; }
    public string SablonAdi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public int EtiketTipId { get; set; }
    public string? SablonFotoYolu { get; set; }
    public int EkleyenKullaniciId { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public LabelType EtiketTip { get; set; } = null!;
    public User EkleyenKullanici { get; set; } = null!;
    public ICollection<TemplateField> Alanlar { get; set; } = new List<TemplateField>();
    public ICollection<Campaign> Kampanyalar { get; set; } = new List<Campaign>();
    public string? LayoutKodu { get; set; }
    public string? LayoutJson { get; set; }

}