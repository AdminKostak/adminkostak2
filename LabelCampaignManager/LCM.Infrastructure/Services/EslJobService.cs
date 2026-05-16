using LCM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace LCM.Infrastructure.Services;

public class EslJobService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EslJobService> _logger;

    public EslJobService(IServiceScopeFactory scopeFactory, ILogger<EslJobService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await KontrolEtAsync();
            // Her dakika kontrol et
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task KontrolEtAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gonderimService = scope.ServiceProvider.GetRequiredService<EslGonderimService>();
            var simdi = TimeOnly.FromDateTime(DateTime.Now);
        var bugun = DateTime.Today;

        // Aktif job'ları al
        var jobs = await db.EslJobs
            .Where(j => j.AktifMi)
            .ToListAsync();

        foreach (var job in jobs)
        {
            var jobZamani = job.CalismaZamani;
            var simdiDakika = simdi.Hour * 60 + simdi.Minute;
            var jobDakika = jobZamani.Hour * 60 + jobZamani.Minute;
            var fark = Math.Abs(simdiDakika - jobDakika);

            Console.WriteLine($"[ESL JOB] {simdi:HH:mm} | Job: {job.JobAdi} | Hedef: {jobZamani:HH:mm} | Fark: {fark} dk");

            // 2 dakika tolerans
            if (fark > 2) continue;

            // Bugün bu job OTOMATIK olarak, ayarlanan saatten sonra zaten çalıştı mı?
            // Saat değiştirilmişse eski loglar geçersiz — yeni saatten sonraki loglara bak
            var jobSaatiDateTime = bugun.AddHours(job.CalismaZamani.Hour)
                                        .AddMinutes(job.CalismaZamani.Minute);

            var bugunOtomatikCalisti = await db.EslGonderimLogs
                .AnyAsync(l =>
                    l.EslJobId == job.Id &&
                    l.GonderimZamani >= jobSaatiDateTime.AddMinutes(-3) &&
                    l.GonderimZamani.Date == bugun &&
                    l.Tetikleyen.StartsWith("JOB:"));

            if (bugunOtomatikCalisti)
            {
                Console.WriteLine($"[ESL JOB] Bugün ayarlanan saatte zaten çalıştı, atlanıyor: {job.JobAdi}");
                continue;
            }

            _logger.LogInformation("ESL Job çalıştırılıyor: {JobAdi}", job.JobAdi);

            try
            {
                await gonderimService.GonderAsync(
                    aktifGonder: job.AktifGonder,
                    planlanmisGonder: job.PlanlanmisGonder,
                    tetikleyenKullaniciId: null,
                    tetikleyenJobId: job.Id,
                    tetikleyenAciklama: $"JOB: {job.JobAdi}"
                );

                job.SonCalisma = DateTime.Now;
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ESL Job hatası: {JobAdi}", job.JobAdi);
            }
        }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ESL Job kontrol hatası — servis çalışmaya devam edecek.");
        }
    }
}