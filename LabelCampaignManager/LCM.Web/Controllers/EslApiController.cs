using LCM.Infrastructure.Data;
using LCM.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LCM.Web.Controllers;

[Authorize(Roles = "Admin")]
public class EslApiController : Controller
{
    private readonly AppDbContext _db;
    private readonly EslApiService _eslApiService;
    private readonly IDataProtector _protektor;

    public EslApiController(AppDbContext db, EslApiService eslApiService, IDataProtectionProvider dp)
    {
        _db = db;
        _eslApiService = eslApiService;
        _protektor = dp.CreateProtector("EslApiSecretKey");
    }

    public async Task<IActionResult> Index()
    {
        var ayar = await _db.EslApiSettings.FirstOrDefaultAsync();
        return View(ayar);
    }

    [HttpPost]
    public async Task<IActionResult> Kaydet(
        string apiUrl, string accessKey, string secretKey,
        string customerStoreCode, string algorithm, string headerPrefix)
    {
        var ayar = await _db.EslApiSettings.FirstOrDefaultAsync();
        if (ayar == null)
        {
            ayar = new LCM.Domain.Entities.EslApiSetting();
            _db.EslApiSettings.Add(ayar);
        }

        ayar.ApiUrl = apiUrl;
        ayar.AccessKey = accessKey;
        ayar.CustomerStoreCode = customerStoreCode;
        ayar.Algorithm = algorithm;
        ayar.HeaderPrefix = headerPrefix;

        if (!string.IsNullOrWhiteSpace(secretKey))
            ayar.SecretKey = _protektor.Protect(secretKey);

        await _db.SaveChangesAsync();
        TempData["Basari"] = "ESL API ayarları kaydedildi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> TestGonder([FromBody] TestGonderModel model)
    {
        var ayar = await _db.EslApiSettings.FirstOrDefaultAsync();
        if (ayar == null)
            return Json(new { basarili = false, mesaj = "API ayarları bulunamadı." });

        var cozulmusAyar = new LCM.Domain.Entities.EslApiSetting
        {
            ApiUrl = ayar.ApiUrl,
            AccessKey = ayar.AccessKey,
            SecretKey = _protektor.Unprotect(ayar.SecretKey),
            CustomerStoreCode = ayar.CustomerStoreCode,
            Algorithm = ayar.Algorithm,
            HeaderPrefix = ayar.HeaderPrefix
        };

        var dateHeader = _eslApiService.UtcTarihUret();
        var sonuc = await _eslApiService.GonderAsync(cozulmusAyar, model.JsonBody, dateHeader);

        bool gercektenBasarili = sonuc.Basarili;
        if (sonuc.Basarili && !string.IsNullOrEmpty(sonuc.YanitIcerigi))
        {
            try
            {
                var yanit = System.Text.Json.JsonDocument.Parse(sonuc.YanitIcerigi);
                if (yanit.RootElement.TryGetProperty("resultCode", out var kod))
                    gercektenBasarili = kod.GetInt32() == 1001;
            }
            catch { }
        }

        return Json(new
        {
            basarili = gercektenBasarili,
            statusKod = sonuc.StatusKod,
            yanitIcerigi = sonuc.YanitIcerigi,
            gonderilenDate = sonuc.GonderilenDate,
            gonderilenAuth = sonuc.GonderilenAuth,
            gonderilenPayload = model.JsonBody
        });
    }
}

public class TestGonderModel
{
    public string JsonBody { get; set; } = string.Empty;
}