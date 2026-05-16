using LCM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LCM.Infrastructure.Services;

public class KampanyaGuncelleyici
{
    private readonly AppDbContext _db;

    public KampanyaGuncelleyici(AppDbContext db)
    {
        _db = db;
    }

    public async Task GuncelleAsync()
    {
        // Otomatik güncelleme ayarı kapalıysa hiçbir şey yapma
        var ayar = await _db.SystemSettings
            .FirstOrDefaultAsync(s => s.AyarAdi == "DashboardOtomatikGuncelleme");
        if (ayar?.AktifMi != true) return;

        var bugun = DateTime.Today;

        

        // Tarihi geçmişleri Pasife al
        var gecmisKampanyalar = await _db.Campaigns
            .Where(c => c.Durum != "Pasif"
                && c.Durum != "Taslak"
                && c.Durum != "Onay Bekliyor"
                && c.Durum != "Reddedildi"
                && c.Durum != "Revize İstendi"
                && c.BitisTarihi < bugun)
            .ToListAsync();
        gecmisKampanyalar.ForEach(k => k.Durum = "Pasif");

        // Henüz başlamamışları Planlanmış yap
        var baslamamisKampanyalar = await _db.Campaigns
            .Where(c => c.Durum != "Planlanmış"
                && c.Durum != "Taslak"
                && c.Durum != "Onay Bekliyor"
                && c.Durum != "Reddedildi"
                && c.Durum != "Revize İstendi"
                && c.BaslangicTarihi > bugun)
            .ToListAsync();
        baslamamisKampanyalar.ForEach(k => k.Durum = "Planlanmış");

        // Tarih aralığında olanları Aktif yap
        var aktifKampanyalar = await _db.Campaigns
            .Where(c => c.Durum != "Aktif"
                && c.Durum != "Taslak"
                && c.Durum != "Onay Bekliyor"
                && c.Durum != "Reddedildi"
                && c.Durum != "Revize İstendi"
                && c.BaslangicTarihi <= bugun
                && c.BitisTarihi >= bugun)
            .ToListAsync();
        aktifKampanyalar.ForEach(k => k.Durum = "Aktif");

        if (gecmisKampanyalar.Any() || baslamamisKampanyalar.Any() || aktifKampanyalar.Any())
            await _db.SaveChangesAsync();
    }
}