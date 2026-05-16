using LCM.Domain.Entities;
using LCM.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LCM.Infrastructure.Services;

public class EslGonderimSonuc
{
    public bool Basarili { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public int ToplamKampanya { get; set; }
    public int BasariliKampanya { get; set; }
    public int BasarisizKampanya { get; set; }
    public int HttpStatusKod { get; set; }
    public string? HataMesaji { get; set; }
    public string? GonderilenJson { get; set; }
}

public class EslGonderimService
{
    private readonly AppDbContext _db;
    private readonly EslApiService _eslApiService;
    private readonly IDataProtector _protektor;

    public EslGonderimService(AppDbContext db, EslApiService eslApiService, IDataProtectionProvider dp)
    {
        _db = db;
        _eslApiService = eslApiService;
        _protektor = dp.CreateProtector("EslApiSecretKey");
    }

   public async Task<(bool Basarili, string Yanit)> LedKomutuGonderAsync(
    string hazirJson, EslApiSetting ayar)
{
    try
    {
        var cozulmusAyar = new EslApiSetting
        {
            ApiUrl = ayar.ApiUrl,
            AccessKey = ayar.AccessKey,
            SecretKey = _protektor.Unprotect(ayar.SecretKey),
            CustomerStoreCode = ayar.CustomerStoreCode,
            Algorithm = ayar.Algorithm,
            HeaderPrefix = ayar.HeaderPrefix
        };

        var dateHeader = _eslApiService.UtcTarihUret();
        var apiSonuc = await _eslApiService.GonderAsync(cozulmusAyar, hazirJson, dateHeader);

        // 401 alırsa token yenileyip 1 kez daha dene
        if (apiSonuc.StatusKod == 401)
        {
            await Task.Delay(500);
            dateHeader = _eslApiService.UtcTarihUret();
            apiSonuc = await _eslApiService.GonderAsync(cozulmusAyar, hazirJson, dateHeader);
        }

        bool gercekBasari = apiSonuc.Basarili;
        if (apiSonuc.Basarili && !string.IsNullOrEmpty(apiSonuc.YanitIcerigi))
        {
            try
            {
                var yanit = System.Text.Json.JsonDocument.Parse(apiSonuc.YanitIcerigi);
                if (yanit.RootElement.TryGetProperty("resultCode", out var kod))
                    gercekBasari = kod.GetInt32() == 1001;
            }
            catch { }
        }

        return (gercekBasari, apiSonuc.YanitIcerigi ?? string.Empty);
    }
    catch (Exception ex)
    {
        return (false, ex.Message);
    }
}
    // Ana gönderim metodu — duruma göre kampanyaları gönderir
    public async Task<List<EslGonderimSonuc>> GonderAsync(
        bool aktifGonder,
        bool planlanmisGonder,
        int? tetikleyenKullaniciId,
        int? tetikleyenJobId,
        string tetikleyenAciklama)
    {
        var sonuclar = new List<EslGonderimSonuc>();

        // API ayarlarını al
        var ayar = await _db.EslApiSettings.FirstOrDefaultAsync();
        if (ayar == null)
        {
            sonuclar.Add(new EslGonderimSonuc
            {
                Basarili = false,
                HataMesaji = "ESL API ayarları bulunamadı."
            });
            return sonuclar;
        }

        var cozulmusAyar = new EslApiSetting
        {
            ApiUrl = ayar.ApiUrl,
            AccessKey = ayar.AccessKey,
            SecretKey = _protektor.Unprotect(ayar.SecretKey),
            CustomerStoreCode = ayar.CustomerStoreCode,
            Algorithm = ayar.Algorithm,
            HeaderPrefix = ayar.HeaderPrefix
        };

        // Gönderilecek durumları belirle
        var durumlar = new List<string>();
        if (aktifGonder) durumlar.Add("Aktif");
        if (planlanmisGonder) durumlar.Add("Planlanmış");

        if (!durumlar.Any())
        {
            sonuclar.Add(new EslGonderimSonuc
            {
                Basarili = false,
                HataMesaji = "Gönderilecek kampanya durumu seçilmedi."
            });
            return sonuclar;
        }

        // Kampanyaları çek (şubelerle birlikte)
        var kampanyalar = await _db.Campaigns
             .Include(c => c.CampaignStores).ThenInclude(cs => cs.Store)
             .Include(c => c.Sablon).ThenInclude(s => s.Alanlar)
             .Include(c => c.Sablon).ThenInclude(s => s.EtiketTip)
             .Where(c =>
                 (aktifGonder && c.Durum == "Aktif") ||
                 (planlanmisGonder && c.Durum == "Planlanmış"))
             .ToListAsync();

        if (!kampanyalar.Any())
        {
            sonuclar.Add(new EslGonderimSonuc
            {
                Basarili = true,
                HataMesaji = "Gönderilecek kampanya bulunamadı.",
                ToplamKampanya = 0
            });
            return sonuclar;
        }

        // FieldMapping'leri çek
        var mappings = await _db.FieldMappings
            .Include(m => m.EslField)
            .ToListAsync();

        // Şubeye göre grupla
        var subeler = kampanyalar
    .SelectMany(c => c.CampaignStores.Select(cs => cs.Store))
    .GroupBy(s => s.StoreCode)
    .Select(g => g.First())
    .ToList();

        foreach (var sube in subeler)
        {
            var subeKampanyalari = kampanyalar
                .Where(c => c.CampaignStores.Any(cs => cs.Store.StoreCode == sube.StoreCode))
                .ToList();

            // JSON items oluştur
            var items = new List<Dictionary<string, string>>();
            foreach (var kampanya in subeKampanyalari)
            {
                var item = new Dictionary<string, string>();
                var sablonMappings = mappings.Where(m => m.SablonId == kampanya.SablonId).ToList();

                foreach (var mapping in sablonMappings)
                {
                    var deger = AlanDegerAl(kampanya, mapping.BizimAlanAdi);
                    if (deger != null && mapping.EslField != null)
                        item[mapping.EslField.VariableName] = deger;
                }

                // SKU ve EAN her zaman gönderilir
                if (!item.ContainsKey("sku"))
                    item["sku"] = kampanya.Sku.ToString();
                if (!item.ContainsKey("ean"))
                    item["ean"] = kampanya.Barkod;
                if (!item.ContainsKey("grade") && kampanya.Sablon?.EtiketTip?.EtiketTipi != null)
                    item["grade"] = kampanya.Sablon.EtiketTip.EtiketTipi;

                if (item.Any())
                    items.Add(item);
            }

            // JSON serialize
            var payload = new
            {
                customerStoreCode = cozulmusAyar.CustomerStoreCode,
                storeCode = sube.StoreCode,
                batchNo = "",
                items
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = false });

            // Gönder — 401 alırsa token yenileyip tekrar dene
            var dateHeader = _eslApiService.UtcTarihUret();
            var apiSonuc = await _eslApiService.GonderAsync(cozulmusAyar, json, dateHeader);

            // 401 aldıysak yeni token ile 1 kez daha dene
            if (apiSonuc.StatusKod == 401)
            {
                await Task.Delay(500); // yarım saniye bekle
                dateHeader = _eslApiService.UtcTarihUret();
                apiSonuc = await _eslApiService.GonderAsync(cozulmusAyar, json, dateHeader);
            }

            bool gercekBasari = apiSonuc.Basarili;
            if (apiSonuc.Basarili && !string.IsNullOrEmpty(apiSonuc.YanitIcerigi))
            {
                try
                {
                    var yanit = System.Text.Json.JsonDocument.Parse(apiSonuc.YanitIcerigi);
                    if (yanit.RootElement.TryGetProperty("resultCode", out var kod))
                        gercekBasari = kod.GetInt32() == 1001;
                }
                catch { }
            }

            var sonuc = new EslGonderimSonuc
            {
                Basarili = gercekBasari,
                StoreCode = sube.StoreCode,
                ToplamKampanya = subeKampanyalari.Count,
                BasariliKampanya = gercekBasari ? subeKampanyalari.Count : 0,
                BasarisizKampanya = gercekBasari ? 0 : subeKampanyalari.Count,
                HttpStatusKod = apiSonuc.StatusKod,
                HataMesaji = gercekBasari ? null : apiSonuc.YanitIcerigi,
                GonderilenJson = json
            };
            sonuclar.Add(sonuc);

            // Log kaydet
            _db.EslGonderimLogs.Add(new EslGonderimLog
            {
                GonderimZamani = DateTime.Now,
                Tetikleyen = tetikleyenAciklama,
                EslJobId = tetikleyenJobId,
                KullaniciId = tetikleyenKullaniciId,
                StoreCode = sube.StoreCode,
                ToplamKampanya = sonuc.ToplamKampanya,
                BasariliKampanya = sonuc.BasariliKampanya,
                BasarisizKampanya = sonuc.BasarisizKampanya,
                HttpStatusKod = sonuc.HttpStatusKod,
                Basarili = sonuc.Basarili,
                HataMesaji = sonuc.HataMesaji,
                GonderilenJson = json
            });
        }

