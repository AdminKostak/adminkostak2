using System.Security.Cryptography;
using System.Text;
using LCM.Domain.Entities;

namespace LCM.Infrastructure.Services;

public class EslApiService
{
    private readonly HttpClient _httpClient;

    public EslApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string UtcTarihUret()
    {
        return DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    public string JwtTokenUret(EslApiSetting ayar, string dateHeaderDegeri)
    {
        var header = Base64UrlEncode(
            Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}")
        );

        var payloadJson = $"{{\"access_key\":\"{ayar.AccessKey}\",\"date\":\"{dateHeaderDegeri}\"}}";
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

        var mesaj = $"{header}.{payload}";
        var secretBytes = Encoding.UTF8.GetBytes(ayar.SecretKey);
        using var hmac = new HMACSHA256(secretBytes);
        var imza = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(mesaj)));

        return $"{header}.{payload}.{imza}";
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public async Task<EslApiSonuc> GonderAsync(EslApiSetting ayar, string jsonBody, string dateHeader)
    {
        try
        {
            var jwtToken = JwtTokenUret(ayar, dateHeader);
            var authHeader = $"{ayar.HeaderPrefix} {jwtToken}";

            var request = new HttpRequestMessage(HttpMethod.Post, ayar.ApiUrl);
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var icerik = await response.Content.ReadAsStringAsync();

            return new EslApiSonuc
            {
                Basarili = response.IsSuccessStatusCode,
                StatusKod = (int)response.StatusCode,
                YanitIcerigi = icerik,
                GonderilenDate = dateHeader,
                GonderilenAuth = authHeader
            };
        }
        catch (Exception ex)
        {
            return new EslApiSonuc
            {
                Basarili = false,
                StatusKod = 0,
                YanitIcerigi = $"Bağlantı hatası: {ex.Message}",
                GonderilenDate = dateHeader,
                GonderilenAuth = ""
            };
        }
    }
}

public class EslApiSonuc
{
    public bool Basarili { get; set; }
    public int StatusKod { get; set; }
    public string YanitIcerigi { get; set; } = string.Empty;
    public string GonderilenDate { get; set; } = string.Empty;
    public string GonderilenAuth { get; set; } = string.Empty;
}