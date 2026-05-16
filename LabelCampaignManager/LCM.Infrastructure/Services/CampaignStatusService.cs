using LCM.Domain.Entities;
using LCM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LCM.Infrastructure.Services;

public class CampaignStatusService
{
    private readonly AppDbContext _db;

    public CampaignStatusService(AppDbContext db)
    {
        _db = db;
    }

    // Merkezi statü geçiş metodu — tüm aksiyonlar buradan geçer
    public async Task StatuGuncelle(
        Campaign kampanya,
        string yeniStatu,
        int? kullaniciId,
        string aksiyon,
        string? yorum = null,
        string? hedefEmail = null)
    {
        kampanya.Durum = yeniStatu;

        _db.CampaignLogs.Add(new CampaignLog
        {
            CampaignId = kampanya.Id,
            KullaniciId = kullaniciId,
            Aksiyon = aksiyon,
            Yorum = yorum,
            HedefEmail = hedefEmail,
            Tarih = DateTime.Now
        });

        await _db.SaveChangesAsync();
    }

    // Kampanya oluşturuldu — onay ayarına göre statü belirle
    public async Task<string> YeniKampanyaStatuBelirle(bool onayAktif, DateTime baslangic, DateTime bitis)
    {
        if (onayAktif)
            return "Onay Bekliyor";

        var bugun = DateTime.Today;
        if (baslangic <= bugun && bitis >= bugun)
            return "Aktif";
        if (baslangic > bugun)
            return "Planlanmış";

        return "Pasif";
    }

    // Onayla
    public async Task Onayla(Campaign kampanya, int kullaniciId)
    {
        var bugun = DateTime.Today;
        string yeniStatu;

        if (kampanya.BaslangicTarihi <= bugun && kampanya.BitisTarihi >= bugun)
            yeniStatu = "Aktif";
        else if (kampanya.BaslangicTarihi > bugun)
            yeniStatu = "Planlanmış";
        else
            yeniStatu = "Pasif";

        kampanya.CurrentOwnerKullaniciId = null;
        kampanya.OnayYorumu = null;

        await StatuGuncelle(kampanya, yeniStatu, kullaniciId, "Onaylandı");
    }

    // Reddet
    public async Task Reddet(Campaign kampanya, int kullaniciId, string yorum)
    {
        kampanya.CurrentOwnerKullaniciId = kampanya.EkleyenKullaniciId;
        kampanya.OnayYorumu = yorum;

        await StatuGuncelle(kampanya, "Reddedildi", kullaniciId, "Reddedildi", yorum);
    }

    // Revize İste
    public async Task RevizeIste(Campaign kampanya, int kullaniciId, string yorum)
    {
        kampanya.CurrentOwnerKullaniciId = kampanya.EkleyenKullaniciId;
        kampanya.OnayYorumu = yorum;

        await StatuGuncelle(kampanya, "Revize İstendi", kullaniciId, "Revize İstendi", yorum);
    }

    // Onaya Gönder
    public async Task OnayaGonder(Campaign kampanya, int kullaniciId, int kampanyaYoneticiId)
    {
        kampanya.CurrentOwnerKullaniciId = kampanyaYoneticiId;
        kampanya.OnayYorumu = null;

        await StatuGuncelle(kampanya, "Onay Bekliyor", kullaniciId, "Onaya Gönderildi");
    }
}