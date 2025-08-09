using System;
using System.Collections.Generic;
using System.Globalization;

using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;

using DalaMock.Host.Mediator;

using Dalamud.Bindings.ImGui;

namespace AllaganMarket.Tables.Columns;

public abstract class GilColumn(NumberFormatInfo gilFormat, ImGuiService imGuiService, StringColumnFilter stringColumnFilter)
    : IntegerColumn<SearchResultConfiguration, SearchResult, MessageBase>(imGuiService, stringColumnFilter)
{
    public override IEnumerable<MessageBase>? Draw(SearchResultConfiguration config, SearchResult item, int rowIndex, int columnIndex)
    {
        ImGui.TableNextColumn();
        if (ImGui.TableGetColumnFlags().HasFlag(ImGuiTableColumnFlags.IsEnabled))
        {
            var str = this.CurrentValue(item);
            if (str != null)
            {
                ImGui.AlignTextToFramePadding();
                if (int.TryParse(str, CultureInfo.CurrentCulture, out var result))
                {
                    ImGui.TextUnformatted(result.ToString("C", gilFormat));
                }
                else
                {
                    ImGui.TextUnformatted(str);
                }
            }
            else
            {
                ImGui.TextUnformatted(this.EmptyText);
            }
        }

        return null;
    }
}
