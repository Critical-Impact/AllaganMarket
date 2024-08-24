using System.Collections.Generic;

using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Wizard;

namespace AllaganMarket.Features;

public class GameIntegrationsFeature(IEnumerable<IFormField<Configuration>> settings) : Feature<Configuration>(
    [
    ],
    settings)
{
    public override string Name { get; } = "Game Integrations";

    public override string Description { get; } = "Activate features that integrate directly into the game.";
}
