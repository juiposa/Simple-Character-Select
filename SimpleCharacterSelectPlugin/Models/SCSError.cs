namespace SimpleCharacterSelectPlugin;

public class SCSError
{   
    public string Message { get; set; }
    public SCSError(string message)
    {
        Message = message;
    }
}