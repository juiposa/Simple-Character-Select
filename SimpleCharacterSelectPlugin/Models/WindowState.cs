using System.Numerics;

namespace SimpleCharacterSelectPlugin.Models;

public class WindowState
{
            public bool IsAddCharacterWindowOpen { get; set; } = false;
        // Settings Variables
        public bool IsSettingsOpen { get; set; } = false;  // Tracks if settings panel is open
        public float ProfileImageScale { get; set; } = 1.0f;  // Image scaling (1.0 = normal size)
        public int ProfileColumns { get; set; } = 3;  // Number of profiles per row
        public float ProfileSpacing { get; set; } = 2.0f; // Default spacing

        public Vector2? MainWindowPos { get; set; }
        public Vector2? MainWindowSize { get; set; }
        public Vector2? AddCharacterButtonPos { get; set; }
        public Vector2? AddCharacterButtonSize { get; set; }
        public Vector2? CharacterNameFieldPos { get; set; }
        public Vector2? CharacterNameFieldSize { get; set; }
        public Vector2? PenumbraFieldPos { get; set; }
        public Vector2? PenumbraFieldSize { get; set; }
        public Vector2? GlamourerFieldPos { get; set; }
        public Vector2? GlamourerFieldSize { get; set; }
        public Vector2? SaveButtonPos { get; set; }
        public Vector2? SaveButtonSize { get; set; }
        public Vector2? FirstCharacterDesignsButtonPos { get; set; }
        public Vector2? FirstCharacterDesignsButtonSize { get; set; }
        public Vector2? DesignPanelAddButtonPos { get; set; }
        public Vector2? DesignPanelAddButtonSize { get; set; }
        public Vector2? DesignNameFieldPos { get; set; }
        public Vector2? DesignNameFieldSize { get; set; }
        public Vector2? DesignGlamourerFieldPos { get; set; }
        public Vector2? DesignGlamourerFieldSize { get; set; }
        public Vector2? SaveDesignButtonPos { get; set; }
        public Vector2? SaveDesignButtonSize { get; set; }
        public bool IsDesignPanelOpen { get; set; } = false;
        public bool IsEditDesignWindowOpen { get; set; } = false;
        public string EditedDesignName { get; set; } = "";
        public string EditedGlamourerDesign { get; set; } = "";



        public Vector2? SettingsButtonPos { get; set; }
        public Vector2? SettingsButtonSize { get; set; }
        public Vector2? QuickSwitchButtonPos { get; set; }
        public Vector2? QuickSwitchButtonSize { get; set; }
        public Vector2? GalleryButtonPos { get; set; }
        public Vector2? GalleryButtonSize { get; set; }
}