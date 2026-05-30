using System;
using System.Transactions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using SimpleCharacterSelectPlugin.Managers;

namespace SimpleCharacterSelectPlugin.Models;

// Currently logged into PlayerCharacter, for keeping session state
public class ActivePlayerCharacter
{
    public IPlayerCharacter InGameCharacter { get; }
    public PlayerCharacter Pc { get; set; }
    
    private Character? queuedCharacter = null;
    private Guid? queuedDesignId = null;

    public Gearset? LastKnownGearset = null;
    
    public ActivePlayerCharacter(IPlayerCharacter ingamePc, PlayerCharacter playerCharacter)
    {
        InGameCharacter = ingamePc;
        Pc = playerCharacter;
    }

    public bool RequiresUpdate()
    {
        return queuedCharacter != null ||  queuedDesignId != null;
    }

    public bool GearsetHasChanged()
    {
        var currentGearset = GearsetManager.GetCurrentGearset();
        if (LastKnownGearset == null) //init
        {
            LastKnownGearset = currentGearset;
            return false;
        } 
        if (LastKnownGearset.Index == currentGearset.Index) // no change
        {
            return false;
        }
        LastKnownGearset = currentGearset;
        return true;
    }

    public void QueueUpdate(Character character, Guid? designId = null)
    {
        Plugin.Log.Debug($"Update queued: {character.Data.Name} {designId}");
        queuedCharacter = character;
        queuedDesignId = designId ?? character.Data.DefaultDesignId;
    }
    
    //apply queued updates
    public void ApplyUpdate()
    {
        if (queuedCharacter != null)
        {
            Pc.ActiveCharacter = queuedCharacter;
            queuedCharacter = null;
        }

        if (queuedDesignId != null)
        {
            Pc.ActiveDesignId = queuedDesignId.Value;
            queuedDesignId = null;
        }
    }
}