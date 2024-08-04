using DalaMock.Host.Mediator;

namespace AllaganMarket.Models;

using System;

public record ToggleWindow(Type WindowType) : MessageBase;

public record OpenWindow(Type WindowType) : MessageBase;

public record CloseWindow(Type WindowType) : MessageBase;
public record PluginLoaded() : MessageBase;