        await _db.SaveChangesAsync();
        return sonuclar;
    }

    // Tekli kampanya gönderimi
    public async Task<List<EslGonderimSonuc>> TekliGonderAsync(
        int kampanyaId,
        int tetikleyenKullaniciId)    {
        var sonuclar = new List<EslGonderimSonuc>();

        var ayar = await _db.EslApiSettings.FirstOrDefaultAsync();
        if (ayar == null)
        {
            sonuclar.Add(new EslGonderimSonuc { Basarili = false, HataMesaji = "ESL API ayarları bulunamadı." });
            return sonuclar;
        }

        var cozulmusAyar = new EslApiSetting
        {
            ApiUrl = ayar.ApiUrl,
            AccessKey = ayar.AccessKey,
            SecretKey = _protektor.Unprotect(ayar.SecretKey),
            CustomerStoreCode = ayar.CustomerStoreCode,
            Algorithm = ayar.Algorithm,
            HeaderPrefix = ayar.HeaderPrefix
        };

        var kampanya = await _db.Campaigns
            .Include(c => c.CampaignStores).ThenInclude(cs => cs.Store)
            .Include(c => c.Sablon).ThenInclude(s => s.Alanlar)
            .Include(c => c.Sablon).ThenInclude(s => s.EtiketTip)
            .FirstOrDefaultAsync(c => c.Id == kampanyaId);

        if (kampanya == null)
        {
            sonuclar.Add(new EslGonderimSonuc { Basarili = false, HataMesaji = "Kampanya bulunamadı." });
            return sonuclar;
        }

        var mappings = await _db.FieldMappings
            .Include(m => m.EslField)
            .Where(m => m.SablonId == kampanya.SablonId)
            .ToListAsync();

        var kullanici = await _db.Users.FindAsync(tetikleyenKullaniciId);
        var tetikleyenAciklama = $"Manuel: {kullanici?.KullaniciAdi ?? "bilinmiyor"}";

        foreach (var cs in kampanya.CampaignStores)
        {
            var item = new Dictionary<string, string>();
            foreach (var mapping in mappings)
            {
                var deger = AlanDegerAl(kampanya, mapping.BizimAlanAdi);
                if (deger != null && mapping.EslField != null)
                    item[mapping.EslField.VariableName] = deger;
            }
            if (!item.ContainsKey("sku")) item["sku"] = kampanya.Sku.ToString();
            if (!item.ContainsKey("ean")) item["ean"] = kampanya.Barkod;
            if (!item.ContainsKey("grade") && kampanya.Sablon?.EtiketTip?.EtiketTipi != null)
                item["grade"] = kampanya.Sablon.EtiketTip.EtiketTipi;
            var payload = new
            {
                customerStoreCode = cozulmusAyar.CustomerStoreCode,
                storeCode = cs.Store.StoreCode,
                batchNo = "",
                items = new[] { item }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = false });

            var dateHeader = _eslApiService.UtcTarihUret();
            var apiSonuc = await _eslApiService.GonderAsync(cozulmusAyar, json, dateHeader);

            // 401 aldıysak yeni token ile 1 kez daha dene
            if (apiSonuc.StatusKod == 401)
            {
                await Task.Delay(500);
                dateHeader = _eslApiService.UtcTarihUret();
                apiSonuc = await _eslApiService.GonderAsync(cozulmusAyar, json, dateHeader);
            }

            bool gercekBasari = apiSonuc.Basarili;
            if (apiSonuc.Basarili && !string.IsNullOrEmpty(apiSonuc.YanitIcerigi))
            {
                try
                {
                    var yanit = System.Text.Json.JsonDocument.Parse(apiSonuc.YanitIcerigi);
                    if (yanit.RootElement.TryGetProperty("resultCode", out var kod))
                        gercekBasari = kod.GetInt32() == 1001;
                }
                catch { }
            }

            var sonuc = new EslGonderimSonuc
            {
                Basarili = gercekBasari,
                StoreCode = cs.Store.StoreCode,
                ToplamKampanya = 1,
                BasariliKampanya = gercekBasari ? 1 : 0,
                BasarisizKampanya = gercekBasari ? 0 : 1,
                HttpStatusKod = apiSonuc.StatusKod,
                HataMesaji = gercekBasari ? null : apiSonuc.YanitIcerigi,
                GonderilenJson = json
            };
            sonuclar.Add(sonuc);

            _db.EslGonderimLogs.Add(new EslGonderimLog
            {
                GonderimZamani = DateTime.Now,
                Tetikleyen = tetikleyenAciklama,
                KullaniciId = tetikleyenKullaniciId,
                StoreCode = cs.Store.StoreCode,
                ToplamKampanya = 1,
                BasariliKampanya = sonuc.BasariliKampanya,
                BasarisizKampanya = sonuc.BasarisizKampanya,
                HttpStatusKod = sonuc.HttpStatusKod,
                Basarili = sonuc.Basarili,
                HataMesaji = sonuc.HataMesaji,
                GonderilenJson = json
            });
        }

