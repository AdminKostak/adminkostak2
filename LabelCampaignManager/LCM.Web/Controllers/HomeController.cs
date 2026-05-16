using LCM.Infrastructure.Data;
using LCM.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LCM.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly KampanyaGuncelleyici _guncelleyici;

    public HomeController(AppDbContext db, KampanyaGuncelleyici guncelleyici)
    {
        _db = db;
        _guncelleyici = guncelleyici;
    }
    public IActionResult YetkiYok()
    {
        return View();
    }
    public IActionResult BaglantiHatasi()
    {
        return View();
    }


    public async Task<IActionResult> Index()
    {
        // Her dashboard açıldığında kampanyaları kontrol et
        await _guncelleyici.GuncelleAsync();

        var bugun = DateTime.Today;

        ViewBag.ToplamKampanya = await _db.Campaigns.CountAsync();
        ViewBag.AktifKampanya = await _db.Campaigns
            .CountAsync(c => c.Durum == "Aktif" && c.BitisTarihi >= bugun);
        ViewBag.ToplamSablon = await _db.Templates.CountAsync();
        ViewBag.ToplamKullanici = await _db.Users.CountAsync(u => u.AktifMi);
        ViewBag.ToplamEtiketTipi = await _db.LabelTypes.CountAsync();

        ViewBag.SonKampanyalar = await _db.Campaigns
            .Include(c => c.Sablon)
            .OrderByDescending(c => c.OlusturmaTarihi)
            .Take(5)
            .ToListAsync();

        ViewBag.AktifSayisi = await _db.Campaigns.CountAsync(c => c.Durum == "Aktif");
        ViewBag.PasifSayisi = await _db.Campaigns.CountAsync(c => c.Durum == "Pasif");
        ViewBag.TaslakSayisi = await _db.Campaigns.CountAsync(c => c.Durum == "Taslak");

        return View();
    }
}