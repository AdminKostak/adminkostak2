using LCM.Domain.Entities;
using LCM.Infrastructure.Data;
using LCM.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LCM.Web.Controllers;

[Authorize]
public class DigitalLabelSpecController : Controller
{
    private readonly AppDbContext _db;

    // Sabit renk listesi
    private static readonly List<(string Ad, string Kod)> Renkler = new()
    {
        ("Kırmızı", "#ef4444"),
        ("Beyaz",   "#f9fafb"),
        ("Sarı",    "#eab308"),
        ("Siyah",   "#111111"),
        ("Mavi",    "#3b82f6")
    };

    public DigitalLabelSpecController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var liste = await _db.DigitalLabelSpecs
            .Include(d => d.EtiketTip)
            .OrderBy(d => d.EtiketAdi)
            .ToListAsync();
        return View(liste);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var kayit = await _db.DigitalLabelSpecs
            .Include(d => d.EtiketTip)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (kayit == null) return NotFound();
        return View(kayit);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await EtiketTipleriDoldur();
        RenkleriDoldur();
        return View(new DigitalLabelSpecViewModel());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(DigitalLabelSpecViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await EtiketTipleriDoldur();
            RenkleriDoldur();
            return View(model);
        }

        var kayit = new DigitalLabelSpec
        {
            EtiketAdi = model.EtiketAdi,
            Inch = model.Inch,
            Olculer = model.Olculer,
            DPI = model.DPI,
            TahminiPilOmru = model.TahminiPilOmru,
            DayanabildigiSicaklik = model.DayanabildigiSicaklik,
            ActiveDisplayArea = model.ActiveDisplayArea,
            Dimensions = model.Dimensions,
            PageSwitch = model.PageSwitch,
            ViewingAngle = model.ViewingAngle,
            EtiketTipId = model.EtiketTipId,
            DesteklenenRenkler = string.Join(",", model.DesteklenenRenkler),
            LedDesteklenenRenkler = string.Join(",", model.LedDesteklenenRenkler)
        };

        _db.DigitalLabelSpecs.Add(kayit);
        await _db.SaveChangesAsync();
        TempData["Basari"] = "Dijital etiket özelliği eklendi.";
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var kayit = await _db.DigitalLabelSpecs.FindAsync(id);
        if (kayit == null) return NotFound();

        await EtiketTipleriDoldur();
        RenkleriDoldur();

        var model = new DigitalLabelSpecViewModel
        {
            Id = kayit.Id,
            EtiketAdi = kayit.EtiketAdi,
            Inch = kayit.Inch,
            Olculer = kayit.Olculer,
            DPI = kayit.DPI,
            TahminiPilOmru = kayit.TahminiPilOmru,
            DayanabildigiSicaklik = kayit.DayanabildigiSicaklik,
            ActiveDisplayArea = kayit.ActiveDisplayArea,
            Dimensions = kayit.Dimensions,
            PageSwitch = kayit.PageSwitch,
            ViewingAngle = kayit.ViewingAngle,
            EtiketTipId = kayit.EtiketTipId,
            DesteklenenRenkler = kayit.DesteklenenRenkler?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim()).ToList() ?? new(),
            LedDesteklenenRenkler = kayit.LedDesteklenenRenkler?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim()).ToList() ?? new()
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(DigitalLabelSpecViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await EtiketTipleriDoldur();
            RenkleriDoldur();
            return View(model);
        }

        var kayit = await _db.DigitalLabelSpecs.FindAsync(model.Id);
        if (kayit == null) return NotFound();

        kayit.EtiketAdi = model.EtiketAdi;
        kayit.Inch = model.Inch;
        kayit.Olculer = model.Olculer;
        kayit.DPI = model.DPI;
        kayit.TahminiPilOmru = model.TahminiPilOmru;
        kayit.DayanabildigiSicaklik = model.DayanabildigiSicaklik;
        kayit.ActiveDisplayArea = model.ActiveDisplayArea;
        kayit.Dimensions = model.Dimensions;
        kayit.PageSwitch = model.PageSwitch;
        kayit.ViewingAngle = model.ViewingAngle;
        kayit.EtiketTipId = model.EtiketTipId;
        kayit.DesteklenenRenkler = string.Join(",", model.DesteklenenRenkler);
        kayit.LedDesteklenenRenkler = string.Join(",", model.LedDesteklenenRenkler);

        await _db.SaveChangesAsync();
        TempData["Basari"] = "Güncellendi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var kayit = await _db.DigitalLabelSpecs.FindAsync(id);
        if (kayit == null) return NotFound();
        _db.DigitalLabelSpecs.Remove(kayit);
        await _db.SaveChangesAsync();
        TempData["Basari"] = "Kayıt silindi.";
        return RedirectToAction("Index");
    }

    private async Task EtiketTipleriDoldur()
    {
        var tipler = await _db.LabelTypes.OrderBy(t => t.EtiketTipi).ToListAsync();
        ViewBag.EtiketTipleri = new SelectList(tipler, "Id", "EtiketTipi");
    }

    private void RenkleriDoldur()
    {
        ViewBag.Renkler = Renkler;
    }
}