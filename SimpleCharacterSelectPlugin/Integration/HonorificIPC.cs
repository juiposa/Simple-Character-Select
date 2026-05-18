using Dalamud.Plugin.Ipc;

namespace SimpleCharacterSelectPlugin.Integration;

public static class HonorificIpc
{
    public static readonly ICallGateSubscriber<string, uint, object[]> GetCharacterTitleList;
    public static readonly ICallGateSubscriber<int, string, object> SetCharacterTitle;
    
    static HonorificIpc()
    {
        GetCharacterTitleList = Plugin.PluginInterface.GetIpcSubscriber<string, uint, object[]>("Honorific.GetCharacterTitleList");
        SetCharacterTitle = Plugin.PluginInterface.GetIpcSubscriber<int, string, object>("Honorific.SetCharacterTitle");
    }
}