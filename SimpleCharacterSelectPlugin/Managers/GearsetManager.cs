using System;
using System.Collections.Generic;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin.Managers;

public static class GearsetManager
{
    public static Gearset GetCurrentGearset()
    {
        return GetGearset(GetCurrentGearsetIndex());
    }

    public static Gearset GetGearset(int index)
    {
        return GetPlayerGearsets()[index];
    }

    private static unsafe int GetCurrentGearsetIndex()
    {
        return (int)UIModule.Instance()->GetRaptureGearsetModule()->CurrentGearsetIndex;
    }

    private static Gearset MakeGearset(RaptureGearsetModule.GearsetEntry gearset)
    {
        return new Gearset
        {
            Index = gearset.Id,
            Name = Encoding.UTF8.GetString(TrimEndBytes(gearset.Name.ToArray())),
            Job = gearset.ClassJob
        };
    }
    
    public static unsafe List<Gearset> GetPlayerGearsets()
    {
        var gearsets = UIModule.Instance()->GetRaptureGearsetModule()->Entries;
        var returnList = new List<Gearset>();
        foreach (var gearset in gearsets)
        {
            returnList.Add(MakeGearset(gearset));
        }
        return returnList;
    }

    private static byte[] TrimEndBytes(byte[] bytes)
    {
        var firstNullByte = Array.FindIndex(bytes, b => b == 0x00);
        if (firstNullByte != -1)
        {
            return bytes[..firstNullByte];
        }
        var splitIndex = Array.FindIndex(bytes, b => b == 0x29);
        if (splitIndex != -1)
        {
            return bytes[..splitIndex];
        }

        return bytes;
    }
    
    public static unsafe void TryApplyGearset(Gearset gearset)
    {
        Plugin.Log.Debug($"Switching to gearset {gearset.DisplayName()}");
        UIModule.Instance()->GetRaptureGearsetModule()->EquipGearset((int)gearset.Index);
    }
}