using LCM.Infrastructure.Data;
using LCM.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LCM.Web.Controllers;

[Authorize]
public class EslGonderimController : Controller
{
    private readonly AppDbContext _db;
    private readonly EslGonderimService _gonderimService;

    public EslGonderimController(AppDbContext db, EslGonderimService gonderimService)
    {
        _db = db;
        _gonderimService = gonderimService;
    }

    // Gönderim log sayfası
    public async Task<IActionResult> Index()
    {
        var loglar = await _db.EslGonderimLogs
            .Include(l => l.Kullanici)
            .Include(l => l.EslJob)
            .OrderByDescending(l => l.GonderimZamani)
            .Take(200)
            .ToListAsync();
        return View(loglar);
    }

    // Manuel toplu gönderim
    [HttpPost]
    [Authorize(Roles = "Admin,KampanyaYonetici")]
    public async Task<IActionResult> ManuelGonder(bool aktifGonder, bool planlanmisGonder)
    {
        if (!aktifGonder && !planlanmisGonder)
            return Json(new { basarili = false, mesaj = "En az bir durum seçiniz." });

        var kullaniciId = int.Parse(User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var kullanici = await _db.Users.FindAsync(kullaniciId);

        var sonuclar = await _gonderimService.GonderAsync(
            aktifGonder: aktifGonder,
            planlanmisGonder: planlanmisGonder,
            tetikleyenKullaniciId: kullaniciId,
            tetikleyenJobId: null,
            tetikleyenAciklama: $"Manuel: {kullanici?.KullaniciAdi}"
        );

        return Json(new { basarili = true, sonuclar });
    }

    // Tekli kampanya gönderimi
    [HttpPost]
    [Authorize(Roles = "Admin,KampanyaYonetici,VeriGirisi")]
    public async Task<IActionResult> TekliGonder(int kampanyaId)
    {
        var kullaniciId = int.Parse(User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var sonuclar = await _gonderimService.TekliGonderAsync(kampanyaId, kullaniciId);

        return Json(new { basarili = true, sonuclar });
    }
}