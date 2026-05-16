namespace LCM.Domain.Entities;

public class CampaignLog
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;

    public int? KullaniciId { get; set; }
    public User? Kullanici { get; set; }

    public string Aksiyon { get; set; } = string.Empty;
    // Örnekler: "Oluşturuldu", "Onaya Gönderildi", "Onaylandı",
    //           "Reddedildi", "Revize İstendi", "Düzenlendi",
    //           "Mail Gönderildi", "Mail Gönderilemedi"

    public string? Yorum { get; set; }
    // Reddet/Revize yorumu veya mail adresi gibi ek bilgi

    public string? HedefEmail { get; set; }
    // Mail gönderildiyse kime gönderildiği

    public DateTime Tarih { get; set; } = DateTime.Now;
}