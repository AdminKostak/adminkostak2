using LCM.Domain.Entities;
using LCM.Infrastructure.Data;
using LCM.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LCM.Web.Controllers;

using LCM.Infrastructure.Helpers;


[Authorize]
public class TemplateController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public TemplateController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // Listeleme
    public async Task<IActionResult> Index()
    {
        var liste = await _db.Templates
            .Include(t => t.EtiketTip)
            .Include(t => t.EkleyenKullanici)
            .OrderByDescending(t => t.OlusturmaTarihi)
            .ToListAsync();
        return View(liste);
    }

    // Yeni Ekle - Form
    [Authorize(Roles = "Admin,KampanyaYonetici")]
    public async Task<IActionResult> Create()
    {
        await EtiketTipleriDoldur();
        await LayoutlariDoldur();
        await CaptchaHazirla("SablonEkleCaptcha");
        return View();
    }

    // Yeni Ekle - Kaydet
    [HttpPost]
    [Authorize(Roles = "Admin,KampanyaYonetici")]
    public async Task<IActionResult> Create(TemplateCreateViewModel model)
    {
        // Captcha kontrolü
        var captchaAktif = await _db.SystemSettings
            .FirstOrDefaultAsync(s => s.AyarAdi == "SablonEkleCaptcha");

        if (captchaAktif?.AktifMi == true)
        {
            var dogruCevap = HttpContext.Session.GetInt32("CaptchaCevap");
            if (model.CaptchaCevap != dogruCevap)
            {
                ModelState.AddModelError("CaptchaCevap", "Captcha cevabı hatalı.");
                await EtiketTipleriDoldur();
                await LayoutlariDoldur();
                await CaptchaHazirla("SablonEkleCaptcha");
                return View(model);
            }
        }

        if (!ModelState.IsValid)
        {
            await EtiketTipleriDoldur();
            await LayoutlariDoldur();
            await CaptchaHazirla("SablonEkleCaptcha");
            return View(model);
        }
        // Aynı etiket tipinde başka şablon var mı?
        var mevcutSablon = await _db.Templates
            .FirstOrDefaultAsync(t => t.EtiketTipId == model.EtiketTipId);
        if (mevcutSablon != null)
        {
            ModelState.AddModelError("EtiketTipId", "Bu etiket tipine zaten bir şablon atanmış.");
            await EtiketTipleriDoldur();
            await LayoutlariDoldur();
            await CaptchaHazirla("SablonEkleCaptcha");
            return View(model);
        }
        // Fotoğraf yükle
        var fotoYolu = await FotoYukle(model.SablonFoto!);
        if (fotoYolu == null)
        {
            ModelState.AddModelError("", "Fotoğraf yüklenirken hata oluştu.");
            await EtiketTipleriDoldur();
            await LayoutlariDoldur();
            await CaptchaHazirla("SablonEkleCaptcha");
            return View(model);
        }

        var kullaniciId = int.Parse(User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var sablon = new Template
        {
            SablonAdi = model.SablonAdi,
            Aciklama = model.Aciklama,
            EtiketTipId = model.EtiketTipId,
            SablonFotoYolu = fotoYolu,
            LayoutKodu = model.LayoutKodu,
            EkleyenKullaniciId = kullaniciId,
            OlusturmaTarihi = DateTime.Now
        };

        _db.Templates.Add(sablon);
        await _db.SaveChangesAsync();

        await AlanlariKaydet(sablon.Id, model);

        TempData["Basari"] = "Şablon başarıyla eklendi.";
        return RedirectToAction("Index");
    }

    // Düzenle - Form
    [Authorize(Roles = "Admin,KampanyaYonetici")]
    public async Task<IActionResult> Edit(int id)
    {
        var sablon = await _db.Templates
            .Include(t => t.Alanlar)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (sablon == null) return NotFound();

        await EtiketTipleriDoldur();
        await LayoutlariDoldur();
        await LayoutlariDoldur();


        var model = new TemplateEditViewModel
        {
            Id = sablon.Id,
            SablonAdi = sablon.SablonAdi,
            Aciklama = sablon.Aciklama,
            EtiketTipId = sablon.EtiketTipId,
            MevcutFotoYolu = sablon.SablonFotoYolu,
            LayoutKodu = sablon.LayoutKodu,
            AlanBaslik = sablon.Alanlar.Any(a => a.AlanAdi == "Baslik" && a.AktifMi),
            AlanAltBaslik = sablon.Alanlar.Any(a => a.AlanAdi == "AltBaslik" && a.AktifMi),
            AlanKampanyaNotu = sablon.Alanlar.Any(a => a.AlanAdi == "KampanyaNotu" && a.AktifMi),
            AlanSubheadline = sablon.Alanlar.Any(a => a.AlanAdi == "Subheadline" && a.AktifMi),
            AlanOriginalPrice = sablon.Alanlar.Any(a => a.AlanAdi == "OriginalPrice" && a.AktifMi),
            AlanDiscountedPrice = sablon.Alanlar.Any(a => a.AlanAdi == "DiscountedPrice" && a.AktifMi),
            AlanBuyQuantityText = sablon.Alanlar.Any(a => a.AlanAdi == "BuyQuantityText" && a.AktifMi),
            AlanPayQuantityText = sablon.Alanlar.Any(a => a.AlanAdi == "PayQuantityText" && a.AktifMi),
            AlanDateRange = sablon.Alanlar.Any(a => a.AlanAdi == "DateRange" && a.AktifMi),
            // ↓ Bunlar eksikti:
            AlanHeadline = sablon.Alanlar.Any(a => a.AlanAdi == "Headline" && a.AktifMi),
            AlanMinBasketText = sablon.Alanlar.Any(a => a.AlanAdi == "MinBasketText" && a.AktifMi),
            AlanDetailText = sablon.Alanlar.Any(a => a.AlanAdi == "DetailText" && a.AktifMi),
            AlanCampaignDescription = sablon.Alanlar.Any(a => a.AlanAdi == "CampaignDescription" && a.AktifMi),
            AlanIsLocalProduction = sablon.Alanlar.Any(a => a.AlanAdi == "IsLocalProduction" && a.AktifMi),
            AlanOriginCountry = sablon.Alanlar.Any(a => a.AlanAdi == "OriginCountry" && a.AktifMi),
            AlanUnitPrice = sablon.Alanlar.Any(a => a.AlanAdi == "UnitPrice" && a.AktifMi),
            AlanPriceUpdateDate = sablon.Alanlar.Any(a => a.AlanAdi == "PriceUpdateDate" && a.AktifMi)
        };

        return View(model);
    }

    // Düzenle - Kaydet
    [HttpPost]
    [Authorize(Roles = "Admin,KampanyaYonetici")]
    public async Task<IActionResult> Edit(TemplateEditViewModel model)
    {
        var captchaAktif = await _db.SystemSettings
    .FirstOrDefaultAsync(s => s.AyarAdi == "SablonEkleCaptcha");

        if (captchaAktif?.AktifMi == true)
        {
            var dogruCevap = HttpContext.Session.GetInt32("CaptchaCevap");
            if (model.CaptchaCevap != dogruCevap)
            {
                ModelState.AddModelError("CaptchaCevap", "Captcha cevabı hatalı.");
                await EtiketTipleriDoldur();
                await LayoutlariDoldur();
                await CaptchaHazirla("SablonEkleCaptcha");
                return View(model);
            }
        }
        if (!ModelState.IsValid)
        {
            await EtiketTipleriDoldur();
            await LayoutlariDoldur();
            return View(model);
        }

        var sablon = await _db.Templates
            .Include(t => t.Alanlar)
            .FirstOrDefaultAsync(t => t.Id == model.Id);
        if (sablon == null) return NotFound();
        // Başka bir şablon bu etiket tipini kullanıyor mu?
        var cakisan = await _db.Templates
            .FirstOrDefaultAsync(t => t.EtiketTipId == model.EtiketTipId && t.Id != model.Id);
        if (cakisan != null)
        {
            ModelState.AddModelError("EtiketTipId", "Bu etiket tipi başka bir şablonda kullanılıyor.");
            await EtiketTipleriDoldur();
            return View(model);
        }

        sablon.SablonAdi = model.SablonAdi;
        sablon.Aciklama = model.Aciklama;
        sablon.EtiketTipId = model.EtiketTipId;
        sablon.LayoutKodu = model.LayoutKodu;


        // Yeni fotoğraf yüklendiyse güncelle
        if (model.SablonFoto != null)
        {
            var yeniFoto = await FotoYukle(model.SablonFoto);
            if (yeniFoto != null)
                sablon.SablonFotoYolu = yeniFoto;
        }

        // Alanları güncelle
        _db.TemplateFields.RemoveRange(sablon.Alanlar);
        await _db.SaveChangesAsync();
        await AlanlariKaydet(sablon.Id, model);

        await _db.SaveChangesAsync();
        TempData["Basari"] = "Şablon güncellendi.";
        return RedirectToAction("Index");
    }
    // Template Builder - Görsel Editör
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Builder(int? id)
    {
        await EtiketTipleriDoldur();
        await LayoutlariDoldur();

        if (id.HasValue)
        {
            var sablon = await _db.Templates
                .Include(t => t.Alanlar)
                .FirstOrDefaultAsync(t => t.Id == id.Value);
            if (sablon == null) return NotFound();
            ViewBag.SablonId = sablon.Id;
            ViewBag.SablonAdi = sablon.SablonAdi;
            ViewBag.LayoutKodu = sablon.LayoutKodu;
            ViewBag.LayoutJson = sablon.LayoutJson;
            ViewBag.Alanlar = sablon.Alanlar.Where(a => a.AktifMi).Select(a => a.AlanAdi).ToList();
        }

        return View();
    }
   
    // Template Builder - Kaydet
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveLayout([FromBody] SaveLayoutDto dto)
    {
        var sablon = await _db.Templates.FirstOrDefaultAsync(t => t.Id == dto.SablonId);
        if (sablon == null) return NotFound();

        sablon.LayoutJson = dto.LayoutJson;
        await _db.SaveChangesAsync();

        return Json(new { ok = true });
    }
    // Sil
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var sablon = await _db.Templates
            .Include(t => t.Alanlar)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (sablon == null) return NotFound();

        var kullanimdaMi = await _db.Campaigns.AnyAsync(c => c.SablonId == id);
        if (kullanimdaMi)
        {
            TempData["Hata"] = "Bu şablon kampanyalarda kullanılıyor, silinemez.";
            return RedirectToAction("Index");
        }

        _db.TemplateFields.RemoveRange(sablon.Alanlar);
        _db.Templates.Remove(sablon);
        await _db.SaveChangesAsync();

        TempData["Basari"] = "Şablon silindi.";
        return RedirectToAction("Index");
    }

    // AJAX - Şablon alanlarını getir (Kampanya eklerken kullanılacak)
    [HttpGet]
    public async Task<IActionResult> GetFields(int templateId)
    {
        var alanlar = await _db.TemplateFields
            .Where(f => f.SablonId == templateId && f.AktifMi)
            .Select(f => f.AlanAdi)
            .ToListAsync();
        return Json(alanlar);
    }

    // ── Yardımcı Metodlar ────────────────────────────────

    private async Task<string?> FotoYukle(IFormFile foto)
    {
        try
        {
            var izinliUzantilar = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var uzanti = Path.GetExtension(foto.FileName).ToLower();
            if (!izinliUzantilar.Contains(uzanti)) return null;
            if (foto.Length > 5 * 1024 * 1024) return null; // max 5MB

            var klasor = Path.Combine(_env.WebRootPath, "uploads", "templates");
            Directory.CreateDirectory(klasor);

            var dosyaAdi = $"{Guid.NewGuid()}{uzanti}";
            var tamYol = Path.Combine(klasor, dosyaAdi);

            using var stream = new FileStream(tamYol, FileMode.Create);
            await foto.CopyToAsync(stream);

            return $"/uploads/templates/{dosyaAdi}";
        }
        catch { return null; }
    }

    private async Task AlanlariKaydet(int sablonId, TemplateCreateViewModel model)
    {
        var alanlar = new List<TemplateField>();
        if (model.AlanBaslik) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "Baslik", AktifMi = true });
        if (model.AlanAltBaslik) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "AltBaslik", AktifMi = true });
        if (model.AlanKampanyaNotu) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "KampanyaNotu", AktifMi = true });
        if (model.AlanSubheadline) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "Subheadline", AktifMi = true });
        if (model.AlanOriginalPrice) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "OriginalPrice", AktifMi = true });
        if (model.AlanDiscountedPrice) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "DiscountedPrice", AktifMi = true });
        if (model.AlanBuyQuantityText) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "BuyQuantityText", AktifMi = true });
        if (model.AlanPayQuantityText) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "PayQuantityText", AktifMi = true });
        if (model.AlanDateRange) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "DateRange", AktifMi = true });
        if (model.AlanHeadline) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "Headline", AktifMi = true });
        if (model.AlanMinBasketText) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "MinBasketText", AktifMi = true });
        if (model.AlanDetailText) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "DetailText", AktifMi = true });
        if (model.AlanCampaignDescription) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "CampaignDescription", AktifMi = true });
        if (model.AlanIsLocalProduction) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "IsLocalProduction", AktifMi = true });
        if (model.AlanOriginCountry) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "OriginCountry", AktifMi = true });
        if (model.AlanUnitPrice) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "UnitPrice", AktifMi = true });
        if (model.AlanPriceUpdateDate) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "PriceUpdateDate", AktifMi = true });
        _db.TemplateFields.AddRange(alanlar);
        await _db.SaveChangesAsync();
    }

    private async Task AlanlariKaydet(int sablonId, TemplateEditViewModel model)
    {
        var alanlar = new List<TemplateField>();
        if (model.AlanBaslik) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "Baslik", AktifMi = true });
        if (model.AlanAltBaslik) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "AltBaslik", AktifMi = true });
        if (model.AlanKampanyaNotu) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "KampanyaNotu", AktifMi = true });
        if (model.AlanSubheadline) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "Subheadline", AktifMi = true });
        if (model.AlanOriginalPrice) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "OriginalPrice", AktifMi = true });
        if (model.AlanDiscountedPrice) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "DiscountedPrice", AktifMi = true });
        if (model.AlanBuyQuantityText) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "BuyQuantityText", AktifMi = true });
        if (model.AlanPayQuantityText) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "PayQuantityText", AktifMi = true });
        if (model.AlanDateRange) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "DateRange", AktifMi = true });
        if (model.AlanHeadline) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "Headline", AktifMi = true });
        if (model.AlanMinBasketText) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "MinBasketText", AktifMi = true });
        if (model.AlanDetailText) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "DetailText", AktifMi = true });
        if (model.AlanCampaignDescription) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "CampaignDescription", AktifMi = true });
        if (model.AlanIsLocalProduction) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "IsLocalProduction", AktifMi = true });
        if (model.AlanOriginCountry) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "OriginCountry", AktifMi = true });
        if (model.AlanUnitPrice) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "UnitPrice", AktifMi = true });
        if (model.AlanPriceUpdateDate) alanlar.Add(new TemplateField { SablonId = sablonId, AlanAdi = "PriceUpdateDate", AktifMi = true });
        _db.TemplateFields.AddRange(alanlar);
        await _db.SaveChangesAsync();
    }

    private async Task EtiketTipleriDoldur()
    {
        var tipler = await _db.LabelTypes.OrderBy(t => t.EtiketTipi).ToListAsync();
        ViewBag.EtiketTipleri = new SelectList(tipler, "Id", "EtiketTipi");
    }
    private async Task LayoutlariDoldur()
    {
        var layoutlar = await _db.Layouts
            .OrderBy(l => l.Ad)
            .Select(l => new { l.Id, l.Ad, l.LayoutKodu, l.LayoutJson })
            .ToListAsync();
        ViewBag.Layoutlar = layoutlar;
    }
    private async Task CaptchaHazirla(string ayarAdi)
    {
        var ayar = await _db.SystemSettings
            .FirstOrDefaultAsync(s => s.AyarAdi == ayarAdi);

        if (ayar?.AktifMi == true)
        {
            var rnd = new Random();
            int s1 = rnd.Next(1, 10), s2 = rnd.Next(1, 10);
            HttpContext.Session.SetInt32("CaptchaCevap", s1 + s2);
            ViewBag.CaptchaAktif = true;
            ViewBag.CaptchaSoru = $"{s1} + {s2} = ?";
        }
        else
        {
            ViewBag.CaptchaAktif = false;
        }
    }
    public class SaveLayoutDto
    {
        public int SablonId { get; set; }
        public string LayoutJson { get; set; } = string.Empty;
    }
}