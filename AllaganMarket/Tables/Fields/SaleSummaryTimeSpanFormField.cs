using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;
using AllaganLib.Interface.Widgets;

using AllaganMarket.Models;

namespace AllaganMarket.Grids.Fields;

public class SaleSummaryTimeSpanFormField : TimeSpanFormField<SaleSummary>
{
    public SaleSummaryTimeSpanFormField(ImGuiService imGuiService) : base(new TimeSpanPickerWidget(), imGuiService)
    {
    }

    public override (TimeUnit, int)? DefaultValue { get; set; } = null;

    public override string Key { get; set; } = "SameSummaryTimeSpan";

    public override string Name { get; set; } = "Sale Summary Time Span";

    public override string HelpText { get; set; } = "";

    public override string Version { get; } = "1.0.0";
}
