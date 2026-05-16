namespace LCM.Web;

public static class AlanAdlari
{
    public static readonly Dictionary<string, string> TurkceAdlar = new()
    {
        { "Baslik",              "BAŞLIK" },
        { "AltBaslik",           "ÜRÜN ANA SATIR" },
        { "KampanyaNotu",        "DESCRIPTION" },
        { "Subheadline",         "ÜRÜN ALT SATIR" },
        { "Headline",            "ÜRÜN DETAY SATIRI" },
        { "MinBasketText",       "SEPET KOŞULU" },
        { "DetailText",          "ÜRÜN DETAY SATIRI" },
        { "OriginalPrice",       "ESKİ FİYAT" },
        { "DiscountedPrice",     "YENİ FİYAT" },
        { "DateRange",           "TARİH ARALIĞI" },
        { "CampaignDescription", "KAMPANYA AÇIKLAMASI" },
        { "IsLocalProduction",   "YERLİ ÜRETİM LOGO" },
        { "OriginCountry",       "ÜRETİM YERİ" },
        { "UnitPrice",           "BİRİM FİYATI" },
        { "PriceUpdateDate",     "FİYAT DEĞİŞİKLİK TARİHİ" },
        { "BuyQuantityText",     "AL MİKTARI" },
        { "PayQuantityText",     "ÖDE MİKTARI" }
    };

    public static string GetTurkce(string dbAdi)
        => TurkceAdlar.TryGetValue(dbAdi, out var ad) ? ad : dbAdi;
}