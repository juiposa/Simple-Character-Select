using System;
using System.Collections.Generic;
using Dalamud.Plugin.Ipc;

namespace SimpleCharacterSelectPlugin.IPC;

public static class GlamourerIpc
{
    public static readonly ICallGateSubscriber<Dictionary<Guid, string>>? GetDesigns;
    public static readonly ICallGateSubscriber<Guid, int, uint, ulong, int>? ApplyDesign;
    public static readonly ICallGateSubscriber<int, uint, ulong, int>? RevertState;

    static GlamourerIpc()
    {
        GetDesigns = Plugin.PluginInterface.GetIpcSubscriber<Dictionary<Guid, string>>("Glamourer.GetDesignList.V2");
        ApplyDesign = Plugin.PluginInterface.GetIpcSubscriber<Guid, int, uint, ulong, int>("Glamourer.ApplyDesign");
        RevertState = Plugin.PluginInterface.GetIpcSubscriber<int, uint, ulong, int>("Glamourer.RevertState");
    }
}