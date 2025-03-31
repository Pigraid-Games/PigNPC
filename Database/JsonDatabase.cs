using System.Text.Json;
using PigNPC.Npc;

namespace PigNPC.Database;

public class JsonDatabase : INpcStorageProvider
{
    private readonly string _folderPath;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonDatabase(string folderPath)
    {
        _folderPath = folderPath;

        if (!Directory.Exists(_folderPath))
            Directory.CreateDirectory(_folderPath);
    }

    public Task<IEnumerable<NpcData>> LoadAllAsync()
    {
        var files = Directory.GetFiles(_folderPath, "*.json");
        var results = new List<NpcData>();

        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var npc = JsonSerializer.Deserialize<NpcData>(json, _serializerOptions);
            if (npc != null)
                results.Add(npc);
        }

        return Task.FromResult<IEnumerable<NpcData>>(results);
    }

    public Task<NpcData?> GetByIdAsync(string npcId)
    {
        var path = GetNpcFilePath(npcId);
        if (!File.Exists(path))
            return Task.FromResult<NpcData?>(null);

        var json = File.ReadAllText(path);
        var npc = JsonSerializer.Deserialize<NpcData>(json, _serializerOptions);
        return Task.FromResult(npc);
    }

    public Task SaveAsync(NpcData npc)
    {
        var json = JsonSerializer.Serialize(npc, _serializerOptions);
        File.WriteAllText(GetNpcFilePath(npc.Id), json);
        return Task.CompletedTask;
    }

    public Task SaveAllAsync(IEnumerable<NpcData> npcs)
    {
        foreach (var npc in npcs)
        {
            var json = JsonSerializer.Serialize(npc, _serializerOptions);
            File.WriteAllText(GetNpcFilePath(npc.Id), json);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string npcId)
    {
        var path = GetNpcFilePath(npcId);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    private string GetNpcFilePath(string npcId)
    {
        var safeId = npcId.Replace(":", "_"); // Avoid invalid characters
        return Path.Combine(_folderPath, $"{safeId}.json");
    }
}