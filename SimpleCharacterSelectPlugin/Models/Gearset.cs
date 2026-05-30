namespace SimpleCharacterSelectPlugin.Models;

public class Gearset
{
    public uint Index = 0;
    public string Name = "";
    public uint Job = 0;

    public string DisplayName()
    {
        return $"#{Index + 1} ({XivConstants.GetJobCode(Job)}) - {Name}";
    }
}