using LCM.Domain.Entities;
using LCM.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LCM.Web.Controllers;

[Authorize]
public class LabelTypeController : Controller
{
    private readonly AppDbContext _db;

    public LabelTypeController(AppDbContext db)
    {
        _db = db;
    }

    // Listeleme
    public async Task<IActionResult> Index()
    {
        var liste = await _db.LabelTypes.OrderBy(x => x.EtiketTipi).ToListAsync();
        return View(liste);
    }

    // Yeni Ekle - Form
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View();
    }

    // Yeni Ekle - Kaydet
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(LabelType model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var varMi = await _db.LabelTypes.AnyAsync(x => x.EtiketTipi == model.EtiketTipi);
        if (varMi)
        {
            ModelState.AddModelError("", "Bu etiket tipi zaten mevcut.");
            return View(model);
        }

        _db.LabelTypes.Add(model);
        await _db.SaveChangesAsync();
        TempData["Basari"] = "Etiket tipi başarıyla eklendi.";
        return RedirectToAction("Index");
    }

    // Düzenle - Form
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var kayit = await _db.LabelTypes.FindAsync(id);
        if (kayit == null) return NotFound();
        return View(kayit);
    }

    // Düzenle - Kaydet
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(LabelType model)
    {
        if (!ModelState.IsValid)
            return View(model);

        _db.LabelTypes.Update(model);
        await _db.SaveChangesAsync();
        TempData["Basari"] = "Etiket tipi güncellendi.";
        return RedirectToAction("Index");
    }

    // Sil
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var kayit = await _db.LabelTypes.FindAsync(id);
        if (kayit == null) return NotFound();

        var kullanimdaMi = await _db.Templates.AnyAsync(t => t.EtiketTipId == id);
        if (kullanimdaMi)
        {
            TempData["Hata"] = "Bu etiket tipi şablonlarda kullanılıyor, silinemez.";
            return RedirectToAction("Index");
        }

        _db.LabelTypes.Remove(kayit);
        await _db.SaveChangesAsync();
        TempData["Basari"] = "Etiket tipi silindi.";
        return RedirectToAction("Index");
    }
}