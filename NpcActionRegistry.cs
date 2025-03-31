using PigNet;
using PigNPC.Npc;

namespace PigNPC;

public class NpcActionRegistry
{
    public static readonly Dictionary<string, Action<CustomNpc, Player>> Actions = new();

    public static void ExecuteAction(string? actionId, CustomNpc npc, Player player)
    {
        Console.WriteLine($"Executing action {actionId} for {npc.Data.Id}");
        if (actionId != null && Actions.TryGetValue(actionId, out var action))
        {
            action(npc, player);
        }
    }
}