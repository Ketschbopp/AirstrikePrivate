using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AirstrikeMod
{
    /// <summary>
    /// Penetrating bomb. Put it on the bomb's projectile def. The bomb can be aimed at
    /// roofed cells, including under overhead mountain (other bombs can't), and before it
    /// detonates it punches through: constructed and thin rock roofs around the impact
    /// cave in on whatever is below, overhead mountain gets a hole punched straight
    /// through (no collapsed rock left behind), and natural rock walls right at the
    /// impact point are smashed.
    /// </summary>
    public class BunkerBusterModExtension : DefModExtension
    {
        // Roofs in this radius cave in (thin) or are punched out (thick).
        public float roofBreakRadius = 2.9f;
        // Natural rock walls in this radius are destroyed.
        public float rockBreakRadius = 1.5f;
    }

    internal static class BunkerBusterHelper
    {
        private static readonly List<IntVec3> _thin = new();
        private static readonly List<Thing> _tmp = new();

        /// <summary>True if <paramref name="ordinance"/> detonates as a bunker buster.</summary>
        public static bool IsBunkerBuster(ThingDef ordinance) =>
            CECompat.ProjectileFor(ordinance)?.HasModExtension<BunkerBusterModExtension>() ?? false;

        public static void Penetrate(ThingDef projectileDef, IntVec3 center, Map map)
        {
            var ext = projectileDef?.GetModExtension<BunkerBusterModExtension>();
            if (ext == null || map == null || !center.InBounds(map)) return;

            _thin.Clear();
            foreach (var c in GenRadial.RadialCellsAround(center, ext.roofBreakRadius, true))
            {
                if (!c.InBounds(map)) continue;
                var roof = c.GetRoof(map);
                if (roof == null) continue;
                if (roof.isThickRoof)
                    map.roofGrid.SetRoof(c, null); // clean hole, no collapsed rock
                else
                    _thin.Add(c);
            }
            if (_thin.Count > 0)
                RoofCollapserImmediate.DropRoofInCells(_thin, map);
            _thin.Clear();

            foreach (var c in GenRadial.RadialCellsAround(center, ext.rockBreakRadius, true))
            {
                if (!c.InBounds(map)) continue;
                _tmp.Clear();
                _tmp.AddRange(c.GetThingList(map));
                foreach (var t in _tmp)
                    if (!t.Destroyed && t.def.building is { isNaturalRock: true } && t.def.destroyable)
                        t.Destroy(DestroyMode.KillFinalize);
            }
            _tmp.Clear();
        }
    }
}
