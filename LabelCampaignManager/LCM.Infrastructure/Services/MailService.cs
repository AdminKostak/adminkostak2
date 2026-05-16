using LCM.Domain.Entities;
using LCM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace LCM.Infrastructure.Services;

public class MailService
{
    private readonly AppDbContext _db;

    public MailService(AppDbContext db)
    {
        _db = db;
    }

    // Temel mail gönderim metodu
    private async Task<bool> GonderAsync(string hedefEmail, string konu, string govde)
    {
        try
        {
            var smtp = await _db.SmtpSettings.FirstOrDefaultAsync();
            if (smtp == null || string.IsNullOrEmpty(smtp.Host)) return false;

            var client = new SmtpClient(smtp.Host, smtp.Port)
            {
                Credentials = new NetworkCredential(smtp.KullaniciAdi, smtp.Sifre),
                EnableSsl = smtp.SslAktif
            };

            var mesaj = new MailMessage
            {
                From = new MailAddress(smtp.GonderenEmail, smtp.GonderenAdi),
                Subject = konu,
                Body = govde,
                IsBodyHtml = true
            };
            mesaj.To.Add(hedefEmail);

            await client.SendMailAsync(mesaj);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Ayar kontrolü — mail bildirimleri açık mı?
    private async Task<bool> MailAktifMi(string ayarAdi)
    {
        var ayar = await _db.SystemSettings
            .FirstOrDefaultAsync(s => s.AyarAdi == ayarAdi);
        return ayar?.AktifMi == true;
    }

    // Kampanya yöneticisine — onay bekliyor maili
    public async Task<(bool Gonderildi, string? Email)> OnayBekliyorMailiGonder(
        Campaign kampanya,
        string yoneticiEmail,
        string baseUrl)
    {
        if (!await MailAktifMi("MailBildirimleriAktif")) return (false, null);
        if (!await MailAktifMi("OnayMailiGonderilsinMi")) return (false, null);

        var link = $"{baseUrl}/Campaign/Detail/{kampanya.Id}";
        var konu = $"Onayınızı Bekleyen Kampanya: {kampanya.Baslik}";
        var govde = $@"
            <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
                <h2 style='color:#2563eb;'>Onayınızı Bekleyen Kampanya</h2>
                <p>Aşağıdaki kampanya onayınızı beklemektedir:</p>
                <table style='width:100%;border-collapse:collapse;margin:16px 0;'>
                    <tr>
                        <td style='padding:8px;background:#f8fafc;font-weight:bold;width:140px;'>Başlık</td>
                        <td style='padding:8px;'>{kampanya.Baslik}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px;background:#f8fafc;font-weight:bold;'>SKU</td>
                        <td style='padding:8px;'>{kampanya.Sku}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px;background:#f8fafc;font-weight:bold;'>Tarih Aralığı</td>
                        <td style='padding:8px;'>{kampanya.BaslangicTarihi:dd.MM.yyyy} - {kampanya.BitisTarihi:dd.MM.yyyy}</td>
                    </tr>
                </table>
                <a href='{link}' style='display:inline-block;padding:12px 24px;background:#2563eb;color:white;
                    text-decoration:none;border-radius:8px;font-weight:bold;'>
                    Kampanyayı İncele
                </a>
                <p style='margin-top:24px;font-size:0.82rem;color:#888;'>Bu mail LCM sistemi tarafından otomatik gönderilmiştir.</p>
            </div>";

        var gonderildi = await GonderAsync(yoneticiEmail, konu, govde);
        return (gonderildi, yoneticiEmail);
    }

    // Veri girişi kullanıcısına — sonuç maili (onay/red/revize)
    public async Task<(bool Gonderildi, string? Email)> SonucMailiGonder(
        Campaign kampanya,
        string kullaniciEmail,
        string aksiyon,
        string? yorum,
        string baseUrl)
    {
        if (!await MailAktifMi("MailBildirimleriAktif")) return (false, null);
        if (!await MailAktifMi("OnayMailiGonderilsinMi")) return (false, null);

        var link = $"{baseUrl}/Campaign/Detail/{kampanya.Id}";

        string renkKodu = aksiyon switch
        {
            "Onaylandı" => "#16a34a",
            "Reddedildi" => "#dc2626",
            _ => "#d97706"
        };

        string baslik = aksiyon switch
        {
            "Onaylandı" => "Kampanyanız Onaylandı",
            "Reddedildi" => "Kampanyanız Reddedildi",
            _ => "Kampanyanız İçin Revize İstendi"
        };

        var yorumSatiri = !string.IsNullOrEmpty(yorum)
            ? $"<tr><td style='padding:8px;background:#f8fafc;font-weight:bold;'>Yorum</td><td style='padding:8px;color:{renkKodu};'>{yorum}</td></tr>"
            : "";

        var govde = $@"
            <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
                <h2 style='color:{renkKodu};'>{baslik}</h2>
                <table style='width:100%;border-collapse:collapse;margin:16px 0;'>
                    <tr>
                        <td style='padding:8px;background:#f8fafc;font-weight:bold;width:140px;'>Başlık</td>
                        <td style='padding:8px;'>{kampanya.Baslik}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px;background:#f8fafc;font-weight:bold;'>SKU</td>
                        <td style='padding:8px;'>{kampanya.Sku}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px;background:#f8fafc;font-weight:bold;'>Durum</td>
                        <td style='padding:8px;font-weight:bold;color:{renkKodu};'>{aksiyon}</td>
                    </tr>
                    {yorumSatiri}
                </table>
                <a href='{link}' style='display:inline-block;padding:12px 24px;background:{renkKodu};color:white;
                    text-decoration:none;border-radius:8px;font-weight:bold;'>
                    Kampanyayı Görüntüle
                </a>
                <p style='margin-top:24px;font-size:0.82rem;color:#888;'>Bu mail LCM sistemi tarafından otomatik gönderilmiştir.</p>
            </div>";

        var gonderildi = await GonderAsync(kullaniciEmail, baslik, govde);
        return (gonderildi, kullaniciEmail);
    }
}