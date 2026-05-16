using LCM.Domain.Entities;

namespace LCM.Web.ViewModels;

public class EslEslestirmeAnaSayfaViewModel
{
    public List<Store> YetkiliStoreler { get; set; } = new();
    public bool HizliEslestirmeIzni { get; set; }
    public bool CokluEslestirmeIzni { get; set; }
}

public class EslEslestirmeIslemViewModel
{
    public int StoreId { get; set; }
    public int KampanyaId { get; set; }
    public List<string> EslBarkodlar { get; set; } = new();
    public bool HizliMod { get; set; }
}

public class OperatorLogViewModel
{
    public List<EslEslestirme> Eslestirmeler { get; set; } = new();
}