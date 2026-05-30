using System;

namespace SimpleCharacterSelectPlugin.Models;

public class GearsetAssignment
{
    public int GearsetIndex = -1;
    public string GearsetDisplay = "";
    public string CharacterName = "";
    public Guid? DesignId = null;
    public string DesignName = "";

    public bool IsValid()
    {
        return GearsetIndex >= 0 && !string.IsNullOrWhiteSpace(CharacterName) && DesignId != null;
    }

    public string DisplayName()
    {
        return $"{GearsetDisplay} / {CharacterName} ({DesignName})";
    }
}