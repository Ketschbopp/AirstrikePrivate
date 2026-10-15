using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AirstrikeMod
{
    /// <summary>
    /// Extra explosions layered on top of a bomb's primary blast, centred on the same
    /// cell in the same tick. Lets one bomb deal several damage types at once (e.g. the
    /// Neum bomb's HE + EMP), which vanilla explosions can't since they carry a single
    /// DamageDef. Put it on the projectile def (airstrike / mortar path) and on the item
    /// def (cook-off path via CompExplosive).
    /// </summary>
    public class SecondaryExplosionsModExtension : DefModExtension
    {
        public List<SecondaryExplosion> explosions = new List<SecondaryExplosion>();
    }

    public class SecondaryExplosion
    {
        public DamageDef damageDef;
        // Negative = reuse the primary explosion's radius.
        public float radius = -1f;
        // Negative = the damage def's default.
        public int damageAmount = -1;
        public float armorPenetration = -1f;
        public float chanceToStartFire;
        public SoundDef explosionSound;
    }

    internal static class SecondaryExplosionHelper
    {
        public static void Trigger(Def source, IntVec3 center, Map map, float primaryRadius,
            Thing instigator)
        {
            var ext = source?.GetModExtension<SecondaryExplosionsModExtension>();
            if (ext?.explosions == null || map == null || !center.InBounds(map)) return;

            foreach (var e in ext.explosions)
            {
                if (e?.damageDef == null) continue;
                var radius = e.radius >= 0f ? e.radius : primaryRadius;
                if (radius <= 0f) continue;

                // projectile: null so this explosion doesn't re-enter our DoExplosion
                // postfix (no recursion, no double pollution).
                GenExplosion.DoExplosion(
                    center: center,
                    map: map,
                    radius: radius,
                    damType: e.damageDef,
                    instigator: instigator,
                    damAmount: e.damageAmount,
                    armorPenetration: e.armorPenetration,
                    explosionSound: e.explosionSound,
                    chanceToStartFire: e.chanceToStartFire);
            }
        }
    }
}
