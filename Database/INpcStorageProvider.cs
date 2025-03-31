using PigNPC.Npc;

namespace PigNPC.Database;

public interface INpcStorageProvider
{
    Task<IEnumerable<NpcData>> LoadAllAsync();
    Task SaveAsync(NpcData npc);
    Task DeleteAsync(string npcId);
    Task<NpcData?> GetByIdAsync(string npcId);
    Task SaveAllAsync(IEnumerable<NpcData> npcs);
}