using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Ipc;

namespace SimpleCharacterSelectPlugin.Integration;

public static class MoodlesIpc
{
    public static ICallGateSubscriber<nint, object>? ClearStatus;
    public static ICallGateSubscriber<List<(Guid, string)>>? GetPresets;
    public static ICallGateSubscriber<Guid, IPlayerCharacter, object>? ApplyPresetByPlayerV2;

    public static void Initialize()
    {
        ClearStatus = Plugin.PluginInterface.GetIpcSubscriber<nint, object>("Moodles.ClearStatusManagerByPtrV2");
        GetPresets = Plugin.PluginInterface.GetIpcSubscriber<List<(Guid, string)>>("Moodles.GetRegisteredProfilesV2");
        ApplyPresetByPlayerV2 = Plugin.PluginInterface.GetIpcSubscriber<Guid, IPlayerCharacter, object>("Moodles.ApplyPresetByPlayerV2");
    }
}