    using LCM.Domain.Entities;
    using LCM.Infrastructure.Data;
    using LCM.Infrastructure.Helpers;
    using LCM.Infrastructure.Services;
    using LCM.Web.ViewModels;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;
    using System.Security.Claims;

    namespace LCM.Web.Controllers;

    [Authorize]
    public class CampaignController : Controller
    {
    private readonly AppDbContext _db;
    private readonly KampanyaGuncelleyici _guncelleyici;
    private readonly EslGonderimService _eslGonderimService;
    private readonly CampaignStatusService _statusService;
    private readonly MailService _mailService;

    public CampaignController(AppDbContext db, KampanyaGuncelleyici guncelleyici, EslGonderimService eslGonderimService, CampaignStatusService statusService, MailService mailService)
    {
        _db = db;
        _guncelleyici = guncelleyici;
        _eslGonderimService = eslGonderimService;
        _statusService = statusService;
        _mailService = mailService;
    }

    // Listeleme
    // Listeleme — filtre parametreleri query string'den gelir
    public async Task<IActionResult> Index(
        string? durum,           // Durum filtresi (Aktif, Pasif, Taslak vb.)
        int? ekleyenId,          // Kullanıcı filtresi
        DateTime? baslangicTarihi, // Kampanya başlangıç tarihi filtresi
        string? aramaMetni)      // SKU veya başlık araması
    {
        // Otomatik durum güncelleme (sistem ayarına göre)
        var otomatikGuncelleme = await _db.SystemSettings
            .FirstOrDefaultAsync(s => s.AyarAdi == "DashboardOtomatikGuncelleme");
        if (otomatikGuncelleme?.AktifMi == true)
            await _guncelleyici.GuncelleAsync();

        // Onay aksiyonları — sadece bunları "son onay aksiyonu" olarak sayıyoruz
        var onayAksiyonlari = new[] { "Onaylandı", "Reddedildi", "Revize İstendi", "Onaya Gönderildi" };

        // Temel sorgu
        var sorgu = _db.Campaigns
            .Include(c => c.Sablon)
            .Include(c => c.EkleyenKullanici)
            .Include(c => c.CampaignStores).ThenInclude(cs => cs.Store)
            .AsQueryable();

        // ── Filtreler ──────────────────────────────────────────
        if (!string.IsNullOrEmpty(durum))
            sorgu = sorgu.Where(c => c.Durum == durum);

        if (ekleyenId.HasValue)
            sorgu = sorgu.Where(c => c.EkleyenKullaniciId == ekleyenId.Value);

        if (baslangicTarihi.HasValue)
            sorgu = sorgu.Where(c => c.BaslangicTarihi.Date == baslangicTarihi.Value.Date);

        if (!string.IsNullOrEmpty(aramaMetni))
            sorgu = sorgu.Where(c =>
                c.Sku.ToString().Contains(aramaMetni) ||
                (c.Baslik != null && c.Baslik.Contains(aramaMetni)));

        // ── Veri çekimi + son onay aksiyonu join ───────────────
        var kampanyalar = await sorgu
            .OrderByDescending(c => c.OlusturmaTarihi)
            .ToListAsync();

        // Son onay loglarını çek — GroupBy/First DB'ye gönderilmiyor, bellekte yapılıyor
        // (EF Core eski SQL Server uyumunda OPENJSON+ROW_NUMBER hatası verir)
        var kampanyaIdleri = kampanyalar.Select(c => c.Id).ToList();

        // SQL Server eski versiyon uyumu: OPENJSON hatası verdiği için
        // Contains(liste) DB'ye gönderilmiyor, tüm loglar çekilip bellekte filtreleniyor.
        // Kampanya sayısı çok büyümeden bu yaklaşım yeterince hızlıdır.
        var tumOnayLoglari = await _db.CampaignLogs
    .OrderByDescending(l => l.Tarih)
    .ToListAsync();

        var kampanyaIdSet = new HashSet<int>(kampanyaIdleri);
        tumOnayLoglari = tumOnayLoglari
            .Where(l => kampanyaIdSet.Contains(l.CampaignId)
                     && onayAksiyonlari.Contains(l.Aksiyon))
            .ToList();

        // Bellekte grupla: her kampanya için en son logu al
        var logSozlugu = tumOnayLoglari
            .GroupBy(l => l.CampaignId)
            .ToDictionary(
                g => g.Key,
                g => g.First()  // zaten Tarih DESC sıralı geldi, ilki en son
            );
        // ViewModel listesi oluştur
        var liste = kampanyalar.Select(c =>
        {
            logSozlugu.TryGetValue(c.Id, out var sonLog);
            return new CampaignListViewModel
            {
                Id = c.Id,
                Sku = c.Sku,
                Barkod = c.Barkod,
                SablonId = c.SablonId,                      // hover önizleme için
                SablonAdi = c.Sablon?.SablonAdi ?? "",
                Baslik = c.Baslik,
                OriginalPrice = c.OriginalPrice,
                DiscountedPrice = c.DiscountedPrice,
                BaslangicTarihi = c.BaslangicTarihi,
                BitisTarihi = c.BitisTarihi,
                Durum = c.Durum,
                EkleyenAdi = (c.EkleyenKullanici?.Ad + " " + c.EkleyenKullanici?.Soyad).Trim(),
                EkleyenKullaniciId = c.EkleyenKullaniciId,
                OlusturmaTarihi = c.OlusturmaTarihi,
                StoreDisplay = string.Join(", ", c.CampaignStores
                    .Select(cs => cs.Store.StoreCode + " - " + cs.Store.StoreName)),
                SonOnayAksiyonu = sonLog?.Aksiyon,
                SonOnayYorumu = sonLog?.Yorum
            };
        }).ToList();

        // ── ViewBag: Filtre için kullanıcı listesi ─────────────
        ViewBag.Kullanicilar = await _db.Users
            .Where(u => u.AktifMi)
            .OrderBy(u => u.Ad)
            .Select(u => new { u.Id, AdSoyad = u.Ad + " " + u.Soyad })
            .ToListAsync();

        // Aktif filtre değerlerini view'a gönder (filtre panelini doldurmak için)
        ViewBag.FiltreDurum = durum;
        ViewBag.FiltreEkleyenId = ekleyenId;
        ViewBag.FiltreBaslangicTarihi = baslangicTarihi?.ToString("yyyy-MM-dd");
        ViewBag.FiltreAramaMetni = aramaMetni;

        return View(liste);
    }
    // Yeni Ekle - Form
    [Authorize(Roles = "Admin,KampanyaYonetici,VeriGirisi")]
        public async Task<IActionResult> Create()
        {
            await SablonlariDoldur();
            await StorelariDoldur();
            await CaptchaHazirla("KampanyaEkleCaptcha");
            return View(new CampaignCreateViewModel());
        }

        // Yeni Ekle - Kaydet
        [HttpPost]
        [Authorize(Roles = "Admin,KampanyaYonetici,VeriGirisi")]
        public async Task<IActionResult> Create(CampaignCreateViewModel model)
        {
            // Captcha kontrolü
            var captchaAktif = await _db.SystemSettings
                .FirstOrDefaultAsync(s => s.AyarAdi == "KampanyaEkleCaptcha");

            if (captchaAktif?.AktifMi == true)
            {
                var dogruCevap = HttpContext.Session.GetInt32("CaptchaCevap");
                if (model.CaptchaCevap != dogruCevap)
                {
                    ModelState.AddModelError("CaptchaCevap", "Captcha cevabı hatalı.");
                    await SablonlariDoldur();
                    await StorelariDoldur();   // ← ekle
                    await CaptchaHazirla("KampanyaEkleCaptcha");
                    return View(model);
                }
            }

            if (!ModelState.IsValid)
            {

                await SablonlariDoldur();
                await StorelariDoldur();
                await CaptchaHazirla("KampanyaEkleCaptcha");
                return View(model);
            }

            var sku = await SkuUret();
            var barkod = "29" + sku.ToString();

        var onayAyar = await _db.SystemSettings
    .FirstOrDefaultAsync(s => s.AyarAdi == "KampanyaOnayaGonderilsinMi");
        var onayAktif = onayAyar?.AktifMi == true;
        var kullanicıRol = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        var kullaniciId = int.Parse(User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

            var kampanya = new Campaign
            {
                Sku = sku,
                Barkod = barkod,
                SablonId = model.SablonId,
                Baslik = model.Baslik,
                AltBaslik = model.AltBaslik,
                KampanyaNotu = model.KampanyaNotu,
                Subheadline = model.Subheadline,
                OriginalPrice = model.OriginalPrice,
                DiscountedPrice = model.DiscountedPrice,
                Headline = model.Headline,
                MinBasketText = model.MinBasketText,
                DetailText = model.DetailText,
                CampaignDescription = model.CampaignDescription,
                IsLocalProduction = model.IsLocalProduction,
                OriginCountry = model.OriginCountry,
                UnitPrice = model.UnitPrice,
                PriceUpdateDate = model.PriceUpdateDate,
                BuyQuantityText = model.BuyQuantityText,
                PayQuantityText = model.PayQuantityText,
                DateRange = TarihAraligiFomatle(model.BaslangicTarihi, model.BitisTarihi),
                BaslangicTarihi = model.BaslangicTarihi,
                BitisTarihi = model.BitisTarihi,
                Durum = "Taslak",
                EkleyenKullaniciId = kullaniciId,
                OlusturmaTarihi = DateTime.Now
            };

        _db.Campaigns.Add(kampanya);
        await _db.SaveChangesAsync();

        if (model.StoreIds != null)
        {
            foreach (var storeId in model.StoreIds)
                _db.CampaignStores.Add(new LCM.Domain.Entities.CampaignStore
                {
                    CampaignId = kampanya.Id,
                    StoreId = storeId
                });
            await _db.SaveChangesAsync();
        }

        // İlk logu at — oluşturuldu
        await _statusService.StatuGuncelle(kampanya, kampanya.Durum, kullaniciId, "Oluşturuldu");

        // Admin ise model'den gelen statüyü kullan
        if (kullanicıRol == "Admin" && !string.IsNullOrEmpty(model.Durum))
        {
            await _statusService.StatuGuncelle(kampanya, model.Durum, kullaniciId, "Statü Belirlendi");
        }
        // VeriGirisi ise onay akışına sok
        else if (kullanicıRol == "VeriGirisi" || kullanicıRol == "KampanyaYonetici")
        {
            if (onayAktif)
            {
                // Kampanya yöneticilerinden birini bul
                var yonetici = await _db.Users
                    .FirstOrDefaultAsync(u => u.Rol.RolAdi == "KampanyaYonetici" && u.AktifMi);

                if (yonetici != null)
                {
                    await _statusService.OnayaGonder(kampanya, kullaniciId, yonetici.Id);

                    // Mail gönder
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    var mailSonuc = await _mailService.OnayBekliyorMailiGonder(kampanya, yonetici.Email, baseUrl);
                    if (mailSonuc.Gonderildi)
                        await _statusService.StatuGuncelle(kampanya, kampanya.Durum, kullaniciId,
                            "Mail Gönderildi", $"Onay maili gönderildi", mailSonuc.Email);
                    else if (mailSonuc.Email != null)
                        await _statusService.StatuGuncelle(kampanya, kampanya.Durum, kullaniciId,
                            "Mail Gönderilemedi", null, mailSonuc.Email);
                }
            }
            else
            {
                var otomatikStatu = await _statusService.YeniKampanyaStatuBelirle(
                    false, kampanya.BaslangicTarihi, kampanya.BitisTarihi);
                await _statusService.StatuGuncelle(kampanya, otomatikStatu, kullaniciId, "Statü Otomatik Belirlendi");
            }
        }

        TempData["Basari"] = $"Kampanya oluşturuldu. SKU: {sku} | Barkod: {barkod}";
        return RedirectToAction("Index");
    }

    // Düzenle - Form
    [Authorize(Roles = "Admin,KampanyaYonetici,VeriGirisi")]
    public async Task<IActionResult> Edit(int id)
    {
        var kampanya = await _db.Campaigns
                .Include(c => c.CampaignStores).ThenInclude(cs => cs.Store)
                .Include(c => c.Sablon).ThenInclude(s => s.Alanlar)
                .Include(c => c.Sablon).ThenInclude(s => s.EtiketTip)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (kampanya == null) return NotFound();

            await SablonlariDoldur();

            var model = new CampaignEditViewModel
            {
                Id = kampanya.Id,
                Sku = kampanya.Sku,
                Barkod = kampanya.Barkod,
                SablonId = kampanya.SablonId,
                Baslik = kampanya.Baslik,
                AltBaslik = kampanya.AltBaslik,
                KampanyaNotu = kampanya.KampanyaNotu,
                Subheadline = kampanya.Subheadline,
                OriginalPrice = kampanya.OriginalPrice,
                DiscountedPrice = kampanya.DiscountedPrice,
                Headline = kampanya.Headline,
                MinBasketText = kampanya.MinBasketText,
                DetailText = kampanya.DetailText,
                CampaignDescription = kampanya.CampaignDescription,
                IsLocalProduction = kampanya.IsLocalProduction,
                OriginCountry = kampanya.OriginCountry,
                UnitPrice = kampanya.UnitPrice,
                PriceUpdateDate = kampanya.PriceUpdateDate,
                BuyQuantityText = kampanya.BuyQuantityText,
                PayQuantityText = kampanya.PayQuantityText,
                DateRange = kampanya.DateRange,
                BaslangicTarihi = kampanya.BaslangicTarihi,
                BitisTarihi = kampanya.BitisTarihi,
                Durum = kampanya.Durum
            };
            await StorelariDoldur();
            model.StoreIds = kampanya.CampaignStores.Select(cs => cs.StoreId).ToList();
            return View(model);
        }

    // Düzenle - Kaydet
    [HttpPost]
    [Authorize(Roles = "Admin,KampanyaYonetici,VeriGirisi")]
    public async Task<IActionResult> Edit(CampaignEditViewModel model)
    {
            if (!ModelState.IsValid)
            {
                await SablonlariDoldur();
                await StorelariDoldur();   // ← bu eksik
                return View(model);
            }
            var kampanya = await _db.Campaigns
                .Include(c => c.CampaignStores)
                .FirstOrDefaultAsync(c => c.Id == model.Id);
            if (kampanya == null) return NotFound();

            kampanya.SablonId = model.SablonId;
            kampanya.Baslik = model.Baslik;
            kampanya.AltBaslik = model.AltBaslik;
            kampanya.KampanyaNotu = model.KampanyaNotu;
            kampanya.Subheadline = model.Subheadline;
            kampanya.OriginalPrice = model.OriginalPrice;
            kampanya.DiscountedPrice = model.DiscountedPrice;
            kampanya.Headline = model.Headline;
            kampanya.MinBasketText = model.MinBasketText;
            kampanya.DetailText = model.DetailText;
            kampanya.CampaignDescription = model.CampaignDescription;
            kampanya.IsLocalProduction = model.IsLocalProduction;
            kampanya.OriginCountry = model.OriginCountry;
            kampanya.UnitPrice = model.UnitPrice;
            kampanya.PriceUpdateDate = model.PriceUpdateDate;
            kampanya.BuyQuantityText = model.BuyQuantityText;
            kampanya.PayQuantityText = model.PayQuantityText;
            kampanya.DateRange = TarihAraligiFomatle(model.BaslangicTarihi, model.BitisTarihi);
            kampanya.BaslangicTarihi = model.BaslangicTarihi;
            kampanya.BitisTarihi = model.BitisTarihi;
            kampanya.Durum = model.Durum;
            // Mevcut store bağlantılarını temizle, yenilerini ekle
            var eskiStores = _db.CampaignStores.Where(cs => cs.CampaignId == kampanya.Id);
            _db.CampaignStores.RemoveRange(eskiStores);
            if (model.StoreIds != null)
            {
                foreach (var storeId in model.StoreIds)
                    _db.CampaignStores.Add(new LCM.Domain.Entities.CampaignStore { CampaignId = kampanya.Id, StoreId = storeId });
            }

        await _db.SaveChangesAsync();

        // VeriGirisi düzenledi — tekrar onaya gönder
        var kullaniciId = int.Parse(User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var kullanicıRol = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        await _statusService.StatuGuncelle(kampanya, kampanya.Durum, kullaniciId, "Düzenlendi");

        if (kullanicıRol == "VeriGirisi")
        {
            var onayAyar = await _db.SystemSettings
                .FirstOrDefaultAsync(s => s.AyarAdi == "KampanyaOnayaGonderilsinMi");

            if (onayAyar?.AktifMi == true)
            {
                var yonetici = await _db.Users
                    .FirstOrDefaultAsync(u => u.Rol.RolAdi == "KampanyaYonetici" && u.AktifMi);

                if (yonetici != null)
                {
                    await _statusService.OnayaGonder(kampanya, kullaniciId, yonetici.Id);

                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    var mailSonuc = await _mailService.OnayBekliyorMailiGonder(
                        kampanya, yonetici.Email, baseUrl);
                    if (mailSonuc.Gonderildi)
                        await _statusService.StatuGuncelle(kampanya, kampanya.Durum, kullaniciId,
                            "Mail Gönderildi", "Düzenleme sonrası onay maili", mailSonuc.Email);
                }
            }
        }

        TempData["Basari"] = "Kampanya güncellendi.";
        return RedirectToAction("Index");
    }

        // Sil
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var kampanya = await _db.Campaigns
                     .Include(c => c.CampaignStores)
                     .FirstOrDefaultAsync(c => c.Id == id);
            if (kampanya == null) return NotFound();

            _db.Campaigns.Remove(kampanya);
            await _db.SaveChangesAsync();
            TempData["Basari"] = "Kampanya silindi.";
            return RedirectToAction("Index");
        }

        // Detay
        public async Task<IActionResult> Detail(int id)
        {
            var kampanya = await _db.Campaigns
               .Include(c => c.Sablon)
                   .ThenInclude(s => s.EtiketTip)
               .Include(c => c.EkleyenKullanici)
               .Include(c => c.CampaignStores).ThenInclude(cs => cs.Store)
               .FirstOrDefaultAsync(c => c.Id == id);

            if (kampanya == null) return NotFound();
            return View(kampanya);
        }
    

        // Yazdır
        public async Task<IActionResult> Print(int id)
        {
            var kampanya = await _db.Campaigns
                .Include(c => c.Sablon)
                    .ThenInclude(s => s.EtiketTip)
                .Include(c => c.EkleyenKullanici)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (kampanya == null) return NotFound();
            return View(kampanya);
        }

        // Liste Yazdır
        public async Task<IActionResult> PrintList()
        {
            var liste = await _db.Campaigns
                .Include(c => c.Sablon)
                .OrderByDescending(c => c.OlusturmaTarihi)
                .ToListAsync();

            return View(liste);
        }

        // AJAX - Şablon seçilince alanları getir
        [HttpGet]
        public async Task<IActionResult> GetTemplateFields(int templateId)
        {
            var sablon = await _db.Templates
                .Include(t => t.Alanlar)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (sablon == null) return Json(new { });

            var sabitSira = new List<string>
    {
        "Baslik", "AltBaslik", "KampanyaNotu", "MinBasketText",
        "Headline", "Subheadline", "DetailText",
        "OriginalPrice", "DiscountedPrice",
        "DateRange", "CampaignDescription",
        "IsLocalProduction", "OriginCountry", "UnitPrice", "PriceUpdateDate",
        "BuyQuantityText", "PayQuantityText"
    };
            // Layout JSON'ını da getir
            Layout? layout = null;
            if (!string.IsNullOrEmpty(sablon.LayoutKodu))
            {
                layout = await _db.Layouts
                    .FirstOrDefaultAsync(l => l.LayoutKodu == sablon.LayoutKodu);
            }
            // Layout JSON'undan Y pozisyonlarını çek
            var yPozisyonlari = new Dictionary<string, int>();
            if (!string.IsNullOrEmpty(layout?.LayoutJson))
            {
                var layoutObj = System.Text.Json.JsonDocument.Parse(layout.LayoutJson);
                if (layoutObj.RootElement.TryGetProperty("alanlar", out var layoutAlanlar))
                {
                    foreach (var la in layoutAlanlar.EnumerateArray())
                    {
                        var ad = la.TryGetProperty("ad", out var adEl) ? adEl.GetString() : null;
                        var y = la.TryGetProperty("y", out var yEl) ? yEl.GetInt32() : 9999;
                        if (!string.IsNullOrEmpty(ad))
                            yPozisyonlari[ad] = y;
                    }
                }
            }

            var alanlar = sablon.Alanlar
                .Where(a => a.AktifMi)
                .OrderBy(a => yPozisyonlari.ContainsKey(a.AlanAdi) ? yPozisyonlari[a.AlanAdi] : 9999)
                .Select(a => a.AlanAdi)
                .ToList();

       

            // Layout JSON'undan etiket bilgilerini çek
            var etiketler = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(layout?.LayoutJson))
            {
                var layoutObj = System.Text.Json.JsonDocument.Parse(layout.LayoutJson);
                if (layoutObj.RootElement.TryGetProperty("alanlar", out var layoutAlanlar))
                {
                    foreach (var la in layoutAlanlar.EnumerateArray())
                    {
                        var ad = la.TryGetProperty("ad", out var adEl) ? adEl.GetString() : null;
                        var etiket = la.TryGetProperty("etiket", out var etiketEl) ? etiketEl.GetString() : null;
                        if (!string.IsNullOrEmpty(ad) && !string.IsNullOrEmpty(etiket))
                            etiketler[ad] = etiket;
                    }
                }
            }

            return Json(new
            {
                alanlar,
                etiketler,
                fotoYolu = sablon.SablonFotoYolu,
                sablonAdi = sablon.SablonAdi,
                layoutKodu = sablon.LayoutKodu,
                layoutJson = layout?.LayoutJson
            });
        }

        // ── Yardımcı Metodlar ────────────────────────────────

        private async Task<long> SkuUret()
        {
            var sonSku = await _db.Campaigns
                .OrderByDescending(c => c.Sku)
                .Select(c => c.Sku)
                .FirstOrDefaultAsync();

            return sonSku == 0 ? 90000000 : sonSku + 1;
        }

        private async Task SablonlariDoldur()
        {
            var sablonlar = await _db.Templates
                .OrderBy(t => t.SablonAdi)
                .ToListAsync();
            ViewBag.Sablonlar = new SelectList(sablonlar, "Id", "SablonAdi");
        }
        private async Task StorelariDoldur()
        {
            var stores = await _db.Stores
                .Where(s => s.AktifMi)
                .OrderBy(s => s.StoreCode)
                .ToListAsync();
            ViewBag.Stores = stores;
        }

        private async Task CaptchaHazirla(string ayarAdi)
        {
            var ayar = await _db.SystemSettings
                .FirstOrDefaultAsync(s => s.AyarAdi == ayarAdi);

            if (ayar?.AktifMi == true)
            {
                var (sayi1, sayi2, sonuc) = CaptchaHelper.YeniSoru();
                HttpContext.Session.SetInt32("CaptchaCevap", sonuc);
                ViewBag.CaptchaAktif = true;
                ViewBag.CaptchaSoru = $"{sayi1} + {sayi2} = ?";
            }
            else
            {
                ViewBag.CaptchaAktif = false;
            }
        }
        private static string TarihAraligiFomatle(DateTime baslangic, DateTime bitis)
        {
            var turkceTakvim = new System.Globalization.CultureInfo("tr-TR");
            if (baslangic.Month == bitis.Month && baslangic.Year == bitis.Year)
            {
                return $"{baslangic.Day} - {bitis.Day} {bitis.ToString("MMMM yyyy", turkceTakvim)}";
            }
            else
            {
                return $"{baslangic.ToString("d MMMM", turkceTakvim)} - {bitis.ToString("d MMMM yyyy", turkceTakvim)}";
            }
        }
        [HttpPost]
        [Authorize(Roles = "Admin,KampanyaYonetici,VeriGirisi")]
        public async Task<IActionResult> EslGonder(int id)
        {
            try
            {
                var kullaniciId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                var sonuclar = await _eslGonderimService.TekliGonderAsync(id, kullaniciId);

                if (!sonuclar.Any())
                    return Json(new { success = false, message = "Sonuç alınamadı." });

                var basariliSayisi = sonuclar.Count(s => s.Basarili);
                var basarisizSayisi = sonuclar.Count(s => !s.Basarili);
                var toplamSube = sonuclar.Count;

                // Tab sonuçları için şube bazlı liste
                var subeSonuclari = sonuclar.Select(s => new
                {
                    storeCode = s.StoreCode,
                    basarili = s.Basarili,
                    hata = s.HataMesaji
                }).ToList();

                if (basariliSayisi == toplamSube)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Tüm şubeler başarılı. ({toplamSube} şube gönderildi)",
                        subeSonuclari
                    });
                }
                else if (basariliSayisi > 0)
                {
                    var hatalar = sonuclar
                        .Where(s => !s.Basarili)
                        .Select(s => $"{s.StoreCode}: {s.HataMesaji}")
                        .ToList();

                    return Json(new
                    {
                        success = true,
                        message = $"{basariliSayisi} şube başarılı, {basarisizSayisi} şube başarısız. Hatalar: {string.Join(" | ", hatalar)}",
                        subeSonuclari
                    });
                }
                else
                {
                    var hatalar = sonuclar
                        .Select(s => $"{s.StoreCode}: {s.HataMesaji}")
                        .ToList();

                    return Json(new
                    {
                        success = false,
                        message = $"Tüm şubeler başarısız. {string.Join(" | ", hatalar)}",
                        subeSonuclari
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata oluştu: {ex.Message}" });
            }
        }
        [HttpGet]
        [Authorize(Roles = "Admin,KampanyaYonetici,VeriGirisi")]
        public async Task<IActionResult> EslJsonOnizle(int id)
        {
            try
            {
                var ayar = await _db.EslApiSettings.FirstOrDefaultAsync();
                if (ayar == null)
                    return Json(new { success = false, message = "ESL API ayarları bulunamadı." });

                var kampanya = await _db.Campaigns
                    .Include(c => c.CampaignStores).ThenInclude(cs => cs.Store)
                    .Include(c => c.Sablon).ThenInclude(s => s.Alanlar)
                    .Include(c => c.Sablon).ThenInclude(s => s.EtiketTip)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (kampanya == null)
                    return Json(new { success = false, message = "Kampanya bulunamadı." });

                var mappings = await _db.FieldMappings
                    .Include(m => m.EslField)
                    .Where(m => m.SablonId == kampanya.SablonId)
                    .ToListAsync();

                var serializerOpt = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var subeler = new List<object>();

                foreach (var cs in kampanya.CampaignStores)
                {
                    var item = new Dictionary<string, string>();

                    foreach (var mapping in mappings)
                    {
                        var deger = AlanDegerAlOnizle(kampanya, mapping.BizimAlanAdi);
                        if (deger != null && mapping.EslField != null)
                            item[mapping.EslField.VariableName] = deger;
                    }

                    if (!item.ContainsKey("sku")) item["sku"] = kampanya.Sku.ToString();
                    if (!item.ContainsKey("ean")) item["ean"] = kampanya.Barkod;

                    // grade her zaman şablonun etiket tipinden gelir, mapping'e bakılmaz
                    var gradeVal = kampanya.Sablon?.EtiketTip?.EtiketTipi;
                    if (!string.IsNullOrEmpty(gradeVal))
                        item["grade"] = gradeVal;
                    var payload = new
                    {
                        customerStoreCode = ayar.CustomerStoreCode,
                        storeCode = cs.Store.StoreCode,
                        batchNo = "",
                        items = new[] { item }
                    };

                    subeler.Add(new
                    {
                        storeCode = cs.Store.StoreCode,
                        storeName = cs.Store.StoreName,
                        json = System.Text.Json.JsonSerializer.Serialize(payload, serializerOpt)
                    });
                }

                return Json(new { success = true, subeler });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        private static string? AlanDegerAlOnizle(Campaign k, string alanAdi) => alanAdi switch
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
    // Kampanya Yöneticisi — bekleyen kampanyalar listesi
    [Authorize(Roles = "Admin,KampanyaYonetici")]
    public async Task<IActionResult> OnayBekleyenler()
    {
        var liste = await _db.Campaigns
            .Include(c => c.Sablon)
            .Include(c => c.EkleyenKullanici)
            .Where(c => c.Durum == "Onay Bekliyor")
            .OrderByDescending(c => c.OlusturmaTarihi)
            .ToListAsync();

        return View(liste);
    }

    // Onayla
    [HttpPost]
    [Authorize(Roles = "KampanyaYonetici,Admin")]
    public async Task<IActionResult> Onayla(int id)
    {
        var kampanya = await _db.Campaigns
            .Include(c => c.EkleyenKullanici)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (kampanya == null) return NotFound();

        var kullaniciId = int.Parse(User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        await _statusService.Onayla(kampanya, kullaniciId);

        // Sonuç maili gönder
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var mailSonuc = await _mailService.SonucMailiGonder(
            kampanya, kampanya.EkleyenKullanici.Email, "Onaylandı", null, baseUrl);
        if (mailSonuc.Gonderildi)
            await _statusService.StatuGuncelle(kampanya, kampanya.Durum, kullaniciId,
                "Mail Gönderildi", "Onay sonuç maili gönderildi", mailSonuc.Email);

        TempData["Basari"] = "Kampanya onaylandı.";
        return RedirectToAction("OnayBekleyenler");
    }

    // Reddet
    [HttpPost]
    [Authorize(Roles = "KampanyaYonetici,Admin")]
    public async Task<IActionResult> Reddet(int id, string yorum)
    {
        var kampanya = await _db.Campaigns
            .Include(c => c.EkleyenKullanici)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (kampanya == null) return NotFound();

        var kullaniciId = int.Parse(User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        await _statusService.Reddet(kampanya, kullaniciId, yorum);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var mailSonuc = await _mailService.SonucMailiGonder(
            kampanya, kampanya.EkleyenKullanici.Email, "Reddedildi", yorum, baseUrl);
        if (mailSonuc.Gonderildi)
            await _statusService.StatuGuncelle(kampanya, kampanya.Durum, kullaniciId,
                "Mail Gönderildi", "Red sonuç maili gönderildi", mailSonuc.Email);

        TempData["Basari"] = "Kampanya reddedildi.";
        return RedirectToAction("OnayBekleyenler");
    }

    // Revize İste
    [HttpPost]
    [Authorize(Roles = "KampanyaYonetici,Admin")]
    public async Task<IActionResult> RevizeIste(int id, string yorum)
    {
        var kampanya = await _db.Campaigns
            .Include(c => c.EkleyenKullanici)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (kampanya == null) return NotFound();

        var kullaniciId = int.Parse(User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        await _statusService.RevizeIste(kampanya, kullaniciId, yorum);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var mailSonuc = await _mailService.SonucMailiGonder(
            kampanya, kampanya.EkleyenKullanici.Email, "Revize İstendi", yorum, baseUrl);
        if (mailSonuc.Gonderildi)
            await _statusService.StatuGuncelle(kampanya, kampanya.Durum, kullaniciId,
                "Mail Gönderildi", "Revize sonuç maili gönderildi", mailSonuc.Email);

        TempData["Basari"] = "Revize talebi gönderildi.";
        return RedirectToAction("OnayBekleyenler");
    }

    // Veri Girişi — kendi kampanyaları
    [Authorize(Roles = "VeriGirisi")]
    public async Task<IActionResult> BenimKampanyalarim()
    {
        var kullaniciId = int.Parse(User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var liste = await _db.Campaigns
            .Include(c => c.Sablon)
            .Include(c => c.CurrentOwner)
            .Where(c => c.EkleyenKullaniciId == kullaniciId)
            .OrderByDescending(c => c.OlusturmaTarihi)
            .ToListAsync();

        return View(liste);
    }

    // Kampanya log/yol haritası (AJAX)
    [HttpGet]
    public async Task<IActionResult> Loglar(int id)
    {
        var loglar = await _db.CampaignLogs
            .Include(l => l.Kullanici)
            .Where(l => l.CampaignId == id)
            .OrderBy(l => l.Tarih)
            .ToListAsync();

        return Json(loglar.Select(l => new
        {
            aksiyon = l.Aksiyon,
            yorum = l.Yorum,
            hedefEmail = l.HedefEmail,
            tarih = l.Tarih.ToString("dd.MM.yyyy HH:mm"),
            kullanici = l.Kullanici != null ? l.Kullanici.Ad + " " + l.Kullanici.Soyad : "Sistem"
        }));
    }
    /// <summary>
    /// Index sayfasında hover popup için kampanya önizleme verisi döner.
    /// Layout JSON + alan değerlerini birlikte gönderir.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> HizliOnizle(int id)
    {
        var kampanya = await _db.Campaigns
            .Include(c => c.Sablon)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (kampanya == null)
            return Json(new { success = false });

        // Şablonun layout JSON'ını çek
        string? layoutJson = null;
        if (!string.IsNullOrEmpty(kampanya.Sablon?.LayoutKodu))
        {
            var layout = await _db.Layouts
                .FirstOrDefaultAsync(l => l.LayoutKodu == kampanya.Sablon.LayoutKodu);
            layoutJson = layout?.LayoutJson;
        }

        // Alan değerlerini sözlük olarak gönder
        // JS tarafı data.degerler.Baslik şeklinde erişiyor,
        // ASP.NET Core varsayılan JSON serializer camelCase üretir,
        // bu yüzden JS'de alan adlarını büyük harfle eşleştiriyoruz — 
        // ya da burada Dictionary kullanıyoruz ki isimler kesin olsun.
        return Json(new
        {
            success = true,
            layoutJson,
            degerler = new Dictionary<string, object?>
            {
                ["Baslik"] = kampanya.Baslik,
                ["AltBaslik"] = kampanya.AltBaslik,
                ["KampanyaNotu"] = kampanya.KampanyaNotu,
                ["Subheadline"] = kampanya.Subheadline,
                ["OriginalPrice"] = kampanya.OriginalPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["DiscountedPrice"] = kampanya.DiscountedPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["BuyQuantityText"] = kampanya.BuyQuantityText,
                ["PayQuantityText"] = kampanya.PayQuantityText,
                ["Headline"] = kampanya.Headline,
                ["MinBasketText"] = kampanya.MinBasketText,
                ["DetailText"] = kampanya.DetailText,
                ["CampaignDescription"] = kampanya.CampaignDescription,
                ["IsLocalProduction"] = kampanya.IsLocalProduction,
                ["OriginCountry"] = kampanya.OriginCountry,
                ["UnitPrice"] = kampanya.UnitPrice,
                ["PriceUpdateDate"] = kampanya.PriceUpdateDate,
                ["DateRange"] = kampanya.DateRange
            }
        });
    }
}