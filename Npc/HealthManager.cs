using PigNet;
using PigNet.Entities;
using PigNet.Items;

namespace PigNPC.Npc;

public class HealthManager(Entity entity) : PigNet.HealthManager(entity)
{
    public override void TakeHit(Entity source, int damage = 1, DamageCause cause = DamageCause.Unknown)
    {
    }

    public override void TakeHit(Entity source, Item tool, int damage = 1, DamageCause cause = DamageCause.Unknown)
    {
    }
    
    public override void OnTick()
    {
    }
}