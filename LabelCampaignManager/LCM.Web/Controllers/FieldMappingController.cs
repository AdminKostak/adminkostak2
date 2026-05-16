using LCM.Domain.Entities;
using LCM.Infrastructure.Data;
using LCM.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LCM.Web.Controllers;

[Authorize(Roles = "Admin,KampanyaYonetici")]
public class FieldMappingController : Controller
{
    private readonly AppDbContext _db;

    public FieldMappingController(AppDbContext db)
    {
        _db = db;
    }

    // Şablon seçim ekranı
    public async Task<IActionResult> Index()
    {
        var sablonlar = await _db.Templates
            .Include(t => t.EtiketTip)
            .OrderBy(t => t.SablonAdi)
            .ToListAsync();
        return View(sablonlar);
    }

    // Eşleştirme ekranı
    public async Task<IActionResult> Manage(int sablonId)
    {
        var sablon = await _db.Templates
            .Include(t => t.EtiketTip)
            .Include(t => t.Alanlar)
            .FirstOrDefaultAsync(t => t.Id == sablonId);

        if (sablon == null) return NotFound();

        var eslAlanlari = await _db.EslFields
            .OrderBy(e => e.VariableName)
            .ToListAsync();

        var mevcutEslesmeler = await _db.FieldMappings
            .Include(m => m.EslField)
            .Where(m => m.SablonId == sablonId)
            .ToListAsync();

        var sabitSira = new List<string>
        {
            "Baslik", "AltBaslik", "KampanyaNotu", "MinBasketText",
            "Headline", "Subheadline", "DetailText",
            "OriginalPrice", "DiscountedPrice",
            "DateRange", "CampaignDescription",
            "IsLocalProduction", "OriginCountry", "UnitPrice", "PriceUpdateDate",
            "BuyQuantityText", "PayQuantityText"
        };

        var model = new FieldMappingViewModel
        {
            SablonId = sablon.Id,
            SablonAdi = sablon.SablonAdi,
            EtiketTipi = sablon.EtiketTip.EtiketTipi,
            SablonAlanlari = sablon.Alanlar
                .Where(a => a.AktifMi)
                .OrderBy(a => sabitSira.IndexOf(a.AlanAdi) == -1
                    ? int.MaxValue
                    : sabitSira.IndexOf(a.AlanAdi))
                .Select(a => a.AlanAdi)
                .ToList(),
            EslAlanlari = eslAlanlari.Select(e => new EslFieldItem
            {
                Id = e.Id,
                VariableName = e.VariableName,
                DataType = e.DataType
            }).ToList(),
            MevcutEslesmeler = sablon.Alanlar
                .Where(a => a.AktifMi)
                .Select(a => new MappingItem
                {
                    BizimAlanAdi = a.AlanAdi,
                    EslFieldId = mevcutEslesmeler
                        .FirstOrDefault(m => m.BizimAlanAdi == a.AlanAdi)?.EslFieldId,
                    EslVariableName = mevcutEslesmeler
                        .FirstOrDefault(m => m.BizimAlanAdi == a.AlanAdi)?.EslField?.VariableName
                })
                .ToList()
        };

        return View(model);
    }

    // Kaydet
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] FieldMappingSaveViewModel model)
    {
        // Mevcut eşleştirmeleri sil
        var eskiler = await _db.FieldMappings
            .Where(m => m.SablonId == model.SablonId)
            .ToListAsync();
        _db.FieldMappings.RemoveRange(eskiler);

        // Yenilerini ekle
        foreach (var item in model.Eslesmeler)
        {
            if (item.EslFieldId.HasValue)
            {
                _db.FieldMappings.Add(new FieldMapping
                {
                    SablonId = model.SablonId,
                    BizimAlanAdi = item.BizimAlanAdi,
                    EslFieldId = item.EslFieldId.Value
                });
            }
        }

        await _db.SaveChangesAsync();
        return Json(new { basari = true });
    }
}