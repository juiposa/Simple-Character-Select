using System;
using System.Collections.Generic;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin
{
    public class CharacterDesign
    {
        public string Name { get; set; }
        public string PenumbraCollection { get; set; } = "";
        public string GlamourerDesign { get; set; } = "";
        public string GlamourerAutomation { get; set; } = "";
        public string CustomizeProfile { get; set; } = "";
        public Honorific Honorific { get; set; } = new Honorific();
        public string MoodlePreset { get; set; } = "";
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        public bool IsFavorite { get; set; }
        public string? PreviewImagePath { get; set; } = null;
        public string Tag { get; set; } = "Unsorted";
        public List<string> KnownTags { get; set; } = new();
        public List<string> DesignTags { get; set; } = new List<string>();
        public Guid? FolderId { get; set; } = null; 
        public Guid Id { get; set; } = Guid.NewGuid();
        public int SortOrder { get; set; } = 0;
        
        /// <summary>
        /// Per-design mod option settings.
        /// Format: ModDirectory -> GroupName -> SelectedOptionNames
        /// </summary>
        public Dictionary<string, Dictionary<string, List<string>>>? ModOptionSettings { get; set; }

        /// <summary>Gearset to switch to when applying this design (null = use character's setting or don't switch).</summary>
        public int? AssignedGearset { get; set; } = null;
    }
}
