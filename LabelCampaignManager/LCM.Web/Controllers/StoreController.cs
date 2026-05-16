using LCM.Domain.Entities;
using LCM.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LCM.Web.Controllers;

[Authorize(Roles = "Admin")]
public class StoreController : Controller
{
    private readonly AppDbContext _db;

    public StoreController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var stores = await _db.Stores
            .OrderBy(s => s.StoreCode)
            .ToListAsync();
        return View(stores);
    }

    public IActionResult Create()
    {
        return View(new Store());
    }

    [HttpPost]
    public async Task<IActionResult> Create(Store model)
    {
        if (!ModelState.IsValid)
            return View(model);

        _db.Stores.Add(model);
        await _db.SaveChangesAsync();
        TempData["Basari"] = "Şube eklendi.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var store = await _db.Stores.FindAsync(id);
        if (store == null) return NotFound();
        return View(store);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Store model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var store = await _db.Stores.FindAsync(model.Id);
        if (store == null) return NotFound();

        store.StoreCode = model.StoreCode;
        store.StoreName = model.StoreName;
        store.AktifMi = model.AktifMi;

        await _db.SaveChangesAsync();
        TempData["Basari"] = "Şube güncellendi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var store = await _db.Stores.FindAsync(id);
        if (store == null) return NotFound();

        _db.Stores.Remove(store);
        await _db.SaveChangesAsync();
        TempData["Basari"] = "Şube silindi.";
        return RedirectToAction("Index");
    }
}