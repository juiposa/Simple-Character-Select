using System;
using System.Linq;
using System.Numerics;
using SimpleCharacterSelectPlugin.Managers;

namespace SimpleCharacterSelectPlugin.Models
{
    [Serializable]
    public class Character
    {
        public CharacterData Data { get; set; } = new();

        public void Save(CharacterData? newData)
        {
            if (newData == null)
            {
                Data = new CharacterData();
                return;
            }
            Data = newData.Clone();
        }

        public CharacterDesign GetDefaultDesign()
        {
            return Data.Designs.ElementAtOrDefault(Data.DefaultDesignIndex) ?? new CharacterDesign();
        }

        public CharacterDesign GetDesign(int designIndex)
        {
            if (designIndex < 0)
            {
                return GetDefaultDesign();
            }

            if (designIndex + 1 > Data.Designs.Count)
            {
                Plugin.Log.Warning("CharacterDesign not found, using default design");
                return GetDefaultDesign();
            }
            CharacterDesign design = Data.Designs[designIndex];
            return design;
        }

        public void SetDefaultDesign(CharacterDesign characterDesign)
        {
            Data.Designs[Data.DefaultDesignIndex] = characterDesign;
        }

        public int GetDesignIndex(CharacterDesign characterDesign)
        {
            return Data.Designs.IndexOf(characterDesign);
        }
    }
    public class DesignFolder
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
        public Vector3? CustomColor { get; set; } = null;

        public Guid? ParentFolderId { get; set; } = null;
        public int SortOrder { get; set; } = 0;

        
        public DesignFolder()
        {
            Name = "";
            Id = Guid.NewGuid();
            ParentFolderId = null;
            SortOrder = 0;
        }

        public DesignFolder(string name)
        {
            Name = name;
            Id = Guid.NewGuid();
        }

        public DesignFolder(string name, Guid id)
        {
            Name = name;
            Id = id;
        }

        public DesignFolder(DesignFolder other)
        {
            Name = other.Name;
            Id = other.Id;
        }
    }
}
