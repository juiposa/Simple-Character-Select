using System.Transactions;

namespace SimpleCharacterSelectPlugin.Models;

// Currently logged into PlayerCharacter, for keeping session state
public class ActivePlayerCharacter
{
    public PlayerCharacter Pc { get; set; }
    
    private Character? queuedCharacter = null;
    private int queuedDesignIndex = -1;
    
    public ActivePlayerCharacter(PlayerCharacter playerCharacter)
    {
        Pc = playerCharacter;
    }

    public bool RequiresUpdate()
    {
        return queuedCharacter != null ||  queuedDesignIndex != -1;
    }

    public void QueueUpdate(Character character, int designIndex = -1)
    {
        queuedCharacter = character;
        queuedDesignIndex = designIndex >= 0 ? designIndex : character.Data.DefaultDesignIndex;
    }
    
    //apply queued updates to PlayerCharacter and return it to be
    public PlayerCharacter ApplyUpdate()
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
        return Pc;
    }
}