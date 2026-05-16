using LCM.Domain.Entities;
using LCM.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LCM.Web.Controllers;

[Authorize(Roles = "Admin,KampanyaYonetici")]
public class LayoutController : Controller
{
    private readonly AppDbContext _db;

    public LayoutController(AppDbContext db)
    {
        _db = db;
    }

    // Listeleme
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var liste = await _db.Layouts
            .OrderByDescending(l => l.OlusturmaTarihi)
            .ToListAsync();
        return View(liste);
    }

    // Yeni Layout - Form
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View();
    }
    [Authorize(Roles = "Admin")]
    // Yeni Layout - Kaydet
    [HttpPost]
    public async Task<IActionResult> Create(string ad, string layoutKodu)
    {
        if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(layoutKodu))
        {
            TempData["Hata"] = "Ad ve kod zorunludur.";
            return View();
        }

        var layout = new Layout
        {
            Ad = ad,
            LayoutKodu = layoutKodu,
            OlusturmaTarihi = DateTime.Now
        };

        _db.Layouts.Add(layout);
        await _db.SaveChangesAsync();

        TempData["Basari"] = "Layout oluşturuldu. Şimdi tasarımı çizebilirsin.";
        return RedirectToAction("Builder", new { id = layout.Id });
    }


    // Builder - Görsel Editör
    public async Task<IActionResult> Builder(int id)
    {
        var layout = await _db.Layouts.FirstOrDefaultAsync(l => l.Id == id);
        if (layout == null) return NotFound();

        ViewBag.LayoutId = layout.Id;
        ViewBag.LayoutAd = layout.Ad;
        ViewBag.LayoutJson = layout.LayoutJson;

        var fontListesi = await _db.Fonts
            .Select(f => f.FontAdi)
            .Distinct()
            .OrderBy(f => f)
            .ToListAsync();
        ViewBag.FontListesi = fontListesi;

        return View();

    }

    // Builder - JSON Kaydet
    [HttpPost]
    public async Task<IActionResult> SaveLayout([FromBody] SaveLayoutDto dto)
    {
        var layout = await _db.Layouts.FirstOrDefaultAsync(l => l.Id == dto.LayoutId);
        if (layout == null) return NotFound();

        layout.LayoutJson = dto.LayoutJson;
        await _db.SaveChangesAsync();

        return Json(new { ok = true });
    }

    // AJAX - Tüm layoutları getir (şablon oluştururken dropdown için)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var liste = await _db.Layouts
            .Select(l => new { l.Id, l.Ad, l.LayoutKodu, l.LayoutJson })
            .ToListAsync();
        return Json(liste);
    }

    // AJAX - Tek layout getir (önizleme için)
    [HttpGet]
    public async Task<IActionResult> Get(int id)
    {
        var layout = await _db.Layouts.FirstOrDefaultAsync(l => l.Id == id);
        if (layout == null) return NotFound();
        return Json(new { layout.Id, layout.Ad, layout.LayoutKodu, layout.LayoutJson });
    }

    // Sil
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var layout = await _db.Layouts.FirstOrDefaultAsync(l => l.Id == id);
        if (layout == null) return NotFound();

        _db.Layouts.Remove(layout);
        await _db.SaveChangesAsync();

        TempData["Basari"] = "Layout silindi.";
        return RedirectToAction("Index");
    }

    public class SaveLayoutDto
    {
        public int LayoutId { get; set; }
        public string LayoutJson { get; set; } = string.Empty;
    }
}