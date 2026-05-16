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

    private bool RequiresUpdate()
    {
        return queuedCharacter != null ||  queuedDesignIndex != -1;
    }

    public void QueueUpdate(Character character, int designIndex = -1)
    {
        queuedCharacter = character;
        queuedDesignIndex = designIndex;
    }
    
    //apply queued updates to PlayerCharacter and return it to be
    public PlayerCharacter ApplyUpdate()
    {
        
    }
    
}