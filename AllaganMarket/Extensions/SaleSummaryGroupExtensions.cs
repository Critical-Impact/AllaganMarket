using AllaganMarket.Models;

namespace AllaganMarket.Extensions;

public static class SaleSummaryGroupExtensions
{
    public static string FormattedName(this SaleSummaryGroup saleSummaryGroup)
    {
        switch (saleSummaryGroup)
        {
            case SaleSummaryGroup.None:
                return "None";
                break;
            case SaleSummaryGroup.Item:
                return "Item";
                break;
            case SaleSummaryGroup.World:
                return "World";
                break;
            case SaleSummaryGroup.IsHq:
                return "Is HQ";
                break;
            case SaleSummaryGroup.Owner:
                return "Owner";
                break;
            case SaleSummaryGroup.Retainer:
                return "Retainer";
                break;
        }

        return saleSummaryGroup.ToString();
    }
}
