using Verse;

namespace AirstrikeMod
{
    /// <summary>
    /// The mod's extra explosion effects (pollution, nuclear core, white phosphorus,
    /// secondary layers), keyed on the exploding projectile def. Runs from the vanilla
    /// GenExplosion postfix, and directly after a Combat Extended explosion (which
    /// bypasses vanilla GenExplosion).
    /// </summary>
    internal static class ExplosionHooks
    {
        public static void Run(ThingDef projectile, IntVec3 center, Map map, float radius, Thing instigator)
        {
            if (projectile == null || map == null) return;
            var ext = projectile.GetModExtension<PolluteOnExplodeModExtension>();
            if (ext != null)
                PolluteHelper.PolluteCellsInRadius(map, center, radius * ext.radiusMultiplier, ext.chance);
            // Projectile defs only: item cook-offs are handled by CompExplosive_Detonate_Patch,
            // so an item def reaching here must not fire its secondaries a second time.
            if (projectile.projectile == null) return;
            NuclearDetonationHelper.Trigger(projectile, center, map);
            WhitePhosphorusHelper.Trigger(projectile, center, map, radius, instigator);
            SecondaryExplosionHelper.Trigger(projectile, center, map, radius, instigator);
        }
    }
}
