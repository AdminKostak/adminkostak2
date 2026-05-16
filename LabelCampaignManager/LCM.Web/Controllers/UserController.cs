using LCM.Domain.Entities;
using LCM.Infrastructure.Auth;
using LCM.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LCM.Web.ViewModels;
using LCM.Infrastructure.Helpers;


namespace LCM.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly AppDbContext _db;

    public UserController(AppDbContext db)
    {
        _db = db;
    }

    // Listeleme
    public async Task<IActionResult> Index()
    {
        var liste = await _db.Users
            .Include(u => u.Rol)
            .OrderByDescending(u => u.OlusturmaTarihi)
            .ToListAsync();
        return View(liste);
    }

    // Yeni Ekle - Form
    public async Task<IActionResult> Create()
    {
        await RolleriDoldur();
        await StoreleriDoldur();
        await CaptchaHazirla("KullaniciEkleCaptcha");
        return View();
    }

    // Yeni Ekle - Kaydet
    [HttpPost]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        // Captcha kontrolü
        var captchaAktif = await _db.SystemSettings
            .FirstOrDefaultAsync(s => s.AyarAdi == "KullaniciEkleCaptcha");

        if (captchaAktif?.AktifMi == true)
        {
            var dogruCevap = HttpContext.Session.GetInt32("CaptchaCevap");
            if (model.CaptchaCevap != dogruCevap)
            {
                ModelState.AddModelError("CaptchaCevap", "Captcha cevabı hatalı.");
                await RolleriDoldur();
                await CaptchaHazirla("KullaniciEkleCaptcha");
                return View(model);
            }
        }
        if (!ModelState.IsValid)
        {
            await RolleriDoldur();
            return View(model);
        }

        var varMi = await _db.Users.AnyAsync(u =>
            u.KullaniciAdi == model.KullaniciAdi || u.Email == model.Email);
        if (varMi)
        {
            ModelState.AddModelError("", "Bu kullanıcı adı veya e-posta zaten kullanılıyor.");
            await RolleriDoldur();
            return View(model);
        }

        var user = new User
        {
            Ad = model.Ad,
            Soyad = model.Soyad,
            KullaniciAdi = model.KullaniciAdi,
            Email = model.Email,
            SifreHash = PasswordHelper.Hash(model.Sifre),
            RolId = model.RolId,
            AktifMi = true,
            OlusturmaTarihi = DateTime.Now
        };

        user.CokluEslestirmeIzni = model.CokluEslestirmeIzni;
        user.HizliEslestirmeIzni = model.HizliEslestirmeIzni;

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (model.YetkiliStoreIdler != null && model.YetkiliStoreIdler.Any())
        {
            foreach (var storeId in model.YetkiliStoreIdler)
            {
                _db.UserStores.Add(new UserStore { UserId = user.Id, StoreId = storeId });
            }
            await _db.SaveChangesAsync();
        }

        TempData["Basari"] = "Kullanıcı başarıyla eklendi.";
        return RedirectToAction("Index");
    }

    // Düzenle - Form
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        await RolleriDoldur();
        await StoreleriDoldur();
        var model = new UserEditViewModel
        {
            Id = user.Id,
            Ad = user.Ad,
            Soyad = user.Soyad,
            KullaniciAdi = user.KullaniciAdi,
            Email = user.Email,
            RolId = user.RolId,
            AktifMi = user.AktifMi,
            CokluEslestirmeIzni = user.CokluEslestirmeIzni,
            HizliEslestirmeIzni = user.HizliEslestirmeIzni,
            YetkiliStoreIdler = await _db.UserStores
    .Where(us => us.UserId == id)
    .Select(us => us.StoreId)
    .ToListAsync()
        };
        return View(model);
    }

    // Düzenle - Kaydet
    [HttpPost]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        var captchaAktif = await _db.SystemSettings
    .FirstOrDefaultAsync(s => s.AyarAdi == "KullaniciEkleCaptcha");

        if (captchaAktif?.AktifMi == true)
        {
            var dogruCevap = HttpContext.Session.GetInt32("CaptchaCevap");
            if (model.CaptchaCevap != dogruCevap)
            {
                ModelState.AddModelError("CaptchaCevap", "Captcha cevabı hatalı.");
                await RolleriDoldur();
                await CaptchaHazirla("KullaniciEkleCaptcha");
                return View(model);
            }
        }
        if (!ModelState.IsValid)
        {
            await RolleriDoldur();
            return View(model);
        }

        var user = await _db.Users.FindAsync(model.Id);
        if (user == null) return NotFound();

        user.Ad = model.Ad;
        user.Soyad = model.Soyad;
        user.KullaniciAdi = model.KullaniciAdi;
        user.Email = model.Email;
        user.RolId = model.RolId;
        user.AktifMi = model.AktifMi;
        user.CokluEslestirmeIzni = model.CokluEslestirmeIzni;
        user.HizliEslestirmeIzni = model.HizliEslestirmeIzni;

        var eskiStoreler = _db.UserStores.Where(us => us.UserId == user.Id);
        _db.UserStores.RemoveRange(eskiStoreler);

        if (model.YetkiliStoreIdler != null && model.YetkiliStoreIdler.Any())
        {
            foreach (var storeId in model.YetkiliStoreIdler)
            {
                _db.UserStores.Add(new UserStore { UserId = user.Id, StoreId = storeId });
            }
        }

        if (!string.IsNullOrEmpty(model.YeniSifre))
            user.SifreHash = PasswordHelper.Hash(model.YeniSifre);

        await _db.SaveChangesAsync();
        TempData["Basari"] = "Kullanıcı güncellendi.";
        return RedirectToAction("Index");
    }

    // Sil — kampanya/log geçmişi korunur, kullanıcı pasife alınır
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        // Kendini silemesin
        var mevcutId = int.Parse(User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        if (user.Id == mevcutId)
        {
            TempData["Hata"] = "Kendi hesabınızı silemezsiniz.";
            return RedirectToAction("Index");
        }

        user.AktifMi = false;
        await _db.SaveChangesAsync();

        TempData["Basari"] = $"{user.Ad} {user.Soyad} pasife alındı.";
        return RedirectToAction("Index");
    }

    private async Task RolleriDoldur()
    {
        var roller = await _db.Roles.OrderBy(r => r.RolAdi).ToListAsync();
        ViewBag.Roller = new SelectList(roller, "Id", "RolAdi");
    }
    private async Task StoreleriDoldur()
{
    var storeler = await _db.Stores
        .Where(s => s.AktifMi)
        .OrderBy(s => s.StoreName)
        .ToListAsync();
    ViewBag.Storeler = storeler;
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
}