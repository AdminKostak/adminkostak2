namespace LCM.Domain.Entities;

public class Store
{
    public int Id { get; set; }
    public string StoreCode { get; set; } = string.Empty;  // örn: "1344"
    public string StoreName { get; set; } = string.Empty;  // örn: "Bağcılar Şubesi"
    public bool AktifMi { get; set; } = true;
}