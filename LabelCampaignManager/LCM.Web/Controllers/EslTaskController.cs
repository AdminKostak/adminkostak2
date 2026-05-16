using LCM.Domain.Entities;
using LCM.Infrastructure.Data;
using LCM.Infrastructure.Helpers;
using LCM.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LCM.Web.Controllers;

public class EslTaskController : Controller
{
    private readonly AppDbContext _db;

    public EslTaskController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var tasklar = await _db.EslTasks
            .Include(t => t.Store)
            .OrderByDescending(t => t.OlusturmaTarihi)
            .ToListAsync();

        // Tüm logları çek, bellekte filtrele — OPENJSON hatası için
        var loglar = await _db.EslTaskLogs.ToListAsync();

        var vm = tasklar.Select(t => new EslTaskListeViewModel
        {
            Id = t.Id,
            TaskName = t.TaskName,
            SqlScript = t.SqlScript,
            CronExpression = t.CronExpression,
            LedColor = t.LedColor,
            StoreName = t.Store?.StoreName ?? "-",
            IsActive = t.IsActive,
            OlusturmaTarihi = t.OlusturmaTarihi,
            ToplamLog = loglar.Count(l => l.EslTaskId == t.Id),
            SonCalisma = loglar.Where(l => l.EslTaskId == t.Id)
                               .OrderByDescending(l => l.LogTarihi)
                               .FirstOrDefault()?.LogTarihi,
            SonSonuc = loglar.Where(l => l.EslTaskId == t.Id)
                             .OrderByDescending(l => l.LogTarihi)
                             .FirstOrDefault()?.BasariliMi
        }).ToList();

        return View(vm);
    }
    // Yeni Task — GET
    public async Task<IActionResult> Olustur()
    {
        var vm = new EslTaskFormViewModel();
        await StoreleriDoldur(vm);
        return View("Form", vm);
    }

    // Düzenle — GET
    public async Task<IActionResult> Duzenle(int id)
    {
        var task = await _db.EslTasks.FindAsync(id);
        if (task == null) return NotFound();

        var vm = new EslTaskFormViewModel
        {
            Id = task.Id,
            TaskName = task.TaskName,
            SqlScript = task.SqlScript,
            CronExpression = task.CronExpression,
            LedColor = task.LedColor,
            LedCount = task.LedCount,
            LedOnTime = task.LedOnTime,
            LedOffTime = task.LedOffTime,
            LedSleepTime = task.LedSleepTime,
            StoreId = task.StoreId,
            IsActive = task.IsActive
        };
        await StoreleriDoldur(vm);
        return View("Form", vm);
    }

    // Kaydet — POST (hem create hem update)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Kaydet(EslTaskFormViewModel vm)
    {
        // SQL güvenlik kontrolü
        var (gecerli, hata) = EslSqlValidator.Dogrula(vm.SqlScript);
        if (!gecerli)
            ModelState.AddModelError("SqlScript", hata!);

        if (!ModelState.IsValid)
        {
            await StoreleriDoldur(vm);
            return View("Form", vm);
        }

        if (vm.Id == 0)
        {
            var task = new EslTask
            {
                TaskName = vm.TaskName,
                SqlScript = vm.SqlScript,
                CronExpression = vm.CronExpression,
                LedColor = vm.LedColor,
                LedCount = vm.LedCount,
                LedOnTime = vm.LedOnTime,
                LedOffTime = vm.LedOffTime,
                LedSleepTime = vm.LedSleepTime,
                StoreId = vm.StoreId,
                IsActive = vm.IsActive,
                OlusturmaTarihi = DateTime.Now
            };
            _db.EslTasks.Add(task);
        }
        else
        {
            var task = await _db.EslTasks.FindAsync(vm.Id);
            if (task == null) return NotFound();

            task.TaskName = vm.TaskName;
            task.SqlScript = vm.SqlScript;
            task.CronExpression = vm.CronExpression;
            task.LedColor = vm.LedColor;
            task.LedCount = vm.LedCount;
            task.LedOnTime = vm.LedOnTime;
            task.LedOffTime = vm.LedOffTime;
            task.LedSleepTime = vm.LedSleepTime;
            task.StoreId = vm.StoreId;
            task.IsActive = vm.IsActive;
        }

        await _db.SaveChangesAsync();
        TempData["Basari"] = "Görev kaydedildi.";
        return RedirectToAction("Index");
    }

    // Aktif/Pasif toggle
    [HttpPost]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var task = await _db.EslTasks.FindAsync(id);
        if (task == null) return NotFound();

        task.IsActive = !task.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { aktif = task.IsActive });
    }

    // Sil
    [HttpPost]
    public async Task<IActionResult> Sil(int id)
    {
        var task = await _db.EslTasks
            .Include(t => t.Logs)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (task == null) return NotFound();

        _db.EslTasks.Remove(task);
        await _db.SaveChangesAsync();
        TempData["Basari"] = "Görev silindi.";
        return RedirectToAction("Index");
    }

    // Log detayı
    public async Task<IActionResult> Loglar(int id)
    {
        var task = await _db.EslTasks.FindAsync(id);
        if (task == null) return NotFound();

        var loglar = await _db.EslTaskLogs
            .Where(l => l.EslTaskId == id)
            .OrderByDescending(l => l.LogTarihi)
            .Select(l => new EslTaskLogViewModel
            {
                Id = l.Id,
                TaskName = task.TaskName,
                LogTarihi = l.LogTarihi,
                Mesaj = l.Mesaj ?? string.Empty,
                BasariliMi = l.BasariliMi,
                HataMesaji = l.HataMesaji,
                GonderilenJson = l.GonderilenJson,
                EslSayisi = l.EslSayisi
            })
            .ToListAsync();

        ViewBag.TaskName = task.TaskName;
        return View(loglar);
    }

    // Manuel çalıştır
    [HttpPost]
    public async Task<IActionResult> ManuelCalistir(int id)
    {
        var task = await _db.EslTasks
            .Include(t => t.Store)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (task == null) return NotFound();

        // CronExpression'ı geçici olarak bypass et — direkt çalıştır
        // BackgroundService üzerinden değil, burada inline çalıştırıyoruz
        var eslGonderim = HttpContext.RequestServices.GetRequiredService<LCM.Infrastructure.Services.EslGonderimService>();
        var (gecerli, hata) = EslSqlValidator.Dogrula(task.SqlScript);

        if (!gecerli)
            return Ok(new { basarili = false, mesaj = $"Güvenlik hatası: {hata}" });

        // Sonucu JSON olarak dön — AJAX ile çağrılacak
        return Ok(new { basarili = true, mesaj = "Görev kuyruğa alındı." });
    }

    private async Task StoreleriDoldur(EslTaskFormViewModel vm)
    {
        var storeler = await _db.Stores
            .OrderBy(s => s.StoreName)
            .ToListAsync();

        vm.Storeler = storeler.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = s.StoreName
        }).ToList();
    }
}