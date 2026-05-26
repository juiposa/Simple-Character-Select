using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.Sheets;

namespace SimpleCharacterSelectPlugin.Models;

public static class XivConstants
{
    public static string GetJobName(uint jobId)
    {
        return Plugin.DataManager.Excel.GetSheet<ClassJob>()[jobId].Name.ExtractText();
    }

    public static string GetJobCode(uint jobId)
    {
        return Plugin.DataManager.Excel.GetSheet<ClassJob>()[jobId].Abbreviation.ExtractText();
    }
}