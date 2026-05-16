using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;

namespace SimpleCharacterSelectPlugin.Models;

public class CharacterData
{
    public string Name { get; set; } = "";
    public string? ImagePath { get; set; }
    public List<CharacterDesign> Designs { get; set; } = new List<CharacterDesign>();
    public int DefaultDesignIndex { get; set; } = 0;
    public Vector3 NameplateColor { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
    public bool IsFavorite { get; set; } = false;
    
    // External plugins

    
    public DateTime DateAdded { get; set; } = DateTime.Now;
    public int SortOrder { get; set; } = 0;
    public byte IdlePoseIndex { get; set; } = 7;
    public byte SitPoseIndex { get; set; } = 255;
    public byte GroundSitPoseIndex { get; set; } = 255;
    public byte DozePoseIndex { get; set; } = 255;
    public string? Pronouns { get; set; }
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

    /// <summary>Gearset to switch to when applying this character (null = don't switch).</summary>
    public int? AssignedGearset { get; set; } = null;

    public List<DesignFolder> DesignFolders { get; set; } = new();
    public Vector3? OverrideAccentColor { get; set; } 
    public string? BackgroundImage { get; set; }
    
    public CharacterData Clone()
    {
        CharacterData clone = new CharacterData();
        
        clone.Name = this.Name;
        clone.ImagePath = this.ImagePath;
        clone.Designs = this.Designs.Slice(0, this.Designs.Count);
        clone.DefaultDesignIndex = this.DefaultDesignIndex;
        clone.NameplateColor = this.NameplateColor.AsVector4().AsVector3();
        clone.IsFavorite = this.IsFavorite;
        clone.DateAdded = this.DateAdded.AddSeconds(0);
        clone.SortOrder = this.SortOrder;
        clone.IdlePoseIndex = this.IdlePoseIndex;
        clone.SitPoseIndex = this.SitPoseIndex;
        clone.GroundSitPoseIndex = this.GroundSitPoseIndex;
        clone.DozePoseIndex = this.DozePoseIndex;
        clone.Pronouns = this.Pronouns;
        clone.Tags = this.Tags.Slice(0, this.Tags.Count);
        clone.KnownTags = this.KnownTags.Slice(0, this.KnownTags.Count);
        clone.DesignTags = this.DesignTags.Slice(0, this.DesignTags.Count);
        clone.AssignedGearset = this.AssignedGearset;
        clone.DesignFolders = this.DesignFolders.Slice(0, this.DesignFolders.Count);
        clone.OverrideAccentColor = this.OverrideAccentColor.HasValue ? this.OverrideAccentColor.Value.AsVector4().AsVector3() : default;
        clone.BackgroundImage = this.BackgroundImage;
        
        return clone;
    }
}