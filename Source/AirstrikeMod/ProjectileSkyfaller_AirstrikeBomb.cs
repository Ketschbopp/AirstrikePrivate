using RimWorld;
using Vehicles;
using Verse;

namespace AirstrikeMod
{
    // Subclass of VF's ProjectileSkyfaller. Animates a bomb falling from origin to
    // destination, then runs our own explosion at impact rather than the wrapped
    // projectile's default Launch path.
    public class ProjectileSkyfaller_AirstrikeBomb : ProjectileSkyfaller
    {
        public ThingDef ordinance;

        protected override void Impact()
        {
            var map = Map;
            var pos = Position;
            var projDef = CECompat.ProjectileFor(ordinance);
            var pp = projDef?.projectile;
            if (pp == null || !pos.InBounds(map))
            {
                Destroy();
                return;
            }

            ExplosionFx.Trigger(projDef, pp, pos, map, caster);
            Destroy();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref ordinance, nameof(ordinance));
        }
    }
}
