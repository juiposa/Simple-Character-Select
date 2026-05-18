using System.Numerics;
using System.Text.Json;

namespace SimpleCharacterSelectPlugin.Models;

public class Honorific
{
    public string Title { get; set; } = "";
    public string Prefix { get; set; } = "";
    public string Suffix { get; set; } = "";
    public bool IsPrefix => Prefix != "" && Suffix == "";
    public Vector3 Color { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
    public Vector3 Glow { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
    public Vector3? Color3 { get; set; } = null;  // Second colour for two-colour gradient
    public int? GradientSet { get; set; } = null;  // -1 = Two Colour Gradient
    public string? AnimationStyle { get; set; } = null;

    public Honorific Clone()
    {
        Honorific clone = new Honorific();
        clone.Title = this.Title;
        clone.Prefix = this.Prefix;
        clone.Suffix = this.Suffix;
        clone.Color = this.Color.AsVector4().AsVector3();
        clone.Glow = this.Glow.AsVector4().AsVector3();
        clone.Color3 = this.Color3.HasValue ? this.Color3.Value.AsVector4().AsVector3() : default;
        clone.GradientSet = this.GradientSet;
        clone.AnimationStyle = this.AnimationStyle;
        return clone;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
}