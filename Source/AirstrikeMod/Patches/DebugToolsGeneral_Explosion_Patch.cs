using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LudeonTK;
using Verse;

namespace AirstrikeMod.Patches
{
    // Adds this mod's explosions to the debug menu (General > Explosion...) as
    // "Rocket_<name>" map tools: click a cell and the projectile detonates there
    // instantly, with all of its extra effects (nuke, white phosphorus, bunker buster,
    // secondaries, Combat Extended shrapnel).
    [HarmonyPatch(typeof(DebugToolsGeneral), "Explosion")]
    public static class DebugToolsGeneral_Explosion_Patch
    {
        private static List<ThingDef> _defs;

        private static List<ThingDef> Explosives => _defs ??= DefDatabase<ThingDef>.AllDefsListForReading
            .Where(d => d.projectile != null && d.projectile.explosionRadius > 0f
                        && d.modContentPack == AirstrikeMod.Content)
            .OrderBy(DebugLabel)
            .ToList();

        public static void Postfix(List<DebugActionNode> __result)
        {
            if (__result == null) return;
            foreach (var def in Explosives)
            {
                var d = def;
                __result.Add(new DebugActionNode(DebugLabel(d), DebugActionType.ToolMap, () =>
                {
                    var map = Find.CurrentMap;
                    var cell = UI.MouseCell();
                    if (map == null || !cell.InBounds(map)) return;
                    ExplosionFx.Trigger(d, d.projectile, cell, map, null);
                }));
            }
        }

        // ROCKET_Bullet_Bomb_BunkerBuster -> Rocket_BunkerBuster
        private static string DebugLabel(ThingDef d)
        {
            var name = d.defName;
            foreach (var prefix in new[] { "ROCKET_Bullet_Bomb_", "ROCKET_Bullet_", "ROCKET_" })
                if (name.StartsWith(prefix)) { name = name.Substring(prefix.Length); break; }
            return "Rocket_" + name;
        }
    }
}
