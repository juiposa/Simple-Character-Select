using System;
using System.Collections.Generic;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin
{
    public class CharacterDesign
    {
        public string Name { get; set; } = "Default";
        public string PenumbraCollection { get; set; } = "";
        public string GlamourerDesign { get; set; } = "";
        public string GlamourerAutomation { get; set; } = "";
        public (Guid, string) CustomizeProfileTuple { get; set; }
        public Honorific Honorific { get; set; } = new Honorific();
        public (Guid, string) MoodlePresetTuple { get; set; }
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

        public CharacterDesign Clone()
        {
            var newDesign = new CharacterDesign();
            newDesign.Name = Name;
            newDesign.PenumbraCollection = PenumbraCollection;
            newDesign.GlamourerDesign = GlamourerDesign;
            newDesign.GlamourerAutomation = GlamourerAutomation;
            newDesign.Honorific = Honorific.Clone();
            newDesign.AssignedGearset =  AssignedGearset;
            newDesign.MoodlePresetTuple = MoodlePresetTuple;
            newDesign.DateAdded = DateAdded.AddDays(0);
            newDesign.IsFavorite = IsFavorite;
            newDesign.PreviewImagePath = PreviewImagePath;
            newDesign.Tag = Tag;
            newDesign.DesignTags = DesignTags;
            newDesign.FolderId = FolderId;
            newDesign.KnownTags = KnownTags;
            newDesign.Id = Id;
            newDesign.SortOrder = SortOrder;
            return newDesign;
        }
    }
}
