using System;
using System.Collections.Generic;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin;


// tracking the state of a player character
public class PlayerCharacter
{
    internal Character? activeCharacter = null!;
    private string lastExecutedGearsetCommand = "";
    private DateTime lastGearsetCommandTime = DateTime.MinValue;
    private readonly Dictionary<string, (string characterName, string designName, DateTime time)> lastAppliedByJob = new();
    private readonly Dictionary<string, (string designName, DateTime time)> lastRandomDesignApplied = new();
    private readonly Dictionary<string, DateTime> lastDesignMacroExecuted = new();
    private bool randomDesignCRAppliedThisSession = false;
    internal byte lastSeenIdlePose = 255;
    internal int suppressIdleSaveForFrames = 0;
    internal byte lastSeenSitPose = 255;
    internal byte lastSeenGroundSitPose = 255;
    internal byte lastSeenDozePose = 255;
    internal int suppressSitSaveForFrames = 0;
    internal int suppressGroundSitSaveForFrames = 0;
    internal int suppressDozeSaveForFrames = 0;
}