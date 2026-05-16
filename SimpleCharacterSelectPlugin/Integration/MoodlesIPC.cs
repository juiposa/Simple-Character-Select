using System;
using System.Collections.Generic;
using Dalamud.Plugin.Ipc;

namespace SimpleCharacterSelectPlugin.IPC;

public static class MoodlesIpc
{
    public static readonly ICallGateSubscriber<nint, object>? ClearStatus;
    public static readonly ICallGateSubscriber<List<(Guid, string)>>? GetPresets;

    static MoodlesIpc()
    {
        ClearStatus = Plugin.PluginInterface.GetIpcSubscriber<nint, object>("Moodles.ClearStatusManagerByPtrV2");
        GetPresets = Plugin.PluginInterface.GetIpcSubscriber<List<(Guid, string)>>("Moodles.GetRegisteredProfilesV2");
    }
}