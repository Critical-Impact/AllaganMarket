using DalaMock.Host.Mediator;

namespace AllaganMarket.Models;

using System;

public record ToggleWindowMessage(Type WindowType) : MessageBase;

public record OpenWindowMessage(Type WindowType) : MessageBase;

public record CloseWindowMessage(Type WindowType) : MessageBase;
public record PluginLoadedMessage() : MessageBase;
public record ConfigurationModifiedMessage() : MessageBase;
