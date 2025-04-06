using System.Numerics;
using System.Text;
using log4net;
using PigNet;
using PigNet.Entities;
using PigNet.Items;
using PigNet.Net.Packets.Mcpe;
using PigNet.Utils.Skins;
using PigNet.Worlds;

namespace PigNPC.Npc;

public class CustomNpc : PlayerMob
{
    public new NpcData Data { get; }
    
    public CustomNpc(NpcData data, string name, Level level) : base(name, level)
    {
        Data = data;
        IsSpawned = false;
        
        var resourcePatch = new SkinResourcePatch() { Geometry = new GeometryIdentifier() {Default = "geometry.humanoid.customSlim" } };
        Skin = new Skin
        {
            SkinId = $"{Guid.NewGuid().ToString()}.CustomSlim",
            SkinResourcePatch = resourcePatch,
            Slim = true,
            Height = 32,
            Width = 64,
            Data = Encoding.Default.GetBytes(new string('Z', 8192)),
        };

        ItemInHand = new ItemAir();
        Scale = data.Scale;

        HideNameTag = data.IsVisible;
        IsAlwaysShowName = data.IsAlwaysShowName;

        HealthManager = new HealthManager(this);
        IsInWater = true;
        NoAi = true;
        HealthManager.IsOnFire = false;
        Velocity = Vector3.Zero;
        PositionOffset = 1.62f;
        if (EntityId == -1)
        {
            EntityId = DateTime.UtcNow.Ticks;
        }

        NameTag = data.DisplayName;
    }

    public override void SendSkin(Player[]? players = null)
    {
        if (Data.SkinType == PigNpcLoader.SkinType.PersistantSkin)
        {
            LogManager.GetLogger(GetType()).Warn($"PersistantSkin detected for {NameTag}");
            base.SendSkin(players);
        }
        else
        {
            LogManager.GetLogger(GetType()).Warn($"PlayerSkin detected for {NameTag}");
            Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(_ =>
            {
                foreach (var player in Level.GetAllPlayers())
                {
                    BroadcastPlayerSkin(player);
                }
            });
        }
    }

    private void BroadcastPlayerSkin(Player player)
    {
        var playerSkin = McpePlayerSkin.CreateObject();
        playerSkin.uuid = ClientUuid;
        playerSkin.skin = player.Skin;
        playerSkin.oldSkinName = "";
        playerSkin.skinName = "";
        playerSkin.isVerified = true;
                
        player.SendPacket(playerSkin);
    }

    public void Spawn()
    {
        SpawnEntity();
        SendSkin();
    }
}