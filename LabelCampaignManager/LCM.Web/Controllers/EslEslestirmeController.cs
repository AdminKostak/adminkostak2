using LCM.Domain.Entities;
using LCM.Infrastructure.Data;
using LCM.Infrastructure.Services;
using LCM.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LCM.Web.Controllers;

[Authorize(Roles = "Admin,Operator")]
public class EslEslestirmeController : Controller
{
    private readonly AppDbContext _db;
    private readonly EslGonderimService _eslGonderimService;

    public EslEslestirmeController(AppDbContext db, EslGonderimService eslGonderimService)
    {
        _db = db;
        _eslGonderimService = eslGonderimService;
    }

    private int AktifKullaniciId()
    {
        return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    // Ana ekran
    public async Task<IActionResult> Index()
    {
        var kullaniciId = AktifKullaniciId();
        var kullanici = await _db.Users
            .Include(u => u.YetkiliStoreler)
            .ThenInclude(us => us.Store)
            .FirstOrDefaultAsync(u => u.Id == kullaniciId);

        if (kullanici == null) return NotFound();

        var model = new EslEslestirmeAnaSayfaViewModel
        {
            YetkiliStoreler = kullanici.YetkiliStoreler.Select(us => us.Store).ToList(),
            HizliEslestirmeIzni = kullanici.HizliEslestirmeIzni,
            CokluEslestirmeIzni = kullanici.CokluEslestirmeIzni
        };

        return View(model);
    }

    // Normal eşleştirme ekranı
    public async Task<IActionResult> NormalEslestirme(int storeId)
    {
        var kullaniciId = AktifKullaniciId();
        var yetkili = await _db.UserStores
            .AnyAsync(us => us.UserId == kullaniciId && us.StoreId == storeId);
        if (!yetkili) return Forbid();

        var kampanyalar = await _db.Campaigns
    .Include(c => c.CampaignStores)
    .Include(c => c.Sablon)
    .Where(c => c.CampaignStores.Any(cs => cs.StoreId == storeId)
             && c.Durum == "Aktif")
    .ToListAsync();

        ViewBag.StoreId = storeId;
        ViewBag.Kampanyalar = kampanyalar;
        var kullanici = await _db.Users.FindAsync(kullaniciId);
        ViewBag.CokluEslestirmeIzni = kullanici?.CokluEslestirmeIzni ?? false;
        var onayPopupAktif = await _db.SystemSettings
    .FirstOrDefaultAsync(s => s.AyarAdi == "EslOnayPopupAktif");
        ViewBag.OnayPopupAktif = onayPopupAktif?.AktifMi ?? true;
        ViewBag.KampanyaLayoutlar = await KampanyaLayoutDictOlustur(kampanyalar);
        return View();
    }

    // Hızlı eşleştirme ekranı
    public async Task<IActionResult> HizliEslestirme(int storeId)
    {
        var kullaniciId = AktifKullaniciId();
        var kullanici = await _db.Users.FindAsync(kullaniciId);
        if (kullanici == null) return NotFound();


        var yetkili = await _db.UserStores
            .AnyAsync(us => us.UserId == kullaniciId && us.StoreId == storeId);
        if (!yetkili) return Forbid();

        var kampanyalar = await _db.Campaigns
   .Include(c => c.CampaignStores)
   .Include(c => c.Sablon)
   .Where(c => c.CampaignStores.Any(cs => cs.StoreId == storeId)
            && c.Durum == "Aktif"
            && !_db.EslEslestirmeler.Any(e => e.KampanyaId == c.Id
                                           && e.StoreId == storeId
                                           && e.BasariliMi))
   .ToListAsync();

        ViewBag.StoreId = storeId;
        ViewBag.Kampanyalar = kampanyalar;
        ViewBag.KampanyaLayoutlar = await KampanyaLayoutDictOlustur(kampanyalar);
        return View();
    }

    // Eşleştirme işlemi
    [IgnoreAntiforgeryToken]
    [HttpPost]
    public async Task<IActionResult> Eslestir([FromBody] EslEslestirmeIslemViewModel model)
    {
        if (model == null || model.EslBarkodlar == null || !model.EslBarkodlar.Any())
            return Json(new { basarili = false, mesaj = "Model boş geldi." });
        var kullaniciId = AktifKullaniciId();
        var tumBasarili = true;
        var hataMesaj = "";

        foreach (var barkod in model.EslBarkodlar)
        {
            var apiSonuc = await _eslGonderimService.EslEslestirAsync(
                barkod, model.KampanyaId, model.StoreId, kullaniciId);

            var eslestirme = new EslEslestirme
            {
                EslBarkod = barkod,
                KampanyaId = model.KampanyaId,
                StoreId = model.StoreId,
                KullaniciId = kullaniciId,
                EslestirmeTarihi = DateTime.Now,
                IslemTipi = model.EslBarkodlar.Count > 1 ? "Çoklu" : "Tekli",
                BasariliMi = apiSonuc.Basarili,
                HataMesaji = apiSonuc.HataMesaji,
                GonderilenJson = apiSonuc.GonderilenJson

            };
            var mevcutEslestirme = await _db.EslEslestirmeler
    .FirstOrDefaultAsync(e => e.EslBarkod == barkod);

            if (mevcutEslestirme != null)
                eslestirme.Override = true;
            _db.EslEslestirmeler.Add(eslestirme);

            if (!apiSonuc.Basarili)
            {
                tumBasarili = false;
                hataMesaj = apiSonuc.HataMesaji ?? "API hatası.";
            }
        }

        await _db.SaveChangesAsync();

        return Json(new
        {
            basarili = tumBasarili,
            mesaj = tumBasarili ? "Eşleştirme başarılı." : hataMesaj
        });
    }

    // Operatör log ekranı
    public async Task<IActionResult> Loglarim()
    {
        var kullaniciId = AktifKullaniciId();
        var model = new OperatorLogViewModel
        {
            Eslestirmeler = await _db.EslEslestirmeler
                .Include(e => e.Kampanya)
                .Include(e => e.Store)
                .Where(e => e.KullaniciId == kullaniciId)
                .OrderByDescending(e => e.EslestirmeTarihi)
                .ToListAsync()
        };
        return View(model);
    }

    // Admin log ekranı
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminLog()
    {
        var eslestirmeler = await _db.EslEslestirmeler
            .Include(e => e.Kampanya)
            .Include(e => e.Store)
            .Include(e => e.Kullanici)
            .OrderByDescending(e => e.EslestirmeTarihi)
            .ToListAsync();
        return View(eslestirmeler);
    }
    private async Task<Dictionary<int, string>> KampanyaLayoutDictOlustur(List<Campaign> kampanyalar)
    {
        var result = new Dictionary<int, string>();

        var layoutKodlari = kampanyalar
            .Where(k => k.Sablon != null && !string.IsNullOrEmpty(k.Sablon.LayoutKodu))
            .Select(k => k.Sablon!.LayoutKodu!)
            .Distinct()
            .ToList();

        if (!layoutKodlari.Any()) return result;

        // Tüm layout'ları çek, bellekte filtrele (OPENJSON hatası önlenir)
        var layoutlar = await _db.Layouts
            .ToListAsync();

        var filtreliLayoutlar = layoutlar
            .Where(l => layoutKodlari.Contains(l.LayoutKodu))
            .ToList();

        foreach (var kampanya in kampanyalar)
        {
            var layoutKodu = kampanya.Sablon?.LayoutKodu;
            if (string.IsNullOrEmpty(layoutKodu)) continue;
            var layout = filtreliLayoutlar.FirstOrDefault(l => l.LayoutKodu == layoutKodu);
            if (layout?.LayoutJson != null)
                result[kampanya.Id] = layout.LayoutJson;
        }
        return result;
    }

}