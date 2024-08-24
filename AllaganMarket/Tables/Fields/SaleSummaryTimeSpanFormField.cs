using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;
using AllaganLib.Interface.Widgets;

using AllaganMarket.Models;

namespace AllaganMarket.Tables.Fields;

public class SaleSummaryTimeSpanFormField(ImGuiService imGuiService)
    : TimeSpanFormField<SaleSummary>(new TimeSpanPickerWidget(), imGuiService)
{
    public override (TimeUnit, int)? DefaultValue { get; set; } = null;

    public override string Key { get; set; } = "SameSummaryTimeSpan";

    public override string Name { get; set; } = "Sale Summary Time Span";

    public override string HelpText { get; set; } = string.Empty;

    public override string Version { get; } = "1.0.0";
}
