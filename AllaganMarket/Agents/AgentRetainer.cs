namespace AllaganMarket.Agents;

using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

[Agent(AgentId.Retainer)]
[StructLayout(LayoutKind.Explicit, Size = 0x4B84)]
public struct AgentRetainer
{
    [FieldOffset(0x0)]
    public AgentInterface AgentInterface;

    [FieldOffset(0x5C)]
    public byte SelectedSlot;

    [FieldOffset(0x4B7C)]
    public uint CurrentPrice;

    [FieldOffset(0x4B80)]
    public bool AdjustingPrice;
}