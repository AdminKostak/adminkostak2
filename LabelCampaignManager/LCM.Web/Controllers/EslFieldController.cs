using LCM.Domain.Entities;
using LCM.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LCM.Web.Controllers;

[Authorize(Roles = "Admin,KampanyaYonetici")]
public class EslFieldController : Controller
{
    private readonly AppDbContext _db;

    public EslFieldController(AppDbContext db)
    {
        _db = db;
    }

    // Listeleme
    public async Task<IActionResult> Index()
    {
        var liste = await _db.EslFields
            .OrderBy(e => e.VariableName)
            .ToListAsync();
        return View(liste);
    }

    // Ekle - Form
    public IActionResult Create()
    {
        return View();
    }

    // Ekle - Kaydet
    [HttpPost]
    public async Task<IActionResult> Create(EslField model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var mevcut = await _db.EslFields
            .FirstOrDefaultAsync(e => e.VariableName == model.VariableName);
        if (mevcut != null)
        {
            ModelState.AddModelError("VariableName", "Bu değişken adı zaten mevcut.");
            return View(model);
        }

        _db.EslFields.Add(model);
        await _db.SaveChangesAsync();
        TempData["Basari"] = "ESL alanı eklendi.";
        return RedirectToAction("Index");
    }

    // Düzenle - Form
    public async Task<IActionResult> Edit(int id)
    {
        var alan = await _db.EslFields.FindAsync(id);
        if (alan == null) return NotFound();
        return View(alan);
    }

    // Düzenle - Kaydet
    [HttpPost]
    public async Task<IActionResult> Edit(EslField model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var cakisan = await _db.EslFields
            .FirstOrDefaultAsync(e => e.VariableName == model.VariableName && e.Id != model.Id);
        if (cakisan != null)
        {
            ModelState.AddModelError("VariableName", "Bu değişken adı başka bir alanda kullanılıyor.");
            return View(model);
        }

        var alan = await _db.EslFields.FindAsync(model.Id);
        if (alan == null) return NotFound();

        alan.VariableName = model.VariableName;
        alan.DataType = model.DataType;
        alan.IsRequired = model.IsRequired;
        alan.Aciklama = model.Aciklama;

        await _db.SaveChangesAsync();
        TempData["Basari"] = "ESL alanı güncellendi.";
        return RedirectToAction("Index");
    }

    // Sil
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var alan = await _db.EslFields
            .Include(e => e.Mappings)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (alan == null) return NotFound();

        if (alan.Mappings.Any())
        {
            TempData["Hata"] = "Bu alan eşleştirmelerde kullanılıyor, silinemez.";
            return RedirectToAction("Index");
        }

        _db.EslFields.Remove(alan);
        await _db.SaveChangesAsync();
        TempData["Basari"] = "ESL alanı silindi.";
        return RedirectToAction("Index");
    }
}