using AllaganLib.Interface.FormFields;

namespace AllaganMarket.Settings;

public interface ISetting : IFormField<Configuration>
{
    public SettingType Type { get; set; }
}
