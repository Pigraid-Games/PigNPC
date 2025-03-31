using System.Reflection;
using System.Text;
using log4net;
using PigNet;
using PigNet.Entities;
using PigNet.Plugins.Attributes;
using PigNet.Utils.Skins;
using PigNPC.Database;
using PigNPC.Npc;

namespace PigNPC;

public class Commands
{

    [Command(Name = "npc help", Description = "Show the help menu")]
    [Authorize(Permission = 4)]
    public void NpcHelp(Player commander)
    {
        var sb = new StringBuilder();
        sb.AppendLine("§e§lPigNPC Help Menu");
        sb.AppendLine("§7Here are the available NPC commands:");
        sb.AppendLine();
        sb.AppendLine("§a/npc help §7- Show this help menu");
        sb.AppendLine("§a/npc list §7- List all the NPCs currently loaded");
        sb.AppendLine("§a/npc create <Name> <DisplayName> <SkinName>  §7- Create an NPC with specific skin/geometry");
        sb.AppendLine("§a/npc create <Name> <DisplayName> <PlayerSkin|PeristantSkin> §7- Create an NPC using your current skin");
        sb.AppendLine("§a/npc remove <Name> §7- Remove an NPC from the world and database");
        sb.AppendLine("§a/npc goto <Name> §7- Teleport yourself to the specified NPC");
        sb.AppendLine("§a/npc tp <Name> §7- Teleport the specified NPC to your location");
        sb.AppendLine("§a/npc setskin <Name> §7- Set the skin of an NPC to your current skin");
        sb.AppendLine("§a/npc setskin <Name> <SkinName> §7- Set the NPC skin using .png/.json files from npc_skins folder");
        sb.AppendLine("§a/npc reload §7- Reload all NPCs from the database");
        sb.AppendLine("§a/npc bind <Name> <ActionName> §7- Bind an action to an NPC");
        sb.AppendLine("§a/npc info §7- Show info about the plugin");

        commander.SendMessage(sb.ToString().TrimEnd());
    }

