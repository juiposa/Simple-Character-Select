using System;
using System.Collections.Generic;
using Dalamud.Plugin.Ipc;

namespace SimpleCharacterSelectPlugin.IPC;

public static class PenumbraIpc
{
    public static readonly ICallGateSubscriber<Dictionary<Guid, string>>? GetCollections;
    public static readonly ICallGateSubscriber<int, Guid?, bool, bool, (int, (Guid, string)?)>? SetCollectionForObject;
    public static readonly ICallGateSubscriber<int, object>? RedrawObject;
    public static readonly ICallGateSubscriber<int, int, object>? Redraw;
    public static readonly ICallGateSubscriber<int>? ApiVersion;
    public static readonly ICallGateSubscriber<byte, Guid?, bool, bool, (int, (Guid Id, string Name)?)>? SetCollection;
    public static readonly ICallGateSubscriber<byte, (Guid Id, string Name)?>? GetCollection;
    


    static PenumbraIpc()
    {
        GetCollections = Plugin.PluginInterface.GetIpcSubscriber<Dictionary<Guid, string>>("Penumbra.GetCollections.V5");
        GetCollection = Plugin.PluginInterface.GetIpcSubscriber<byte, (Guid Id, string Name)?>("Penumbra.GetCollection");
        SetCollectionForObject = Plugin.PluginInterface.GetIpcSubscriber<int, Guid?, bool, bool, (int, (Guid, string)?)>("Penumbra.SetCollectionForObject.V5");
        RedrawObject = Plugin.PluginInterface.GetIpcSubscriber<int, object>("Penumbra.RedrawObject.V5");
        Redraw = Plugin.PluginInterface.GetIpcSubscriber<int, int, object>("Penumbra.RedrawObject.V5");
        ApiVersion = Plugin.PluginInterface.GetIpcSubscriber<int>("Penumbra.ApiVersion");
        SetCollection = Plugin.PluginInterface.GetIpcSubscriber<byte, Guid?, bool, bool, (int, (Guid Id, string Name)?)>("Penumbra.SetCollection");
    }
}