        await _db.SaveChangesAsync();
        return sonuclar;
    }

    // Campaign entity'sinden alan değerini okur
    private static string? AlanDegerAl(Campaign k, string alanAdi) => alanAdi switch
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
        _ => null
    };
    public async Task<EslGonderimSonuc> EslEslestirAsync(
    string eslId,
    int kampanyaId,
    int storeId,
    int kullaniciId)
    {
        var ayar = await _db.EslApiSettings.FirstOrDefaultAsync();
        if (ayar == null)
            return new EslGonderimSonuc { Basarili = false, HataMesaji = "ESL API ayarları bulunamadı." };

        var cozulmusAyar = new EslApiSetting
        {
            ApiUrl = ayar.ApiUrl,
            AccessKey = ayar.AccessKey,
            SecretKey = _protektor.Unprotect(ayar.SecretKey),
            CustomerStoreCode = ayar.CustomerStoreCode,
            Algorithm = ayar.Algorithm,
            HeaderPrefix = ayar.HeaderPrefix
        };

        var kampanya = await _db.Campaigns
            .Include(c => c.Sablon).ThenInclude(s => s.EtiketTip)
            .FirstOrDefaultAsync(c => c.Id == kampanyaId);

        if (kampanya == null)
            return new EslGonderimSonuc { Basarili = false, HataMesaji = "Kampanya bulunamadı." };

        var store = await _db.Stores.FindAsync(storeId);
        if (store == null)
            return new EslGonderimSonuc { Basarili = false, HataMesaji = "Store bulunamadı." };

        var item = new Dictionary<string, string>
        {
            ["sku"] = kampanya.Sku.ToString(),
            ["ean"] = kampanya.Barkod,
            ["eslId"] = eslId,
            ["IIS_COMMAND"] = "BIND"
        }; 

        var payload = new
        {
            customerStoreCode = cozulmusAyar.CustomerStoreCode,
            storeCode = store.StoreCode,
            batchNo = "",
            items = new[] { item }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = false });

        var dateHeader = _eslApiService.UtcTarihUret();
        var apiSonuc = await _eslApiService.GonderAsync(cozulmusAyar, json, dateHeader);

        if (apiSonuc.StatusKod == 401)
        {
            await Task.Delay(500);
            dateHeader = _eslApiService.UtcTarihUret();
            apiSonuc = await _eslApiService.GonderAsync(cozulmusAyar, json, dateHeader);
        }

        bool gercekBasari = apiSonuc.Basarili;
        if (apiSonuc.Basarili && !string.IsNullOrEmpty(apiSonuc.YanitIcerigi))
        {
            try
            {
                var yanit = System.Text.Json.JsonDocument.Parse(apiSonuc.YanitIcerigi);
                if (yanit.RootElement.TryGetProperty("resultCode", out var kod))
                    gercekBasari = kod.GetInt32() == 1001;
            }
            catch { }
        }


        return new EslGonderimSonuc
        {
            Basarili = gercekBasari,
            StoreCode = store.StoreCode,
            ToplamKampanya = 1,
            BasariliKampanya = gercekBasari ? 1 : 0,
            BasarisizKampanya = gercekBasari ? 0 : 1,
            HttpStatusKod = apiSonuc.StatusKod,
            HataMesaji = gercekBasari ? null : apiSonuc.YanitIcerigi,
            GonderilenJson = json
        };
    }
}