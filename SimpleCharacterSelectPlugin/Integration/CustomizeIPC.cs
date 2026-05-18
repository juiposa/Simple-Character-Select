using System;
using System.Collections.Generic;
using Dalamud.Plugin.Ipc;

namespace SimpleCharacterSelectPlugin.Integration;

public static class CustomizeIpc
{
    public static readonly ICallGateSubscriber<IList<(Guid, string, string, IList<(string, ushort, byte, ushort)>, int, bool)>>? GetProfileList;
    public static readonly ICallGateSubscriber<Guid, (int, string?)>? GetByUniqueId;
    public static readonly ICallGateSubscriber<ushort, string, (int, Guid?)>? SetTempProfile;
    public static readonly ICallGateSubscriber<ushort, (int, Guid?)>? GetActiveProfile;
    public static readonly ICallGateSubscriber<Guid, int>? DisableProfile;
    public static readonly ICallGateSubscriber<ushort, int>? DeleteTemporaryProfileOnCharacter;

    static CustomizeIpc()
    {
        GetProfileList = Plugin.PluginInterface.GetIpcSubscriber<IList<(Guid, string, string, IList<(string, ushort, byte, ushort)>, int, bool)>>("CustomizePlus.Profile.GetList");
        GetByUniqueId = Plugin.PluginInterface.GetIpcSubscriber<Guid, (int, string?)>("CustomizePlus.Profile.GetByUniqueId");
        SetTempProfile = Plugin.PluginInterface.GetIpcSubscriber<ushort, string, (int, Guid?)>("CustomizePlus.Profile.SetTemporaryProfileOnCharacter");
        GetActiveProfile = Plugin.PluginInterface.GetIpcSubscriber<ushort, (int, Guid?)>("CustomizePlus.Profile.GetActiveProfileIdOnCharacter");
        DisableProfile = Plugin.PluginInterface.GetIpcSubscriber<Guid, int>("CustomizePlus.Profile.DisableByUniqueId");
        DeleteTemporaryProfileOnCharacter = Plugin.PluginInterface.GetIpcSubscriber<ushort, int>("CustomizePlus.Profile.DeleteTemporaryProfileOnCharacter");
    }
}