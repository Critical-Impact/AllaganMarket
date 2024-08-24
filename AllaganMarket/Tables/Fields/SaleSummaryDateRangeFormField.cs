using System;

using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;
using AllaganLib.Interface.Widgets;

using AllaganMarket.Models;

namespace AllaganMarket.Tables.Fields;

public class SaleSummaryDateRangeFormField(ImGuiService imGuiService)
    : DateRangeFormField<SaleSummary>(new DateRangePickerWidget(), imGuiService)
{
    public override (DateTime, DateTime)? DefaultValue { get; set; } = null;

    public override string Key { get; set; } = "SaleSummaryDateRange";

    public override string Name { get; set; } = "Sale Summary Date Range";

    public override string HelpText { get; set; } = "Sets the date range for the sale summary results.";

    public override string Version { get; } = "1.0.0";
}
