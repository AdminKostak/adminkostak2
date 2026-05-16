using LCM.Domain.Entities;
using LCM.Infrastructure.Data;
using LCM.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Cronos;

namespace LCM.Infrastructure.Services;

public class EslLedJobService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EslLedJobService> _logger;
    private const int KontrolAraligi = 60;

    public EslLedJobService(IServiceScopeFactory scopeFactory, ILogger<EslLedJobService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ESL LED Job Service başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await JoblarıKontrolEt(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ESL LED Job döngüsünde hata.");
            }

            await Task.Delay(TimeSpan.FromSeconds(KontrolAraligi), stoppingToken);
        }
    }

    private async Task JoblarıKontrolEt(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var aktifTasklar = await db.EslTasks
            .Where(t => t.IsActive)
            .Include(t => t.Store)
            .ToListAsync(ct);

        foreach (var task in aktifTasklar)
        {
            if (CronZamaniGeldiMi(task.CronExpression))
            {
                // Her task için ayrı scope — uzun süren işlemlerde çakışma olmaz
                using var taskScope = _scopeFactory.CreateScope();
                await TaskCalistir(task, taskScope, ct);
            }
        }
    }

    private async Task TaskCalistir(EslTask task, IServiceScope scope, CancellationToken ct)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eslGonderim = scope.ServiceProvider.GetRequiredService<EslGonderimService>();

        var log = new EslTaskLog
        {
            EslTaskId = task.Id,
            LogTarihi = DateTime.Now
        };

        try
        {
            // 1. SQL güvenlik kontrolü
            var (gecerli, hata) = EslSqlValidator.Dogrula(task.SqlScript);
            if (!gecerli)
            {
                log.BasariliMi = false;
                log.HataMesaji = $"Güvenlik hatası: {hata}";
                db.EslTaskLogs.Add(log);
                await db.SaveChangesAsync(ct);
                return;
            }

            // 2. SQL çalıştır — EslId + Sku listesi al
            var eslIdler = await EslIdleriniCek(task.SqlScript, db, ct);

            if (eslIdler.Count == 0)
            {
                log.BasariliMi = true;
                log.Mesaj = "Sorgu çalıştı fakat ESL ID bulunamadı.";
                log.EslSayisi = 0;
                db.EslTaskLogs.Add(log);
                await db.SaveChangesAsync(ct);
                return;
            }

            // 3. API ayarını al (store filtresi yok — tek kayıt)
            var apiAyar = await db.EslApiSettings.FirstOrDefaultAsync(ct);
            if (apiAyar == null)
            {
                log.BasariliMi = false;
                log.HataMesaji = "EslApiSettings bulunamadı.";
                db.EslTaskLogs.Add(log);
                await db.SaveChangesAsync(ct);
                return;
            }

            // 4. Batch hazırla ve gönder (max 1000'erli gruplar)
            var gonderilenJsonler = new List<string>();
            var batches = eslIdler.Chunk(1000);

            foreach (var batch in batches)
            {
                var payload = new
                {
                    customerStoreCode = apiAyar.CustomerStoreCode,
                    storeCode = task.Store.StoreCode,
                    batchNo = $"JOB_{task.Id}_{DateTime.Now:yyyyMMddHHmmss}",
                    items = batch.Select(eslId => new
                    {
                        customerStoreCode = apiAyar.CustomerStoreCode,
                        storeCode = task.Store.StoreCode,
                        sku = eslId.Sku,
                        eslId = eslId.EslId,
                        IIS_COMMAND = "CUTPAGE_FLASHLIGHTS",
                        IIS_PARAM = new
                        {
                            led_count = task.LedCount,
                            led_color = new[] { task.LedColor },
                            led_on_time = task.LedOnTime,
                            led_off_time = task.LedOffTime,
                            led_sleep_time = task.LedSleepTime
                        }
                    }).ToArray()
                };

                var json = JsonSerializer.Serialize(payload);
                gonderilenJsonler.Add(json);

                var (basarili, yanit) = await eslGonderim.LedKomutuGonderAsync(json, apiAyar);
                if (!basarili)
                    throw new Exception($"API hatası: {yanit}");
            }

            log.BasariliMi = true;
            log.EslSayisi = eslIdler.Count;
            log.Mesaj = $"{eslIdler.Count} etikete LED komutu gönderildi.";
            log.GonderilenJson = string.Join("\n---\n", gonderilenJsonler);
        }
        catch (Exception ex)
        {
            log.BasariliMi = false;
            log.HataMesaji = ex.Message;
        }

        db.EslTaskLogs.Add(log);
        await db.SaveChangesAsync(ct);
    }

    private static async Task<List<(string EslId, string Sku)>> EslIdleriniCek(
        string sqlScript, AppDbContext db, CancellationToken ct)
    {
        var sonuclar = new List<(string EslId, string Sku)>();

        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sqlScript;
        cmd.CommandTimeout = 30;

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var eslId = reader["EslId"]?.ToString() ?? string.Empty;
            var sku = reader["Sku"]?.ToString() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(eslId))
                sonuclar.Add((eslId, sku));
        }

        return sonuclar;
    }

    private static bool CronZamaniGeldiMi(string cron)
    {
        try
        {
            var expression = CronExpression.Parse(cron, CronFormat.IncludeSeconds);
            var simdi = DateTime.UtcNow;
            var sonrakiZaman = expression.GetNextOccurrence(simdi.AddSeconds(-61), TimeZoneInfo.Utc);
            return sonrakiZaman.HasValue && sonrakiZaman.Value <= simdi;
        }
        catch
        {
            return false;
        }
    }
}