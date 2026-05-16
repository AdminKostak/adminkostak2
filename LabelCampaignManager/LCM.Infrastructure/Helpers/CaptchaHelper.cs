namespace LCM.Infrastructure.Helpers;

public static class CaptchaHelper
{
    public static (int Sayi1, int Sayi2, int Sonuc) YeniSoru()
    {
        var rnd = new Random();
        int sayi1 = rnd.Next(1, 10);
        int sayi2 = rnd.Next(1, 10);
        return (sayi1, sayi2, sayi1 + sayi2);
    }
}