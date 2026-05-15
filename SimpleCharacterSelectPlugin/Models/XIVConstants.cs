namespace SimpleCharacterSelectPlugin.Models;

public static class XivConstants
{
    public static readonly string[] Worlds = new[] { "Excalibur" }; //TODO
    
    private static readonly string[] RoleNames = new[] { "Tank", "Healer", "Melee", "Ranged", "Caster", "Crafter", "Gatherer" };
    
    public static readonly Job[] Jobs = new[]
    {
        // Tanks
        new Job(19u, "Paladin", "PLD", "Tank"), new Job(21u, "Warrior", "WAR", "Tank"), new Job(32u, "Dark Knight", "DRK", "Tank"), new Job(37u, "Gunbreaker", "GNB", "Tank"),
        // Healers
        new Job(24u, "White Mage", "WHM", "Healer"), new Job(28u, "Scholar", "SCH", "Healer"), new Job(33u, "Astrologian", "AST", "Healer"), new Job(40u, "Sage", "SGE", "Healer"),
        // Melee DPS
        new Job(20u, "Monk", "MNK", "Melee"), new Job(22u, "Dragoon", "DRG", "Melee"), new Job(30u, "Ninja", "NIN", "Melee"), 
        new Job(34u, "Samurai", "SAM", "Melee"), new Job(39u, "Reaper", "RPR", "Melee"), new Job(41u, "Viper", "VPR", "Melee"),
        new Job(43u, "Beastmaster", "BST", "Melee"),
        // Ranged Physical DPS
        new Job(23u, "Bard", "BRD", "Ranged"), new Job(31u, "Machinist", "MCH", "Ranged"), new Job(38u, "Dancer", "DNC", "Ranged"),
        // Caster DPS
        new Job(25u, "Black Mage", "BLM", "Caster"), new Job(27u, "Summoner", "SMN", "Caster"), new Job(35u, "Red Mage", "RDM", "Caster"), new Job(36u, "Blue Mage", "BLU", "Caster"), new Job(42u, "Pictomancer", "PCT", "Caster"),
        // Crafters
        new Job(8u, "Carpenter", "CRP", "Crafter"), new Job(9u, "Blacksmith", "BSM", "Crafter"), new Job(10u, "Armorer", "ARM", "Crafter"), new Job(11u, "Goldsmith", "GSM", "Crafter"),
        new Job(12u, "Leatherworker", "LTW", "Crafter"), new Job(13u, "Weaver", "WVR", "Crafter"), new Job(14u, "Alchemist", "ALC", "Crafter"), new Job(15u, "Culinarian", "CUL", "Crafter"),
        // Gatherers
        new Job(16u, "Miner", "MIN", "Gatherer"), new Job(17u, "Botanist", "BTN", "Gatherer"), new Job(18u, "Fisher", "FSH", "Gatherer")
    };

    public class Job
    {
        uint Id;
        string Name;
        string Code;
        string Role;
        
        public Job(uint id, string name, string code, string role)
        {
            Id = id;
            Name = name;
            Code = code;
            Role = role;
        }
    }
}