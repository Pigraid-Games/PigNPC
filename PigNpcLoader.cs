using System.Reflection;
using log4net;
using PigNet;
using PigNet.Plugins;
using PigNet.Plugins.Attributes;
using PigNet.Utils.Skins;
using PigNet.Utils.Vectors;
using PigNPC.Database;
using PigNPC.Npc;

namespace PigNPC;

[Plugin(PluginName = "PigNPC",
    PluginVersion = "1.0.0-RELEASE",
    Description = "PigNPC is a plugin that allows you to create NPCs in Minecraft",
    Author = "Antoine LANGEVIN")]
public class PigNpcLoader : Plugin
{
    private static readonly Dictionary<string, CustomNpc> _npcsById = new();
    private static INpcStorageProvider _storageProvider = null!;
    private static LevelManager _levelManager = null!;
    
    private static readonly ILog Log = LogManager.GetLogger(typeof(PigNpcLoader));

    public const string DatabaseJson = "json";
    public const string DatabaseSqlite = "sqlite";
    public const string DatabaseMysql = "mysql";
    public const string DbFolder = "/npc_db/";
    
    public enum SkinType : short
    {
        PlayerSkin,
        PersistantSkin
    }
    
    protected override void OnEnable()
    {
        var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (folder == null)
        {
            Log.Error("There was an error while getting the folder");
            return;
        }
        _storageProvider = NpcStorageFactory.Create(DatabaseJson, folder + DbFolder);
        _levelManager = Context.LevelManager;
        
        Context.Server.PluginManager.LoadCommands(new Commands());

        Context.Server.PlayerFactory.PlayerCreated += (_, args) =>
        {
            var player = args.Player;

            player.PlayerJoin += (_, _) =>
            {
                _npcsById.ToList().ForEach(npc => npc.Value.SendSkin([player]));
            };

            player.PlayerDamageToEntity += (_, eventArgs) =>
            {
                var entity = eventArgs.Entity;
                if (entity is not CustomNpc npc) return;
                NpcActionRegistry.ExecuteAction(npc.Data.ActionId, npc, player);
            };
        };
        
        LoadAllNpcsAsync().GetAwaiter().GetResult();
    }

    public static async Task LoadAllNpcsAsync()
    {
        var npcs = await _storageProvider.LoadAllAsync();
        var folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", "npc_skins");

        foreach (var data in npcs)
        {
            var level = _levelManager.Levels.FirstOrDefault(l => l.LevelName.Equals(data.LevelName));
            if (level == null)
            {
                Log.Debug("Trying to load the level of the NPC...");
                var levelLoaded = _levelManager.GetLevel(data.LevelName);
                if (levelLoaded == null)
                {
                    Log.Debug("Couldn't find the level for this NPC, skipping it");
                    continue;
                }

                levelLoaded.Initialize();
                level = levelLoaded;
            }

            Log.Warn($"Trying to get the skin for NPC {data.NameTag}");
            var skinName = Path.GetFileNameWithoutExtension(data.GeometryJsonName);
            var texturePath = Path.Combine(folder, $"{skinName}.png");
            var geometryPath = Path.Combine(folder, $"{skinName}.json");
            var knownPosition = new PlayerLocation(data.X, data.Y, data.Z, data.HeadYaw, data.Yaw, data.Pitch);
            Log.Warn("skinName: " + skinName + " texturePath: " + texturePath + " geometryPath: " + geometryPath);
            
            CustomNpc npc;

            byte[]? skinBytes = null;
            if (!File.Exists(texturePath))
            {
                npc = new CustomNpc(data, data.NameTag, level)
                {
                    KnownPosition = knownPosition
                };
                if (data.SkinType == SkinType.PersistantSkin)
                {
                    Log.Warn("Skin file not found, trying to use saved skin");
                    npc.Skin = data.Skin;
                    skinBytes = npc.Skin!.Data;
                }
                else Log.Warn("PlayerSkin type detected - no skin needed");
            }
            else
            {
                Log.Warn("Skin file found, trying to apply it...");
                skinBytes = Skin.GetTextureFromFile(texturePath);
                npc = new CustomNpc(data, data.NameTag, level)
                {
                    KnownPosition = knownPosition,
                    Skin = new Skin
                    {
                        Data = skinBytes
                    }
                };
            }

            if (File.Exists(geometryPath))
            {
                Log.Warn("Geometry file found for skin, trying to apply it...");

                data.GeometryJsonName = skinName + ".json";

                var geometryJson = File.ReadAllText(geometryPath);
                geometryJson = geometryJson.Replace("geometry.unknown", $"geometry.{skinName}");

                var geometryModel = Skin.Parse(geometryJson);
                var fullGeometryName = $"geometry.{skinName}";

                npc = new CustomNpc(data, data.NameTag, level)
                {
                    KnownPosition = knownPosition,
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
            
            npc.Spawn();
            _npcsById[data.Id] = npc;
        }

        Log.Info($"Loaded {_npcsById.Count} NPCs");
    }

    public static void Register(CustomNpc npc)
    {
        _npcsById[npc.Data.Id] = npc;
        _storageProvider.SaveAsync(npc.Data); // Async fire-and-forget
    }

    public static void Unregister(string npcId)
    {
        if (_npcsById.TryGetValue(npcId, out var npc))
        {
            npc.DespawnEntity();
            _npcsById.Remove(npcId);
            _storageProvider.DeleteAsync(npcId);
        }
    }

    public static CustomNpc? GetNpcById(string npcId)
    {
        return _npcsById.GetValueOrDefault(npcId);
    }

    public static IEnumerable<CustomNpc> GetAll() => _npcsById.Values;

    public static async Task SaveAllAsync()
    {
        var data = _npcsById.Values.Select(n => n.Data);
        await _storageProvider.SaveAllAsync(data);
    }

    public override void OnDisable()
    {
        foreach (var npc in _npcsById.Values)
            npc.DespawnEntity();

        _npcsById.Clear();
    }
}
