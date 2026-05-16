namespace LCM.Web.ViewModels;

public class EslTaskListeViewModel
{
    public int Id { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string SqlScript { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public string LedColor { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public int ToplamLog { get; set; }
    public DateTime? SonCalisma { get; set; }
    public bool? SonSonuc { get; set; }
}

public class EslTaskFormViewModel
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Görev adı zorunludur.")]
    public string TaskName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "SQL zorunludur.")]
    public string SqlScript { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Cron ifadesi zorunludur.")]
    public string CronExpression { get; set; } = string.Empty;

    public string LedColor { get; set; } = "red";
    public string LedCount { get; set; } = "50";
    public string LedOnTime { get; set; } = "10";
    public string LedOffTime { get; set; } = "5";
    public string LedSleepTime { get; set; } = "0";

    public int StoreId { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Storeler { get; set; } = new();
}

public class EslTaskLogViewModel
{
    public int Id { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public DateTime LogTarihi { get; set; }
    public string Mesaj { get; set; } = string.Empty;
    public bool BasariliMi { get; set; }
    public string? HataMesaji { get; set; }
    public string? GonderilenJson { get; set; }
    public int? EslSayisi { get; set; }
}