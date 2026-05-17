using System.Transactions;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace SimpleCharacterSelectPlugin.Models;

// Currently logged into PlayerCharacter, for keeping session state
public class ActivePlayerCharacter
{
    public IPlayerCharacter InGameCharacter { get; }
    public PlayerCharacter Pc { get; set; }
    
    private Character? queuedCharacter = null;
    private int queuedDesignIndex = -1;
    
    public ActivePlayerCharacter(IPlayerCharacter ingamePc, PlayerCharacter playerCharacter)
    {
        InGameCharacter = ingamePc;
        Pc = playerCharacter;
    }

    public bool RequiresUpdate()
    {
        return queuedCharacter != null ||  queuedDesignIndex != -1;
    }

    public void QueueUpdate(Character character, int designIndex = -1)
    {
        Plugin.Log.Debug($"Update queued: {character.Data.Name} {designIndex}");
        queuedCharacter = character;
        queuedDesignIndex = designIndex >= 0 ? designIndex : character.Data.DefaultDesignIndex;
    }
    
    //apply queued updates
    public void ApplyUpdate()
    {
        if (queuedCharacter != null)
        {
            Pc.ActiveCharacter = queuedCharacter;
            queuedCharacter = null;
        }

        if (queuedDesignIndex != -1)
        {
            Pc.ActiveDesign =  queuedDesignIndex;
            queuedDesignIndex = -1;
        }
    }
}