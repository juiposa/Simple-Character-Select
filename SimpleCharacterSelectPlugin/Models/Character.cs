using System;
using System.Numerics;
using SimpleCharacterSelectPlugin.Managers;

namespace SimpleCharacterSelectPlugin.Models
{
    [Serializable]
    public class Character
    {
        public CharacterData Data { get; set; }
        private CharacterData savedData;

        public Character()
        {
            Data = new CharacterData();
            savedData = new CharacterData();
        }
        
        public void Save()
        {
            savedData = Data.Clone();
        }

        public void LoadFromConfig(string json)
        {
            
        }
        
        public void ResetEdits()
        {
            Data = savedData.Clone(); //TODO doesn't work as intended
        }

        public CharacterDesign GetDefaultDesign()
        {
            return Data.Designs[Data.DefaultDesignIndex];
        }

        public void SetDefaultDesign(CharacterDesign characterDesign)
        {
            Data.Designs[Data.DefaultDesignIndex] = characterDesign;
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
