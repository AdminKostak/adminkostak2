namespace LCM.Domain.Entities;

public class EslTaskLog
{
    public int Id { get; set; }
    public int EslTaskId { get; set; }
    public EslTask EslTask { get; set; } = null!;

    public DateTime LogTarihi { get; set; } = DateTime.Now;
    public string Mesaj { get; set; } = string.Empty;      // "125 etikette komut gönderildi"
    public bool BasariliMi { get; set; }
    public string? HataMesaji { get; set; }
    public string? GonderilenJson { get; set; }            // API'ye giden payload
    public int? EslSayisi { get; set; }                    // Kaç ESL'e gönderildi
}