    [Command(Name = "npc bind", Description = "Bind an action to an NPC")]
    [Authorize(Permission = 4)]
    public async void NpcBind(Player commander, string name, string actionName)
    {
        var npc = PigNpcLoader.GetAll()
            .FirstOrDefault(n => n.Data.NameTag.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        if (npc == null)
        {
            commander.SendMessage($"§cNo NPC found with name '{name}'");
            return;
        }

        if (!NpcActionRegistry.Actions.TryGetValue(actionName, out _))
        {
            commander.SendMessage($"§cNo Action with that name '{name}, please register the action first with your plugins");
            return;
        }
        
        npc.Data.ActionId = actionName;
        await PigNpcLoader.SaveAllAsync();
        
        commander.SendMessage($"§aAction '{actionName}' bound successfully to the npc '{name}");
    }

    [Command(Name = "npc list", Description = "List all the npcs in the world")]
    [Authorize(Permission = 4)]
    public void NpcList(Player commander)
    {
        var npcs = PigNpcLoader.GetAll().ToList();

        if (npcs.Count == 0)
        {
            commander.SendMessage("§cThere are no NPCs currently loaded");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"§aNPCs currently loaded: §7({npcs.Count})");

        foreach (var npc in npcs)
        {
            sb.AppendLine($"§f• §e{npc.Data.NameTag} §7in level §b{npc.Data.LevelName}");
        }

        commander.SendMessage(sb.ToString().TrimEnd());
    }

    [Command(Name = "npc create", Description = "Create a npc and spawn it in the world - using your skin")]
    [Authorize(Permission = 4)]
    public void NpcCreate(Player commander, string name, string displayName, PigNpcLoader.SkinType skinType)
    {
        // Check for duplicates
        var exists = PigNpcLoader.GetAll()
            .Any(n => n.Data.NameTag.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        if (exists)
        {
            commander.SendMessage($"§cAn NPC with the name '{name}' already exists");
            return;
        }

        // Get commander position
        var pos = commander.KnownPosition;

        // Prepare skin
        Skin? skin = null;
        if (skinType == PigNpcLoader.SkinType.PersistantSkin)
        {
            skin = commander.Skin;
        }

        var npcData = new NpcData
        {
            Id = Guid.NewGuid().ToString(),
            NameTag = name,
            LevelName = commander.Level.LevelId,
            X = pos.X,
            Y = pos.Y,
            Z = pos.Z,
            Pitch = pos.Pitch,
            Yaw = pos.Yaw,
            HeadYaw = pos.HeadYaw,
            Skin = skin,
            SkinType = skinType,
            IsVisible = true,
            IsAlwaysShowName = false,
            ActionId = null,
            DisplayName = displayName
        };

        var npc = new CustomNpc(npcData, name, commander.Level)
        {
            KnownPosition = commander.KnownPosition,
            Skin = skin,
        };
        npc.Spawn();

        PigNpcLoader.Register(npc);
        commander.SendMessage($"§aNPC '{name}' has been created with {skinType}");
    }

    [Command(Name = "npc create", Description = "Create a npc and spawn it in the world")]
    [Authorize(Permission = 4)]
    public void NpcCreate(Player commander, string name, string displayName, string? skinName)
    {
        // Check if name is taken
        var exists = PigNpcLoader.GetAll()
            .Any(n => n.Data.NameTag.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        if (exists)
        {
            commander.SendMessage($"§cAn NPC with the name '{name}' already exists");
            return;
        }

        // Skin folder
        var folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", "npc_skins");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            commander.SendMessage(
                "§eSkin folder 'npc_skins' has been created. Place .png and optionally .json files inside");
            return;
        }

        var texturePath = Path.Combine(folder, $"{skinName}.png");
        var geometryPath = skinName != null ? Path.Combine(folder, $"{skinName}.json") : null;
        LogManager.GetLogger(GetType()).Warn($"Geometry file: {geometryPath}");

        if (!File.Exists(texturePath))
        {
            commander.SendMessage($"§cMissing texture file '{skinName}.png'");
            return;
        }

        var skinBytes = Skin.GetTextureFromFile(texturePath);
        if (skinBytes == null)
        {
            commander.SendMessage("§cInvalid skin texture. Must be 64x32, 64x64, or 128x128");
            return;
        }

        // Get player position
        var pos = commander.KnownPosition;

        var npcData = new NpcData
        {
            Id = Guid.NewGuid().ToString(),
            NameTag = name,
            LevelName = commander.Level.LevelId,
            X = pos.X,
            Y = pos.Y,
            Z = pos.Z,
            Pitch = pos.Pitch,
            Yaw = pos.Yaw,
            HeadYaw = pos.HeadYaw,
            IsVisible = true,
            IsAlwaysShowName = false,
            ActionId = null,
            SkinType = PigNpcLoader.SkinType.PersistantSkin
        };

        var npc = new CustomNpc(npcData, name, commander.Level)
        {
            KnownPosition = commander.KnownPosition,
            Skin = commander.Skin
        };

        if (File.Exists(geometryPath))
        {
            LogManager.GetLogger(GetType()).Warn("Geometry file found for skin, trying to apply it...");

            npc.Data.GeometryJsonName = skinName + ".json";

            var geometryJson = File.ReadAllText(geometryPath);
            geometryJson = geometryJson.Replace("geometry.unknown", $"geometry.{skinName}");
            var geometryModel = Skin.Parse(geometryJson);

            var fullGeometryName = $"geometry.{skinName}";

            npc = new CustomNpc(npcData, name, commander.Level)
            {
                KnownPosition = commander.KnownPosition,
                Skin =
                {
                    Data = skinBytes,
                    SkinResourcePatch = new SkinResourcePatch 
                        { Geometry = new GeometryIdentifier { Default = fullGeometryName } },
                    GeometryName = fullGeometryName,
                    GeometryData = Skin.ToJson(geometryModel),
                    IsVerified = true
                }
            };
        }

        // Spawn and register
        npc.Data.Skin = npc.Skin;
        npc.Data.DisplayName = displayName;
        npc.Spawn();
        PigNpcLoader.Register(npc);

        commander.SendMessage($"§aNPC '{name}' has been created and spawned");
    }

    [Command(Name = "npc remove", Description = "Remove a npc from the world and delete it from the database")]
    [Authorize(Permission = 4)]
    public void NpcRemove(Player commander, string name)
    {
        var npc = PigNpcLoader.GetAll()
            .FirstOrDefault(n => n.Data.NameTag.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        if (npc == null)
        {
            commander.SendMessage($"§cNo NPC found with name '{name}'.");
            return;
        }

        PigNpcLoader.Unregister(npc.Data.Id);
        commander.SendMessage($"§aNPC '{name}' has been removed and deleted from the database.");
    }

    [Command(Name = "npc goto", Description = "Teleport to an npc")]
    [Authorize(Permission = 4)]
    public void NpcGoto(Player commander, string name)
    {
        var npc = PigNpcLoader.GetAll()
            .FirstOrDefault(n => n.Data.NameTag.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        if (npc == null)
        {
            commander.SendMessage($"§cNo NPC found with name '{name}'");
            return;
        }

        commander.SendMessage($"§aYou have been teleported to NPC '{name}'");
        commander.SetPosition(npc.KnownPosition);
    }

    [Command(Name = "npc tp", Description = "Teleport an npc to you")]
    [Authorize(Permission = 4)]
    public void NpcTp(Player commander, string name)
    {
        var npc = PigNpcLoader.GetAll()
            .FirstOrDefault(n => n.Data.NameTag.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        if (npc == null)
        {
            commander.SendMessage($"§cNo NPC found with name '{name}'");
            return;
        }

        var targetLocation = commander.KnownPosition;

        npc.Data.X = targetLocation.X;
        npc.Data.Y = targetLocation.Y;
        npc.Data.Z = targetLocation.Z;
        npc.Data.Pitch = targetLocation.Pitch;
        npc.Data.Yaw = targetLocation.Yaw;
        npc.Data.HeadYaw = targetLocation.HeadYaw;

        PigNpcLoader.Register(npc);

        commander.SendMessage($"§aYou have updated the position of the npc '{name}' successfully");
    }

    [Command(Name = "npc setskin", Description = "Set the skin of a npc to your skin")]
    [Authorize(Permission = 4)]
    public void NpcSetSkin(Player commander, string name)
    {
        var npc = PigNpcLoader.GetAll()
            .FirstOrDefault(n => n.Data.NameTag.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        if (npc == null)
        {
            commander.SendMessage($"§cNo NPC found with name '{name}'");
            return;
        }

        npc.Skin = commander.Skin;
        npc.Data.Skin = npc.Skin;

        npc.SendSkin();

        PigNpcLoader.Register(npc);

        commander.SendMessage($"§aSkin of NPC '{name}' has been updated to your skin");
    }

    [Command(Name = "npc setskin",
        Description = "Set the skin of a npc to a given skin. skinName = texture & geometry file")]
    [Authorize(Permission = 4)]
    public void NpcSetSkin(Player commander, string name, string skinName)
    {
        var npc = PigNpcLoader.GetAll()
            .FirstOrDefault(n => n.Data.NameTag.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        if (npc == null)
        {
            commander.SendMessage($"§cNo NPC found with name '{name}'");
            return;
        }

        var folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", "npc_skins");

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            commander.SendMessage(
                "§eSkin folder 'npc_skins' has been created. Place .png and optionally .json files with matching skin IDs inside");
            return;
        }

        var texturePath = Path.Combine(folder, $"{skinName}.png");
        var geometryPath = Path.Combine(folder, $"{skinName}.json");

        if (!File.Exists(texturePath))
        {
            commander.SendMessage($"§cMissing texture file '{skinName}.png' in 'npc_skins/'");
            return;
        }

        var skinBytes = Skin.GetTextureFromFile(texturePath);
        if (skinBytes == null)
        {
            commander.SendMessage("§cInvalid skin texture. Must be 64x32, 64x64, or 128x128");
            return;
        }

        npc.Skin.Data = skinBytes;

        if (File.Exists(geometryPath))
        {
            var geometryJson = File.ReadAllText(geometryPath);
            var geometryName = $"geometry.{skinName}";
            var geometryModel = Skin.Parse(geometryJson);

            npc.Skin.GeometryName = geometryName;
            npc.Skin.GeometryData = Skin.ToJson(geometryModel);
            npc.Skin.SkinResourcePatch = new SkinResourcePatch
            {
                Geometry = new GeometryIdentifier { Default = geometryName }
            };
        }

        npc.Data.Skin = npc.Skin;

        npc.SendSkin();
        PigNpcLoader.Register(npc);

        commander.SendMessage(
            $"§aSkin of NPC '{name}' has been updated using '{skinName}.png'{(File.Exists(geometryPath) ? " and geometry" : "")}");
    }

    [Command(Name = "npc reload", Description = "Update the internal data of the plugin with the database")]
    [Authorize(Permission = 4)]
    public async void NpcReload(Player commander)
    {
        // Despawn & clear current NPCs
        foreach (var npc in PigNpcLoader.GetAll())
            npc.DespawnEntity();

        var internalField = typeof(PigNpcLoader).GetField("_npcsById", BindingFlags.NonPublic | BindingFlags.Static);
        if (internalField?.GetValue(null) is Dictionary<string, CustomNpc> npcsById)
            npcsById.Clear();

        await PigNpcLoader.LoadAllNpcsAsync();
        commander.SendMessage("§aNPCs have been reloaded from the database");
    }

    private static CustomNpc? FindNpcByName(string name, Player commander)
    {
        var npc = PigNpcLoader.GetAll()
            .FirstOrDefault(n => n.Data.NameTag.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        if (npc == null)
            commander.SendMessage($"§cNo NPC found with name '{name}'");

        return npc;
    }

    private static bool IsNameTaken(string name)
    {
        return PigNpcLoader.GetAll()
            .Any(n => n.Data.NameTag.Equals(name, StringComparison.InvariantCultureIgnoreCase));
    }

    private static string EnsureSkinFolder(Player commander)
    {
        var folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", "npc_skins");

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            commander.SendMessage("§eSkin folder 'npc_skins' created. Place your .png and .json files inside.");
        }

        return folder;
    }

    private static Skin? LoadSkinFromFiles(Player commander, string skinId, out string? errorMessage)
    {
        errorMessage = null;
        var folder = EnsureSkinFolder(commander);

        var texturePath = Path.Combine(folder, $"{skinId}.png");
        var geometryPath = Path.Combine(folder, $"{skinId}.json");

        if (!File.Exists(texturePath))
        {
            errorMessage = $"§cMissing texture file '{skinId}.png'";
            return null;
        }

        var skinBytes = Skin.GetTextureFromFile(texturePath);
        if (skinBytes == null)
        {
            errorMessage = "§cInvalid texture file. Must be 64x32, 64x64 or 128x128.";
            return null;
        }

        var skin = new Skin
        {
            Data = skinBytes,
            IsVerified = true
        };

        if (File.Exists(geometryPath))
        {
            var geometryJson = File.ReadAllText(geometryPath);
            var geometryName = $"geometry.{skinId}";
            var geometryModel = Skin.Parse(geometryJson);

            skin.GeometryName = geometryName;
            skin.GeometryData = Skin.ToJson(geometryModel);
            skin.SkinResourcePatch = new SkinResourcePatch
            {
                Geometry = new GeometryIdentifier { Default = geometryName }
            };
        }

        return skin;
    }
}