namespace LCM.Domain.Entities;

public class EslTask
{
    public int Id { get; set; }
    public string TaskName { get; set; } = string.Empty;

    // Kullanıcının girdiği: exec sp_GetDiscountEsls @store_code = '1233'
    public string SqlScript { get; set; } = string.Empty;

    // Cron ifadesi: "0 */15 * * * *" gibi
    public string CronExpression { get; set; } = string.Empty;

    // LED ayarları
    public string LedColor { get; set; } = "red"; // red, blue, green, yellow, violet, indigo, white
    public string LedCount { get; set; } = "50";
    public string LedOnTime { get; set; } = "10";
    public string LedOffTime { get; set; } = "5";
    public string LedSleepTime { get; set; } = "0";

    // Hangi store için çalışacak (EslApiSettings'den customerStoreCode alınır)
    public int StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    public ICollection<EslTaskLog> Logs { get; set; } = new List<EslTaskLog>();
}