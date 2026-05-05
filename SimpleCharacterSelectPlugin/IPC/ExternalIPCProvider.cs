using System;
using System.Collections.Generic;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace SimpleCharacterSelectPlugin;

public class ExternalIPCProvider
{
    // Target application IPC subscribers with correct signatures
    public ICallGateSubscriber<Dictionary<Guid, string>>? PenumbraGetCollectionsIpc;
    public ICallGateSubscriber<int, Guid?, bool, bool, (int, (Guid, string)?)>? PenumbraSetCollectionForObjectIpc;
    public ICallGateSubscriber<int, object>? PenumbraRedrawObjectIpc;
    public ICallGateSubscriber<Dictionary<Guid, string>>? GlamourerGetDesignsIpc;
    public ICallGateSubscriber<Guid, int, uint, ulong, int>? GlamourerApplyDesignIpc;
    public ICallGateSubscriber<IList<(Guid, string, string, IList<(string, ushort, byte, ushort)>, int, bool)>>? CustomizePlusGetProfileListIpc;
    public ICallGateSubscriber<Guid, (int, string?)>? CustomizePlusGetByUniqueIdIpc;
    public ICallGateSubscriber<ushort, string, (int, Guid?)>? CustomizePlusSetTempProfileIpc;

    // Revert operation IPC subscribers
    public ICallGateSubscriber<int, uint, ulong, int>? GlamourerRevertStateIpc;
    public ICallGateSubscriber<nint, object>? MoodlesClearStatusIpc;
    public ICallGateSubscriber<ushort, (int, Guid?)>? CustomizePlusGetActiveProfileIpc;
    public ICallGateSubscriber<Guid, int>? CustomizePlusDisableProfileIpc;
    public ICallGateSubscriber<int, int, object>? PenumbraRedrawIpc;

    public ExternalIPCProvider(IDalamudPluginInterface pluginInterface)
    {
        // Initialize target application IPC subscribers with correct signatures
        PenumbraGetCollectionsIpc = pluginInterface.GetIpcSubscriber<Dictionary<Guid, string>>("Penumbra.GetCollections.V5");
        PenumbraSetCollectionForObjectIpc = pluginInterface.GetIpcSubscriber<int, Guid?, bool, bool, (int, (Guid, string)?)>("Penumbra.SetCollectionForObject.V5");
        PenumbraRedrawObjectIpc = pluginInterface.GetIpcSubscriber<int, object>("Penumbra.RedrawObject.V5");
        GlamourerGetDesignsIpc = pluginInterface.GetIpcSubscriber<Dictionary<Guid, string>>("Glamourer.GetDesignList.V2");
        GlamourerApplyDesignIpc = pluginInterface.GetIpcSubscriber<Guid, int, uint, ulong, int>("Glamourer.ApplyDesign");
        CustomizePlusGetProfileListIpc = pluginInterface.GetIpcSubscriber<IList<(Guid, string, string, IList<(string, ushort, byte, ushort)>, int, bool)>>("CustomizePlus.Profile.GetList");
        CustomizePlusGetByUniqueIdIpc = pluginInterface.GetIpcSubscriber<Guid, (int, string?)>("CustomizePlus.Profile.GetByUniqueId");
        CustomizePlusSetTempProfileIpc = pluginInterface.GetIpcSubscriber<ushort, string, (int, Guid?)>("CustomizePlus.Profile.SetTemporaryProfileOnCharacter");

        // Revert operation IPC subscribers
        GlamourerRevertStateIpc = pluginInterface.GetIpcSubscriber<int, uint, ulong, int>("Glamourer.RevertState");
        MoodlesClearStatusIpc = pluginInterface.GetIpcSubscriber<nint, object>("Moodles.ClearStatusManagerByPtrV2");
        CustomizePlusGetActiveProfileIpc = pluginInterface.GetIpcSubscriber<ushort, (int, Guid?)>("CustomizePlus.Profile.GetActiveProfileIdOnCharacter");
        CustomizePlusDisableProfileIpc = pluginInterface.GetIpcSubscriber<Guid, int>("CustomizePlus.Profile.DisableByUniqueId");
        PenumbraRedrawIpc = pluginInterface.GetIpcSubscriber<int, int, object>("Penumbra.RedrawObject.V5");
    }
}