using System;

using AllaganMarket.Models;

using DalaMock.Host.Mediator;

namespace AllaganMarket.Mediator;
#pragma warning disable SA1649
#pragma warning disable SA1402

public record DeleteSoldItem(SoldItem SoldItem) : MessageBase;

public record OpenMoreInformation(uint ItemId) : MessageBase;

public record ToggleWindowMessage(Type WindowType) : MessageBase;

public record OpenWindowMessage(Type WindowType) : MessageBase;

public record CloseWindowMessage(Type WindowType) : MessageBase;

public record PluginLoadedMessage() : MessageBase;

public record ConfigurationModifiedMessage() : MessageBase;

#pragma warning restore SA1402
#pragma warning restore SA1649
