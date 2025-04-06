using PigNet.Utils.Skins;

namespace PigNPC.Npc;

public class NpcData
{
    public string Id { get; set; } = null!;
    public string NameTag { get; set; } = null!;
    public string LevelName { get; set; } = null!;
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public float Pitch { get; set; }
    public float Yaw { get; set; }
    public float HeadYaw { get; set; }
    public float Scale { get; set; } = 1;

    public string GeometryJsonName { get; set; } = string.Empty;

    public Skin? Skin { get; set; }

    public bool IsVisible { get; set; }
    public bool IsAlwaysShowName { get; set; }
    
    public string? ActionId { get; set; }
    
    public PigNpcLoader.SkinType SkinType { get; set; }
    public string DisplayName { get; set; }
}