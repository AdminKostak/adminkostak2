using LCM.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LCM.Web.Controllers;

[Authorize(Roles = "Admin")]
public class SystemSettingController : Controller
{
    private readonly AppDbContext _db;

    public SystemSettingController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var ayarlar = await _db.SystemSettings.OrderBy(a => a.AyarAdi).ToListAsync();
        ViewBag.SmtpSetting = await _db.SmtpSettings.FirstOrDefaultAsync();
        return View(ayarlar);
    }

    [HttpPost]
    public async Task<IActionResult> Guncelle(int id)
    {
        var ayar = await _db.SystemSettings.FindAsync(id);
        if (ayar == null) return NotFound();

        ayar.AktifMi = !ayar.AktifMi;
        await _db.SaveChangesAsync();

        TempData["Basari"] = $"{ayar.AyarAdi} güncellendi.";
        return RedirectToAction("Index");
    }
    [HttpPost]
    public async Task<IActionResult> SmtpGuncelle(
    string host, int port, string kullaniciAdi,
    string sifre, string gonderenAdi, string gonderenEmail, bool sslAktif)
    {
        var smtp = await _db.SmtpSettings.FirstOrDefaultAsync();
        if (smtp == null)
        {
            smtp = new LCM.Domain.Entities.SmtpSetting();
            _db.SmtpSettings.Add(smtp);
        }

        smtp.Host = host ?? "";
        smtp.Port = port;
        smtp.KullaniciAdi = kullaniciAdi ?? "";
        smtp.GonderenAdi = gonderenAdi ?? "";
        smtp.GonderenEmail = gonderenEmail ?? "";
        smtp.SslAktif = sslAktif;

        // Şifre boş geldiyse mevcut şifreyi koru
        if (!string.IsNullOrEmpty(sifre))
            smtp.Sifre = sifre;

        await _db.SaveChangesAsync();
        TempData["Basari"] = "SMTP ayarları güncellendi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> SmtpTest(string hedefEmail)
    {
        var smtp = await _db.SmtpSettings.FirstOrDefaultAsync();
        if (smtp == null || string.IsNullOrEmpty(smtp.Host))
            return Json(new { ok = false, mesaj = "SMTP ayarları henüz yapılandırılmamış." });

        try
        {
            var client = new System.Net.Mail.SmtpClient(smtp.Host, smtp.Port)
            {
                Credentials = new System.Net.NetworkCredential(smtp.KullaniciAdi, smtp.Sifre),
                EnableSsl = smtp.SslAktif
            };
            var mesaj = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(smtp.GonderenEmail, smtp.GonderenAdi),
                Subject = "LCM — SMTP Test Maili",
                Body = "<p>Bu bir test mailidir. SMTP ayarlarınız çalışıyor.</p>",
                IsBodyHtml = true
            };
            mesaj.To.Add(hedefEmail);
            await client.SendMailAsync(mesaj);
            return Json(new { ok = true, mesaj = "Test maili başarıyla gönderildi." });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, mesaj = $"Hata: {ex.Message}" });
        }
    }
}