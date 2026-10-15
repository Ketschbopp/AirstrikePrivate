using HarmonyLib;
using RimWorld;
using Verse;

namespace AirstrikeMod.Patches
{
    // Bomb items cooking off in storage go through CompExplosive, which doesn't tell
    // GenExplosion which item exploded. Fire the item's secondary explosions here.
    [HarmonyPatch(typeof(CompExplosive), "Detonate")]
    public static class CompExplosive_Detonate_Patch
    {
        public static void Prefix(CompExplosive __instance, out (bool already, IntVec3 pos) __state)
        {
            __state = (__instance.destroyedThroughDetonation, __instance.parent.PositionHeld);
        }

        public static void Postfix(CompExplosive __instance, Map map,
            (bool already, IntVec3 pos) __state)
        {
            // Only when this call actually detonated (vanilla sets the flag on success).
            if (__state.already || !__instance.destroyedThroughDetonation) return;
            var def = __instance.parent.def;
            if (def.HasModExtension<SecondaryExplosionsModExtension>())
                SecondaryExplosionHelper.Trigger(def, __state.pos, map, __instance.ExplosiveRadius(), null);
            if (def.HasModExtension<WhitePhosphorusModExtension>())
                WhitePhosphorusHelper.Trigger(def, __state.pos, map, __instance.ExplosiveRadius(), null);
        }
    }
}
