using HarmonyLib;
using Verse;

namespace AirstrikeMod.Patches
{
    [HarmonyPatch(typeof(GenExplosion), nameof(GenExplosion.DoExplosion))]
    public static class GenExplosion_DoExplosion_Patch
    {
        public static void Postfix(IntVec3 center, Map map, float radius, ThingDef projectile,
            Thing instigator)
        {
            if (projectile == null || map == null) return;
            var ext = projectile.GetModExtension<PolluteOnExplodeModExtension>();
            if (ext != null)
                PolluteHelper.PolluteCellsInRadius(map, center, radius * ext.radiusMultiplier, ext.chance);
            // Projectile defs only: item cook-offs are handled by CompExplosive_Detonate_Patch,
            // so an item def reaching here must not fire its secondaries a second time.
            if (projectile.projectile != null)
                SecondaryExplosionHelper.Trigger(projectile, center, map, radius, instigator);
        }
    }
}
