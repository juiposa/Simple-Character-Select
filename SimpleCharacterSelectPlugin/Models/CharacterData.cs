using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;

namespace SimpleCharacterSelectPlugin.Models;

public class CharacterData
{
    public string Name { get; set; } = "";

    /// <summary>Optional alias used for Name Sync. If empty, uses Name.</summary>
    public string Alias { get; set; } = "";

    /// <summary>When true, this character's name won't be shared via Name Sync regardless of global setting.</summary>
    public bool ExcludeFromNameSync { get; set; } = false;

    public string Macros { get; set; } = ""; 
    public string? ImagePath { get; set; }
    public List<CharacterDesign> Designs { get; set; } = new List<CharacterDesign>();
    public Vector3 NameplateColor { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
    public string PenumbraCollection { get; set; } = "";
    public string GlamourerDesign { get; set; } = "";
    public string CustomizeProfile { get; set; } = "";
    public bool IsFavorite { get; set; } = false; 
    public DateTime DateAdded { get; set; } = DateTime.Now;
    public int SortOrder { get; set; } = 0;
    public Honorific Honorific { get; set; } = new Honorific();
    public string MoodlePreset { get; set; } = "";
    public byte IdlePoseIndex { get; set; } = 7;
    public byte SitPoseIndex { get; set; } = 255;
    public byte GroundSitPoseIndex { get; set; } = 255;
    public byte DozePoseIndex { get; set; } = 255;
    public string? Pronouns { get; set; }
    public string? LastInGameName { get; set; }
    public List<string> Tags { get; set; } = new();
    [JsonIgnore]
    public string Tag
    {
        get => Tags.FirstOrDefault() ?? "";
        set
        {
            Tags.Clear();
            if (!string.IsNullOrWhiteSpace(value))
                Tags.Add(value);
        }
    }
    public List<string> KnownTags { get; set; } = new();
    public List<string> DesignTags { get; set; } = new List<string>();
    public string CharacterAutomation { get; set; } = "";

    /// <summary>Gearset to switch to when applying this character (null = don't switch).</summary>
    public int? AssignedGearset { get; set; } = null;

    public List<DesignFolder> DesignFolders { get; set; } = new();
    public Vector3? OverrideAccentColor { get; set; } 
    public string? BackgroundImage { get; set; }
    
    public CharacterData Clone()
    {
        CharacterData clone = new CharacterData();
        
        clone.Alias = this.Alias;
        clone.Name = this.Name;
        clone.ExcludeFromNameSync = this.ExcludeFromNameSync;
        clone.Macros = this.Macros;
        clone.ImagePath = this.ImagePath;
        clone.Designs = this.Designs.Slice(0, this.Designs.Count);
        clone.NameplateColor = this.NameplateColor.AsVector4().AsVector3();
        clone.PenumbraCollection = this.PenumbraCollection;
        clone.GlamourerDesign = this.GlamourerDesign;
        clone.CustomizeProfile = this.CustomizeProfile;
        clone.IsFavorite = this.IsFavorite;
        clone.DateAdded = this.DateAdded.AddSeconds(0);
        clone.SortOrder = this.SortOrder;
        clone.Honorific =  this.Honorific;
        clone.MoodlePreset = this.MoodlePreset;
        clone.IdlePoseIndex = this.IdlePoseIndex;
        clone.SitPoseIndex = this.SitPoseIndex;
        clone.GroundSitPoseIndex = this.GroundSitPoseIndex;
        clone.DozePoseIndex = this.DozePoseIndex;
        clone.Pronouns = this.Pronouns;
        clone.Tags = this.Tags.Slice(0, this.Tags.Count);
        clone.KnownTags = this.KnownTags.Slice(0, this.KnownTags.Count);
        clone.DesignTags = this.DesignTags.Slice(0, this.DesignTags.Count);
        clone.CharacterAutomation = this.CharacterAutomation;
        clone.AssignedGearset = this.AssignedGearset;
        clone.DesignFolders = this.DesignFolders.Slice(0, this.DesignFolders.Count);
        clone.OverrideAccentColor = this.OverrideAccentColor.HasValue ? this.OverrideAccentColor.Value.AsVector4().AsVector3() : default;
        clone.BackgroundImage = this.BackgroundImage;
        
        return clone;
    }
}