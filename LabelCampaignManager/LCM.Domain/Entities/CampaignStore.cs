namespace LCM.Domain.Entities;

public class CampaignStore
{
    public int CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;

    public int StoreId { get; set; }
    public Store Store { get; set; } = null!;
}