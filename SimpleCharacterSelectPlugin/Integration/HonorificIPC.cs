using Dalamud.Plugin.Ipc;

namespace SimpleCharacterSelectPlugin.Integration;

public static class HonorificIpc
{
    public static readonly ICallGateSubscriber<string, uint, object[]> GetCharacterTitleList;
    
    static HonorificIpc()
    {
        GetCharacterTitleList = Plugin.PluginInterface.GetIpcSubscriber<string, uint, object[]>("Honorific.GetCharacterTitleList");
    }
}