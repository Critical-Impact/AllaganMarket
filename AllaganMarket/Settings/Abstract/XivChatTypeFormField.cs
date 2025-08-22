using System;
using System.Collections.Generic;
using System.Linq;

using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

using Dalamud.Game.Text;

namespace AllaganMarket.Settings.Abstract;

public abstract class XivChatTypeFormField : EnumFormField<XivChatType, Configuration>
{
    private readonly Dictionary<Enum, string> choices;

    protected XivChatTypeFormField(ImGuiService imGuiService)
        : base(imGuiService)
    {
        this.choices = Enum.GetValues<XivChatType>().ToDictionary(c => (Enum)c, c => XivChatTypeExtensions.GetDetails(c)?.FancyName ?? c.ToString());
    }

    public override bool Equal(Enum item1, Enum item2)
    {
        return Equals(item1, item2);
    }

    public override Dictionary<Enum, string> Choices => this.choices;
}
