using LCM.Infrastructure.Auth;
using LCM.Infrastructure.Data;
using LCM.Web.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LCM.Web.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;

    public AccountController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _db.Users
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.KullaniciAdi == model.KullaniciAdi && u.AktifMi);

        if (user == null || !PasswordHelper.Verify(model.Sifre, user.SifreHash))
        {
            ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.KullaniciAdi),
            new Claim(ClaimTypes.Role, user.Rol.RolAdi),
            new Claim("AdSoyad", $"{user.Ad} {user.Soyad}")
        };

        var identity = new ClaimsIdentity(claims, "LCMCookie");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("LCMCookie", principal, new AuthenticationProperties
        {
            IsPersistent = model.BeniHatirla,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("LCMCookie");
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}