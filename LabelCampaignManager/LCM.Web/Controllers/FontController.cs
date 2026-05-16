using LCM.Domain.Entities;
using LCM.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LCM.Web.Controllers;

public class FontController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public FontController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ─── Font Yönetimi Sayfası ───────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var fontlar = await _db.Fonts.OrderBy(f => f.FontAdi).ThenBy(f => f.FontWeight).ToListAsync();
        return View(fontlar);
    }

    // ─── Font Yükle ──────────────────────────────────────────────────────────
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Yukle(IFormFile dosya, string fontAdi, int fontWeight, bool italic)
    {
        if (dosya == null || dosya.Length == 0)
            return Json(new { ok = false, mesaj = "Dosya seçilmedi." });

        var uzanti = Path.GetExtension(dosya.FileName).ToLower();
        if (!new[] { ".ttf", ".otf", ".woff", ".woff2" }.Contains(uzanti))
            return Json(new { ok = false, mesaj = "Geçersiz dosya türü." });

        if (string.IsNullOrWhiteSpace(fontAdi))
            return Json(new { ok = false, mesaj = "Font adı boş olamaz." });

        // Dosya adını temizle ve kaydet
        var dosyaAdi = dosya.FileName.Replace(" ", "-");
        var kayitYolu = Path.Combine(_env.WebRootPath, "fonts", dosyaAdi);
        using (var stream = new FileStream(kayitYolu, FileMode.Create))
            await dosya.CopyToAsync(stream);

        // DB'ye kaydet
        _db.Fonts.Add(new Font
        {
            FontAdi = fontAdi.Trim(),
            DosyaAdi = dosyaAdi,
            FontWeight = fontWeight,
            Italic = italic,
            EklenmeTarihi = DateTime.Now
        });
        await _db.SaveChangesAsync();

        return Json(new { ok = true });
    }

    // ─── Font Sil ────────────────────────────────────────────────────────────
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var font = await _db.Fonts.FindAsync(id);
        if (font == null) return Json(new { ok = false });

        // Dosyayı sil (Poppins gibi manuel eklenenler silinmesin diye kontrol)
        var dosyaYolu = Path.Combine(_env.WebRootPath, "fonts", font.DosyaAdi);
        if (System.IO.File.Exists(dosyaYolu))
            System.IO.File.Delete(dosyaYolu);

        _db.Fonts.Remove(font);
        await _db.SaveChangesAsync();

        return Json(new { ok = true });
    }

    // ─── Dinamik CSS Endpoint'i ───────────────────────────────────────────────
    [ResponseCache(Duration = 0)]
    public async Task<IActionResult> FontCss()
    {
        var fontlar = await _db.Fonts.OrderBy(f => f.FontAdi).ThenBy(f => f.FontWeight).ToListAsync();

        var sb = new System.Text.StringBuilder();
        foreach (var f in fontlar)
        {
            sb.AppendLine($"@font-face {{");
            sb.AppendLine($"    font-family: '{f.FontAdi}';");
            sb.AppendLine($"    src: url('/fonts/{f.DosyaAdi}');");
            sb.AppendLine($"    font-weight: {f.FontWeight};");
            sb.AppendLine($"    font-style: {(f.Italic ? "italic" : "normal")};");
            sb.AppendLine($"}}");
        }

        return Content(sb.ToString(), "text/css");
    }

    // ─── Design.cshtml için font listesi (JSON) ──────────────────────────────
    public async Task<IActionResult> FontListesi()
    {
        var fontlar = await _db.Fonts
            .Select(f => f.FontAdi)
            .Distinct()
            .OrderBy(f => f)
            .ToListAsync();

        return Json(fontlar);
    }
}