namespace LCM.Domain.Entities;

public class EslGonderimLog
{
    public int Id { get; set; }
    public DateTime GonderimZamani { get; set; } = DateTime.Now;
    public string Tetikleyen { get; set; } = string.Empty;    // "Manuel: ahmet" veya "JOB: Öğlen Gönderimi"
    public int? EslJobId { get; set; }                        // Job tetiklediyse
    public EslJob? EslJob { get; set; }
    public int? KullaniciId { get; set; }                     // Manuel gönderdiyse
    public User? Kullanici { get; set; }
    public string StoreCode { get; set; } = string.Empty;     // Hangi şubeye gitti
    public int ToplamKampanya { get; set; }                   // Kaç kampanya gönderildi
    public int BasariliKampanya { get; set; }                 // Kaçı başarılı
    public int BasarisizKampanya { get; set; }                // Kaçı başarısız
    public int HttpStatusKod { get; set; }                    // ESL'den gelen status
    public bool Basarili { get; set; }                        // Genel sonuç
    public string? HataMesaji { get; set; }                   // Hata varsa
    public string? GonderilenJson { get; set; }               // Gönderilen payload (debug için)
}