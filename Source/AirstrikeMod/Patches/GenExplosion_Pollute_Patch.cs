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
            ExplosionHooks.Run(projectile, center, map, radius, instigator);
        }
    }
}
