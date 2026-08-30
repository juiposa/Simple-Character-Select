using System.Numerics;
using System.Text.Json;

namespace SimpleCharacterSelectPlugin.Models;

public class Honorific
{
    public string Title { get; set; } = "";
    public string Location { get; set; } = "prefix";
    public Vector3 Color { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
    public Vector3 Glow { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);

    public Honorific Clone()
    {
        Honorific clone = new Honorific();
        clone.Title = this.Title;
        clone.Location = this.Location;
        clone.Color = this.Color.AsVector4().AsVector3();
        clone.Glow = this.Glow.AsVector4().AsVector3();
        return clone;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
}