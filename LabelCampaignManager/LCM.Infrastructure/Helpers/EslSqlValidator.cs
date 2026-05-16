namespace LCM.Infrastructure.Helpers;

public static class EslSqlValidator
{
    // Sadece exec ile başlamalı
    // Tehlikeli keyword'ler yasak
    private static readonly string[] YasakliKelimeler =
    [
        "drop", "delete", "truncate", "insert", "update",
        "alter", "create", "grant", "revoke", "xp_", "sp_executesql",
        "--", "/*", "*/", "shutdown", "bulk"
    ];

    public static (bool Gecerli, string? Hata) Dogrula(string sqlScript)
    {
        if (string.IsNullOrWhiteSpace(sqlScript))
            return (false, "SQL boş olamaz.");

        var temiz = sqlScript.Trim().ToLower();

        if (!temiz.StartsWith("exec ") && !temiz.StartsWith("exec\t"))
            return (false, "Sorgu yalnızca 'exec' ile başlayabilir.");

        foreach (var kelime in YasakliKelimeler)
        {
            if (temiz.Contains(kelime))
                return (false, $"Yasak ifade içeriyor: '{kelime}'");
        }

        return (true, null);
    }
}