using System;
using System.Collections.Generic;

using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Wizard;

using AllaganMarket.Settings;

namespace AllaganMarket.Features;

public class MiscFeature : Feature<Configuration>
{
    public MiscFeature(IEnumerable<IFormField<Configuration>> settings)
        : base(
            [
                typeof(AddTitleMenuButtonSetting),
                typeof(AddDtrBarEntrySetting),
            ],
            settings)
    {
    }

    public override string Name { get; } = "Misc Features";

    public override string Description { get; } = "Miscellaneous features that improve your overall experience.";
}
