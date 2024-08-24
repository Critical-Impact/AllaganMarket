using AllaganMarket.Models;

namespace AllaganMarket.Extensions;

public static class SaleSummaryGroupExtensions
{
    public static string FormattedName(this SaleSummaryGroup saleSummaryGroup)
    {
        return saleSummaryGroup switch
        {
            SaleSummaryGroup.None => "None",
            SaleSummaryGroup.Item => "Item",
            SaleSummaryGroup.World => "World",
            SaleSummaryGroup.IsHq => "Is HQ",
            SaleSummaryGroup.Owner => "Owner",
            SaleSummaryGroup.Retainer => "Retainer",
            _ => saleSummaryGroup.ToString(),
        };
    }
}
