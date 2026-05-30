namespace SimpleCharacterSelectPlugin.Managers;

public static class MigrationManager
{

    public static void RunMigrations(Plugin plugin)
    {
        MigrateDefaultDesigns(plugin);
    }


    private static void MigrateDefaultDesigns(Plugin plugin)
    {
        foreach (var character in plugin.Characters)
        {
            var defaultDesign = character.GetDesign(character.Data.DefaultDesignIndex);
            character.Data.DefaultDesignId = defaultDesign.Id;
        }
    }
    
}