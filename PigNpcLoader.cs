using System.Reflection;
using System.Text;
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
    PluginVersion = "ALPHA-20241027",
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

            var skinName = Path.GetFileNameWithoutExtension(data.GeometryJsonName);
            var texturePath = Path.Combine(folder, $"{skinName}.png");
            var geometryPath = Path.Combine(folder, $"{skinName}.json");

            if (!File.Exists(texturePath))
            {
                Log.Warn($"Missing texture for NPC {data.NameTag}, skipping...");
                continue;
            }

            var skinBytes = Skin.GetTextureFromFile(texturePath);
            if (skinBytes == null)
            {
                Log.Warn($"Invalid texture for NPC {data.NameTag}, skipping...");
                continue;
            }

            var npc = new CustomNpc(data, data.NameTag, level)
            {
                Skin = new Skin
                {
                    Data = skinBytes
                }
            };

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
                    KnownPosition = new PlayerLocation(data.X, data.Y, data.Z, data.HeadYaw, data.Yaw, data.Pitch),
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

            npc.Data.Skin = npc.Skin;
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
