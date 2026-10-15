using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AirstrikeMod.Patches
{
    /// <summary>
    /// Makes a hovering aircraft clickable where it is drawn. Vanilla click-selection
    /// only looks at things in the cell under the mouse, but the hover is drawn
    /// <c>visualAltitude</c> cells above its ground cell, so it would otherwise only be
    /// selectable by clicking its shadow.
    /// </summary>
    [HarmonyPatch(typeof(Selector), "SelectableObjectsUnderMouse")]
    public static class Selector_SelectLoiter_Patch
    {
        public static void Postfix(ref IEnumerable<object> __result)
        {
            var map = Find.CurrentMap;
            if (map == null) return;
            List<object> hits = null;
            var mouse = UI.MouseMapPosition();
            foreach (var loiter in VehicleSkyfaller_Loiter.AllSpawned)
            {
                if (loiter == null || !loiter.Spawned || loiter.Map != map) continue;
                if (!loiter.SpriteContains(mouse) && UI.MouseCell() != loiter.Position) continue;
                (hits ??= new List<object>()).Add(loiter);
            }
            if (hits == null) return;
            __result = Prepend(hits, __result);
        }

        private static IEnumerable<object> Prepend(List<object> first, IEnumerable<object> rest)
        {
            foreach (var o in first) yield return o;
            if (rest == null) yield break;
            foreach (var o in rest)
                if (!first.Contains(o)) yield return o;
        }
    }
}
