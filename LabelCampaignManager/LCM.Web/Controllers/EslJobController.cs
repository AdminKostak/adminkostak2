using LCM.Domain.Entities;
using LCM.Infrastructure.Data;
using LCM.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace LCM.Web.Controllers;

[Authorize(Roles = "Admin")]
public class EslJobController : Controller
{
    private readonly AppDbContext _db;
    private readonly EslGonderimService _eslGonderimService;

    public EslJobController(AppDbContext db, EslGonderimService eslGonderimService)
    {
        _db = db;
        _eslGonderimService = eslGonderimService;
    }
    public async Task<IActionResult> Index()
    {
        var jobs = await _db.EslJobs
            .Include(j => j.OlusturanKullanici)
            .OrderBy(j => j.CalismaZamani)
            .ToListAsync();
        return View(jobs);
    }

    public IActionResult Create()
    {
        return View(new EslJob());
    }

    [HttpPost]
    public async Task<IActionResult> Create(EslJob model)
    {
        var kullaniciId = int.Parse(User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        // Navigation property'ler form'dan gelmez, ModelState'ten temizle
        ModelState.Remove("OlusturanKullanici");
        ModelState.Remove("OlusturanKullaniciId");

        if (!ModelState.IsValid)
            return View(model);

        model.OlusturanKullaniciId = kullaniciId;
        model.OlusturmaTarihi = DateTime.Now;
        _db.EslJobs.Add(model);
        await _db.SaveChangesAsync();
        TempData["Basari"] = "Job oluşturuldu.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var job = await _db.EslJobs.FindAsync(id);
        if (job == null) return NotFound();
        return View(job);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EslJob model)
    {
        // Navigation property'ler form'dan gelmez, ModelState'ten temizle
        ModelState.Remove("OlusturanKullanici");
        ModelState.Remove("OlusturanKullaniciId");

        if (!ModelState.IsValid)
            return View(model);

        var job = await _db.EslJobs.FindAsync(model.Id);
        if (job == null) return NotFound();
        job.JobAdi = model.JobAdi;
        job.CalismaZamani = model.CalismaZamani;
        job.AktifGonder = model.AktifGonder;
        job.PlanlanmisGonder = model.PlanlanmisGonder;
        job.AktifMi = model.AktifMi;
        await _db.SaveChangesAsync();
        TempData["Basari"] = "Job güncellendi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var job = await _db.EslJobs.FindAsync(id);
        if (job == null) return NotFound();

        _db.EslJobs.Remove(job);
        await _db.SaveChangesAsync();
        TempData["Basari"] = "Job silindi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleAktif(int id)
    {
        var job = await _db.EslJobs.FindAsync(id);
        if (job == null) return NotFound();

        job.AktifMi = !job.AktifMi;
        await _db.SaveChangesAsync();

        TempData["Basari"] = job.AktifMi ? "Job aktifleştirildi." : "Job pasife alındı.";
        return RedirectToAction("Index");
    }
    // Job'ın göndereceği kampanyaların JSON önizlemesi
    [HttpGet]
    public async Task<IActionResult> JobOnizle(int id)
    {
        try
        {
            var job = await _db.EslJobs.FindAsync(id);
            if (job == null)
                return Json(new { success = false, message = "Job bulunamadı." });

            var ayar = await _db.EslApiSettings.FirstOrDefaultAsync();
            if (ayar == null)
                return Json(new { success = false, message = "ESL API ayarları bulunamadı." });

            // Job'ın durumlarına göre kampanyaları çek
            var durumlar = new List<string>();
            if (job.AktifGonder) durumlar.Add("Aktif");
            if (job.PlanlanmisGonder) durumlar.Add("Planlanmış");

            if (!durumlar.Any())
                return Json(new { success = false, message = "Job'da gönderilecek durum seçili değil." });

            // Önce DB'den çek, sonra bellekte işle
            // OPENJSON hatasını önlemek için Contains yerine açık koşul kullan
            var kampanyalar = await _db.Campaigns
                .Include(c => c.CampaignStores).ThenInclude(cs => cs.Store)
                .Include(c => c.Sablon).ThenInclude(s => s.Alanlar)
                .Include(c => c.Sablon).ThenInclude(s => s.EtiketTip)
                .Where(c =>
                    (job.AktifGonder && c.Durum == "Aktif") ||
                    (job.PlanlanmisGonder && c.Durum == "Planlanmış"))
                .ToListAsync();

            if (!kampanyalar.Any())
                return Json(new { success = false, message = "Gönderilecek kampanya bulunamadı." });

            var mappings = await _db.FieldMappings
                .Include(m => m.EslField)
                .ToListAsync();

            var serializerOpt = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            // Bellekte grupla — SQL'e çevrilmez
            var tumStoreler = kampanyalar
                .SelectMany(c => c.CampaignStores.Select(cs => cs.Store))
                .ToList();

            var subeler = tumStoreler
                .GroupBy(s => s.StoreCode)
                .Select(g => g.First())
                .ToList();
            var subeJsonlari = new List<object>();

            foreach (var sube in subeler)
            {
                var subeKampanyalari = kampanyalar
                    .Where(c => c.CampaignStores.Any(cs => cs.Store.StoreCode == sube.StoreCode))
                    .ToList();

                var items = new List<Dictionary<string, string>>();

                foreach (var kampanya in subeKampanyalari)
                {
                    var item = new Dictionary<string, string>();
                    var sablonMappings = mappings
                        .Where(m => m.SablonId == kampanya.SablonId)
                        .ToList();

                    foreach (var mapping in sablonMappings)
                    {
                        var deger = JobAlanDegerAl(kampanya, mapping.BizimAlanAdi);
                        if (deger != null && mapping.EslField != null)
                            item[mapping.EslField.VariableName] = deger;
                    }

                    if (!item.ContainsKey("sku")) item["sku"] = kampanya.Sku.ToString();
                    if (!item.ContainsKey("ean")) item["ean"] = kampanya.Barkod;

                    var gradeVal = kampanya.Sablon?.EtiketTip?.EtiketTipi;
                    if (!string.IsNullOrEmpty(gradeVal))
                        item["grade"] = gradeVal;

                    items.Add(item);
                }

                var payload = new
                {
                    customerStoreCode = ayar.CustomerStoreCode,
                    storeCode = sube.StoreCode,
                    batchNo = "",
                    items
                };

                subeJsonlari.Add(new
                {
                    storeCode = sube.StoreCode,
                    storeName = sube.StoreName,
                    kampanyaSayisi = subeKampanyalari.Count,
                    json = System.Text.Json.JsonSerializer.Serialize(payload, serializerOpt)
                });
            }

            return Json(new
            {
                success = true,
                jobAdi = job.JobAdi,
                toplamKampanya = kampanyalar.Count,
                toplamSube = subeler.Count,
                subeler = subeJsonlari
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Hata: {ex.Message}" });
        }
    }

    // Job'ı manuel çalıştır
    [HttpPost]
    public async Task<IActionResult> JobCalistir(int id)
    {
        try
        {
            var job = await _db.EslJobs.FindAsync(id);
            if (job == null)
                return Json(new { success = false, message = "Job bulunamadı." });

            var kullaniciId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var kullanici = await _db.Users.FindAsync(kullaniciId);

            var sonuclar = await _eslGonderimService.GonderAsync(
                aktifGonder: job.AktifGonder,
                planlanmisGonder: job.PlanlanmisGonder,
                tetikleyenKullaniciId: kullaniciId,
                tetikleyenJobId: job.Id,
                tetikleyenAciklama: $"Manuel Job: {kullanici?.KullaniciAdi} ({job.JobAdi})"
            );

            // Son çalışma zamanını güncelle
            job.SonCalisma = DateTime.Now;
            await _db.SaveChangesAsync();

            var basariliSayisi = sonuclar.Count(s => s.Basarili);
            var basarisizSayisi = sonuclar.Count(s => !s.Basarili);
            var toplamSube = sonuclar.Count;

            var subeSonuclari = sonuclar.Select(s => new
            {
                storeCode = s.StoreCode,
                basarili = s.Basarili,
                hata = s.HataMesaji
            }).ToList();

            if (basariliSayisi == toplamSube)
                return Json(new
                {
                    success = true,
                    message = $"Tüm şubeler başarılı. ({toplamSube} şube gönderildi)",
                    subeSonuclari
                });
            else if (basariliSayisi > 0)
                return Json(new
                {
                    success = true,
                    message = $"{basariliSayisi} şube başarılı, {basarisizSayisi} şube başarısız.",
                    subeSonuclari
                });
            else
                return Json(new
                {
                    success = false,
                    message = $"Tüm şubeler başarısız.",
                    subeSonuclari
                });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Hata: {ex.Message}" });
        }
    }

    private static string? JobAlanDegerAl(LCM.Domain.Entities.Campaign k, string alanAdi) => alanAdi switch
    {
        "Baslik" => k.Baslik,
        "AltBaslik" => k.AltBaslik,
        "KampanyaNotu" => k.KampanyaNotu,
        "Subheadline" => k.Subheadline,
        "Headline" => k.Headline,
        "MinBasketText" => k.MinBasketText,
        "DetailText" => k.DetailText,
        "CampaignDescription" => k.CampaignDescription,
        "OriginalPrice" => k.OriginalPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture),
        "DiscountedPrice" => k.DiscountedPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture),
        "DateRange" => k.DateRange,
        "OriginCountry" => k.OriginCountry,
        "UnitPrice" => k.UnitPrice,
        "PriceUpdateDate" => k.PriceUpdateDate,
        "BuyQuantityText" => k.BuyQuantityText,
        "PayQuantityText" => k.PayQuantityText,
        "IsLocalProduction" => k.IsLocalProduction ? "1" : "0",
        "Grade" => k.Sablon?.EtiketTip?.EtiketTipi,
        _ => null
    };
}