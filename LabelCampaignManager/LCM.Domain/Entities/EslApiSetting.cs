namespace LCM.Domain.Entities;

public class EslApiSetting
{
    public int Id { get; set; }
    public string ApiUrl { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string CustomerStoreCode { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "HS256";
    public string HeaderPrefix { get; set; } = "HSIAM1";
}