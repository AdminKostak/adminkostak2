namespace LCM.Domain.Entities;

public class EslJob
{
    public int Id { get; set; }
    public string JobAdi { get; set; } = string.Empty;        // "Öğlen Gönderimi"
    public TimeOnly CalismaZamani { get; set; }               // 12:00
    public bool AktifGonder { get; set; } = true;             // Aktif kampanyalar
    public bool PlanlanmisGonder { get; set; } = false;       // Planlanmış kampanyalar
    public bool AktifMi { get; set; } = true;                 // Job açık/kapalı
    public DateTime? SonCalisma { get; set; }                 // Son tetiklenme zamanı
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public int OlusturanKullaniciId { get; set; }
    public User OlusturanKullanici { get; set; } = null!;
}