using Dalamud.Game.ClientState.Objects.SubKinds;

namespace SimpleCharacterSelectPlugin.Models;

// tracking the state of a player character
public class PlayerCharacter
{
    public IPlayerCharacter? InGameCharacter { get; set; }
    public string Name { get; set; } = "";
    public string World { get; set; } = "";
    public string FullName => $"{Name}@{World}";
    public Character? AssignedCharacter { get; set; } = null;
    public Character? ActiveCharacter = null!;
    public int ActiveDesign { get; set; } = 0;
    // internal byte lastSeenIdlePose = 255;
    // internal byte lastSeenSitPose = 255;
    // internal byte lastSeenGroundSitPose = 255;
    // internal byte lastSeenDozePose = 255;
    
    public PlayerCharacter(IPlayerCharacter? ingame, string name, string world)
    {
        InGameCharacter = ingame;
        Name = name;
        World = world; 
    }
}