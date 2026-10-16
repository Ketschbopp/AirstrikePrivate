# Rocket's Airstrike

A RimWorld 1.7 mod for [Vehicle Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3014915404). Adds airstrike gizmos to flying vehicles: drop bombs on a point, carpet-bomb in a line, or strafe with cannons. The aircraft taxis, takes off, flies the strike, then returns and lands - same map or across the world.

## What you get in-game

### Three strike modes

- **Precision Strike** - one bomb on one cell. Use when you only need to delete one target.
- **Bombing Run** - a line of bombs along a rotatable rectangle. Spacing is driven by the selected bomb's explosion radius.
- **Strafing Run** - fire down a long, thin rectangle. Uses its own ammo, not the bomb pool. The Falcon, Kraken and Apache can switch strafing weapon in the **Payload** tab:
  - **Cannon** - autocannon rounds (steel by default).
  - **Missiles** - air-to-ground missiles (`ROCKET_StrafingMissile`, crafted at the machining table) with a smoke trail and motor flame. 200% armor penetration and triple damage against vehicles, but a small blast covering only the 3x3 cells around the impact. Fewer shots per run (10 on jets, 6 on the Apache).
  - **Seeker missiles** - air-to-ground missiles with a seeker head (`ROCKET_SeekerMissile`, machining table, needs Microelectronics). Each missile locks onto a hostile Vehicle Framework vehicle within 18 cells of where it's aimed and steers into it in flight (turn-rate limited, so it visibly curves), following it if it moves. Salvos spread over vehicles not already targeted; if a target dies the missile looks for another; with nothing to lock it flies like a normal missile. Quadruple damage against vehicles. 8 per run on jets, 5 on the Apache. Tuning: `AirstrikeMod.SeekerMissileModExtension` (`seekRadius`, `turnRateDegPerTick`, `retargetInterval`, `terminalDistance`).

Each mode is a separate gizmo on the vehicle. A fourth **Select ordinance** gizmo picks which shell/bomb type the Precision and Bombing Run gizmos drop.

### Staying airborne (Apache)

The Apache doesn't have to land after a strafing run. With **Stay airborne** toggled on (the default; the toggle is on the grounded Apache), it finishes the run, overshoots the last target a few cells and hovers there.

Select the hovering Apache (click its shadow on the ground) to:

- order another **Strafing Run** - it flies straight from the hover, no takeoff needed, and returns to hovering afterwards;
- **switch weapon** (cannon / missiles) for the next run;
- **Land at...** a spot you pick (with rotation), **Land here** (nearest clear spot below it), or **Return to base** (the spot it originally took off from).

It burns fuel while hovering (8× its driving rate: about 21 in-game hours on a full tank) and makes an emergency landing at the nearest clear spot when the tank runs dry. While airborne it can't be shot at.

For modders: set `<loiterAfterSortie>true</loiterAfterSortie>` (and optionally `loiterFuelMultiplier`) on any in-place (`inPlaceSortie`) strike comp.

### Multi-target chains (Shift+click)

For Bombing Run and Strafing Run, **hold Shift while clicking** to add another rectangle to the run. Right-click to commit the chain (or cancel if nothing's locked).

When you chain two or more targets, the aircraft flies a smooth curved path through every rectangle in one continuous pass - no second takeoff, no landing in between. The plane banks naturally between rectangles. Chain length is capped by how many sortie-worths of ordnance you have in cargo.

A faded ghost of the aircraft is shown on the approach side of the cursor so you can see where the plane will be coming from.

### Pilot skill matters

Each strike comp has a required skill (Intellectual for bombs, Shooting for cannons) and a minimum level. The vehicle won't launch the strike until at least one assigned pilot meets the level. Higher skill levels narrow the bomb scatter and earn XP per drop (or once per strafing sortie). The tooltip on each gizmo shows the targeting accuracy rating in colour.

### Cross-map strikes

If you have more than one map loaded, clicking a strike gizmo opens a menu to pick which map you're striking. Pick the destination, then target on that map. The aircraft does a real round-trip world flight and is billed real fuel. Cross-map sorties also let you pick where the aircraft lands on the home map afterwards.

### Settings

- **General** tab:
  - Fuel cost percentage for an airstrike sortie, and hover fuel use for aircraft that stay airborne (0% = free).
  - Whether hover-capable aircraft stay airborne after strafing by default (each aircraft's own toggle still overrides it).
  - **Weapons**: bomb damage multiplier (25-300%, also scales CE armour penetration), bomb and strafing scatter multiplier (0% = pinpoint), strafing ammo use (0% = free), allow nuclear weapons (off hides the URDAM and tactical nuke from the payload picker), and white phosphorus lingering burn patches on/off.
  - **Reset to defaults** button.
- **Debug** tab: draw the spline flight path in white for tuning.
- **Debug actions**: in dev mode, the debug menu's General > Explosion... list gains a `Rocket_<name>` entry for every explosive projectile in the mod (bombs, missiles, the WP shell, the tactical nuke and URDAM when Rimatomics is loaded). Click a cell and it detonates there instantly with all of its effects.

## Vehicles that get the gizmos

**Ships in this mod**

- **Falcon** (`ROCKET_Falcon`) - jet aircraft, gated behind the Jet Aircraft research project (or Combat Vehicles when Vanilla Vehicles Expanded is loaded). Precision, Bombing Run, and Strafing Run.
- **Kraken** (`ROCKET_Kraken`) - J-20-style canard-delta jet. Inherits every property of the Falcon (`ParentName="ROCKET_FalconBase"`), including research, cost and compat patches; only the name and art differ.
- **Spirit** (`ROCKET_Spirit`) - stealth bomber, gated behind Stealth Aircraft. Precision and Bombing Run.

The Falcon, Kraken and Spirit can drop every bomb type on both Precision Strikes and Bombing Runs. A Bombing Run still needs `dropCount` (5) bombs in cargo.

Cargo capacity: Falcon/Kraken 280 kg, Spirit 2400 kg, Apache 600 kg.

**Mod-gated compatibility patches** (no-op if the host mod isn't loaded)

- **Vanilla Vehicles Expanded**: adds Precision + Strafing Run to the **Warbird**.
- **LTS Ammunition (Simple Ammo Pack)**: swaps the Falcon's strafing ammo from steel to `AmmoIndustrial`.

You can hand-edit `1.7/Patches/Vehicle_Patches.xml` to add the gizmos to any other aerial vehicle.

## Combat Extended (optional)

The mod works on vanilla and, optionally, with [Combat Extended](https://github.com/CombatExtended-Continued/CombatExtended). Nothing changes without CE; with CE active, `1.7/Mods/CombatExtended` loads and the DLL switches to CE behaviour (all via reflection - no hard dependency):

- **Bombs** explode through CE's `GenExplosionCE` (CE armour penetration and falloff) and throw CE shrapnel (Explosive, High-Explosive, Neum, Blockbuster, bunker buster bombs). Nuclear, EMP, white phosphorus and pollution effects still apply.
- **Vanilla mortar shells** in the payload lists work with CE's shell ammo (resolved through CE's `detonateProjectile`).
- **Cannon strafing** uses CE's 20x102mm NATO ammo set (the CE autocannon calibre): the Payload tab offers one cannon choice per ammo type - **AP, Incendiary, AP-HE, Sabot** - each firing its own CE round and consuming its own cartridges (one per bullet). Compatible with CE's **generic ammo** option (the choices then use the generic autocannon ammo) and its **ammo system off** option (strafing becomes free). Any strafing weapon can opt in with `<ammoSet>` (comp or `weapons` entry).
- **Missile strafing** fires a CE missile (450 mm penetrator, small HE burst, smoke trail).
- **Aircraft armour** converted to CE units (jets 6 mm / 12 MPa, Apache 10 mm / 22 MPa); bombs and missiles get inventory **Bulk**.

> Combat Extended has no RimWorld 1.7 release yet (it currently supports 1.6). The integration is written against CE's current source and activates once CE runs on 1.7.

## New items

- **Explosive / High-Explosive / Incendiary / Antigrain bomb** (`ROCKET_Bomb_*`) - large air-dropped bombs, craftable at the machining table. Drop in Bombs category.
- **Blockbuster bomb** (`ROCKET_Bomb_Blockbuster`) - bigger and heavier than the high-explosive bomb: 250 damage, 10.9 radius (HE: 150 / 7.8).
- **Neum bomb** (`ROCKET_Bomb_Neum`) - made with uranium. Same blast as the high-explosive bomb, plus an EMP pulse and fire from the same detonation.
- **Bunker buster** (`ROCKET_Bomb_BunkerBuster`) - long penetrating bomb (Advanced Fabrication). The only bomb that can be aimed at roofed cells and overhead mountain: before it detonates, constructed and thin rock roofs within 2.9 cells cave in on whatever is below, overhead mountain within 2.9 cells is punched out cleanly (no collapsed rock), and natural rock walls within 1.5 cells are smashed. Then a 5.9-radius blast for 260 damage / 0.6 armour penetration (`AirstrikeMod.BunkerBusterModExtension`).
- **Smoke bomb** (`ROCKET_Bomb_Smoke`) - bursts into a radius-8 cloud of blinding smoke.
- **White phosphorus bomb** (`ROCKET_Bomb_WhitePhosphorus`) - a 12-radius incendiary burst that sets everything alight and releases tox gas (harmful with Biotech). Bursts in a white flash and throws burning fragments outward on thick white smoke trails. Like real WP it clings and keeps burning: pawns caught in the burst or hit by fragments are set alight and take deep burns, and many fragments leave burning phosphorus that burns on any terrain, can't be put out and re-ignites its cell for 25-50 seconds. Leaves a blinding white smoke screen (`AirstrikeMod.WhitePhosphorusModExtension`).
- **Toxic gas bomb** (`ROCKET_Bomb_ToxGas`, **requires Biotech**) - no blast; fills a radius-9 area with tox gas. Lives in `1.7/Mods/Biotech`, loaded only when Biotech is active.
- **Tactical nuclear bomb** (`ROCKET_Bomb_TacticalNuke`, **requires [Dubs Rimatomics](https://github.com/Dubwise56/Rimatomics)**) - built from a Rimatomics fission warhead. Scaled to a 30-cell blast (about 54.5% of the warhead's own detonation, radius and damage alike): 30-radius plasma blast for 327 damage / 54.5 armor penetration, everything within 13.6 cells vaporised, roofs stripped to 30 cells, a mushroom cloud - plus an EMP pulse over the blast radius. Does not cook off when damaged. Lives in `1.7/Mods/Rimatomics`, loaded only when Rimatomics is active.
- **URDAM** (`ROCKET_Bomb_URDAM`, **requires Dubs Rimatomics**) - a JDAM-style guided nuke with the tactical nuke's original numbers: 8.25-radius plasma blast for 90 damage / 15 armor penetration, 3.75-cell vaporise core, roofs stripped to 8.25 cells, a small mushroom cloud and EMP. Its guidance kit (`AirstrikeMod.GuidedBombModExtension`, `scatterMultiplier` 0) ignores the aircraft's scatter, so it always lands on the aimed cell.
- The Bombing Run and Precision Strike can also drop any vanilla mortar shell from cargo.
- **White phosphorus shell** (`ROCKET_Shell_WhitePhosphorus`) - a mortar shell version of the white phosphorus bomb (Mortars research, machining table): the same burst, sticky fire, lingering burning phosphorus and smoke screen over a 5.9 radius. Fits any vanilla mortar. With Combat Extended it has no recipe, because CE mortars only fire CE ammo.
- **IEDs** (`ROCKET_IED_*`, Security tab, IEDs research plus the bomb's own research) - one per bomb except the tactical nuke: explosive, high-explosive, incendiary, antigrain, blockbuster, neum, smoke, white phosphorus, bunker buster, toxic gas (Biotech) and URDAM (Rimatomics). Built from one bomb, 10 steel and 1 component. Walking over one doesn't set it off; press **Detonate** (select several to blow them together) and it explodes with the bomb's own projectile and all of its effects. Heavy damage lights a short wick, and destroying it sets it off at once. Shows its blast radius while placing and when selected. The URDAM IED can't be detonated while nuclear weapons are disabled in the settings (`AirstrikeMod.CompProperties_RemoteDetonator`, `bomb` field).

## Pilot records

Pilots earn three records visible in the Records tab:

- **sorties flown** - once per launch, regardless of mode.
- **ordinance dropped** - one per bomb (chosen pilot only, not the whole crew).
- **time on sortie** - wall-clock ticks spent in an airstrike-flagged sortie.

---

## For modders: adding airstrike to your own vehicle

The mod exposes three comps that you bolt onto a `Vehicles.VehicleDef`. The vehicle must already have a `Vehicles.CompProperties_VehicleLauncher` (i.e. it's a Vehicle Framework aerial vehicle).

### Quickest path: patch an existing vehicle

```xml
<Operation Class="PatchOperationFindMod">
    <mods><li>YourHostMod</li></mods>
    <match Class="PatchOperationAdd">
        <xpath>Defs/Vehicles.VehicleDef[defName="YourVehicle"]/comps</xpath>
        <value>
            <li Class="AirstrikeMod.CompProperties_AirstrikePrecision">
                <skyfallerBombing>ROCKET_BombingPass</skyfallerBombing>
                <bombFireSound>ROCKET_HardpointLatchRelease</bombFireSound>
                <requiredSkill>Intellectual</requiredSkill>
                <requiredSkillLevel>4</requiredSkillLevel>
                <scatter>1</scatter>
                <skillScatter>4</skillScatter>
                <ordinance>
                    <li>Shell_HighExplosive</li>
                    <li>ROCKET_Bomb_Explosive</li>
                </ordinance>
            </li>
        </value>
    </match>
</Operation>
```

Mod-gate with `PatchOperationFindMod` so your patch is silent when the host mod (or this mod) isn't loaded.

### The three comp classes

| Class | Gizmo | Pattern |
|---|---|---|
| `AirstrikeMod.CompProperties_AirstrikePrecision` | Precision Strike | Single bomb on the cursor cell |
| `AirstrikeMod.CompProperties_AirstrikeBombingRun` | Bombing Run | Line of `dropCount` bombs |
| `AirstrikeMod.CompProperties_AirstrikeStrafingRun` | Strafing Run | Cannon fire across a rectangle |

You can add any subset. A vehicle with all three gets all three gizmos.

### Shared fields (all three comps)

These come from `CompProperties_AirstrikeBase`:

| Field | Default | Meaning |
|---|---|---|
| `skyfallerBombing` | required | Skyfaller def used for the buzz. Always `ROCKET_BombingPass` unless you've defined your own. |
| `ordinance` | (list) | Allowed ThingDefs the player can pick in the ordnance selector. Precision and Bombing only. Strafing ignores this. |
| `scatter` | `0` | Floor on per-bomb random offset in cells. 0 = always lands exactly. |
| `skillScatter` | `0` | Additional scatter cells added at zero pilot skill; falls to 0 at max skill. Total scatter is capped at `flyAltitude` and at 16 cells. |
| `flyAltitude` | `6` | Draw altitude of the plane during the buzz. Also caps total scatter. Lower for low-altitude strafers. |
| `sortieSpeedMultiplier` | `1` | Multiplies the plane's `FlightSpeed` for the whole sortie. `0.5` = slow movie pass; `1.5` = streaking attack. |
| `bombFireSound` | `null` | One-shot sound at the plane's position per bomb dropped. Strafing has its own `fireSound` field. |
| `requiredSkill` | `null` | `SkillDef` gating launch and driving accuracy. Null = anyone can use, no XP awarded. |
| `requiredSkillLevel` | `0` | Minimum level the best pilot must have in `requiredSkill`. Ignored if `requiredSkill` is null. |

### `CompProperties_AirstrikeBombingRun` extras

| Field | Default | Meaning |
|---|---|---|
| `dropCount` | `5` | Bombs per sortie. Required-shells count. Drives footprint length. |
| `spacingMultiplier` | `1.8` | Bomb-to-bomb spacing as a multiple of the selected bomb's explosion radius. |

### `CompProperties_AirstrikeStrafingRun` extras

| Field | Default | Meaning |
|---|---|---|
| `projectileDef` | required | Vanilla bullet projectile to spawn per shot (e.g. `Bullet_AutocannonTurret`). |
| `ammoDef` | `null` | Cargo item consumed for rounds. Null = free (no ammo cost). |
| `ammoPerRound` | `1` | Units of `ammoDef` per round fired. |
| `fireSound` | `null` | One-shot sound at each fired cell. |
| `runLength` | `40` | Cells along the flight axis. Drives sortie ammo cost (`runLength × ammoPerRound`). |
| `runWidth` | `3` | Cells perpendicular to flight axis. Each fire-cell gets a random perpendicular offset within this width. |
| `leadCells` | `8` | Fires this many cells ahead of the plane's current position (so the bullets visibly fall from the plane). |
| `bulletsPerRound` | `4` | Bullets spawned per fired cell. |
| `spreadCells` | `1` | Per-bullet biaxial scatter on top of the run-width perpendicular offset. |
| `fireOriginOffset` | `3` | Cells between bullet spawn point and its target cell (gives the bullet visible travel time). |

### Multi-damage bombs: `SecondaryExplosionsModExtension`

Vanilla explosions carry a single damage type. Add `AirstrikeMod.SecondaryExplosionsModExtension` to a bomb's projectile def (airstrike and mortar path) and to its item def (cook-off path) to fire extra explosions on the same cell in the same tick. The Neum bomb uses it for its EMP layer:

```xml
<modExtensions>
    <li Class="AirstrikeMod.SecondaryExplosionsModExtension">
        <explosions>
            <li>
                <damageDef>EMP</damageDef>
                <!-- optional: radius (default = primary radius), damageAmount,
                     armorPenetration, chanceToStartFire, explosionSound -->
            </li>
        </explosions>
    </li>
</modExtensions>
```

### Strafing weapons: `weapons`

`CompProperties_AirstrikeStrafingRun`'s own fields form the default weapon (named by `defaultWeaponLabel`, default `cannon`). Add a `weapons` list to offer alternatives in the Payload tab; any field left out inherits the comp's value:

```xml
<weapons>
    <li>
        <label>missiles</label>
        <projectileDef>ROCKET_Bullet_StrafingMissile</projectileDef>
        <ammoDef>ROCKET_StrafingMissile</ammoDef>
        <bulletsPerRound>1</bulletsPerRound>
        <spreadCells>0</spreadCells>
        <runLength>10</runLength>
        <runWidth>1</runWidth>
        <!-- also: ammoPerRound, leadCells, fireOriginOffset, fireSound -->
    </li>
</weapons>
```

`AirstrikeMod.Projectile_AirstrikeMissile` registers its own def as the damage weapon, so Vehicle Framework's `Vehicles.VehicleDamageMultiplierDefModExtension` on the projectile def scales its damage against vehicles.

### Penetrating bombs: `BunkerBusterModExtension`

On a bomb's projectile def. The bomb can be targeted at roofed cells, and before the explosion it collapses thin roofs and removes thick roofs within `roofBreakRadius` (default 2.9) and destroys natural rock within `rockBreakRadius` (default 1.5).

### Nuclear effects: `NuclearDetonationModExtension`

On a projectile def, adds Rimatomics-style detonation effects to its explosion: `vaporizeRadius` (destroys everything except the attacking aircraft and effects), `roofStripRadius` (removes non-thick roofs), `detonationSound`, `cloudMote` and `cloudScale`.

### Engine flames on inheriting defs

When a vehicle has more than one `CompProperties_EngineFlame` (for example a child def appending its own to an inherited one), only the **last** one draws. The Kraken uses this to replace the Falcon's single centre flame with twin flames.

### Sounds and visuals provided by the mod

Reference these directly from your patches - they're shipped by Rocket's Airstrike.

| Def | What it's for |
|---|---|
| `ROCKET_BombingPass` | The standard buzz skyfaller. Use as `<skyfallerBombing>`. |
| `ROCKET_HardpointLatchRelease` | Per-bomb release thunk. Positional, pitch-varied. |
| `ROCKET_InterfaceBeep1` | UI beep on confirmed target placement (used internally by all targeters). |

### Engine flames (optional, cosmetic)

Add `AirstrikeMod.CompProperties_EngineFlame` to draw animated exhaust flames during takeoff, landing, and the buzz. See the Falcon's vehicle def for a complete example - you set the flame `drawSize`, `pivot`, `rotationDeg`, and per-cardinal-direction offset lists.

### Multi-comp on one vehicle

The "Select ordinance" picker and the `ShowAllOrdinance` toggle are stored on the *primary* comp (first-found) of the vehicle. All comps share the same selected ordnance and visibility setting at runtime, so you can attach Precision + BombingRun and they'll always operate on the same bomb type.

### Save compatibility note

If you rename or remove a `CompProperties_Airstrike*` from a vehicle after a save was created with the previous setup, the comp's stored fields (`selectedOrdinance`, `showAllOrdinance`) will simply default on load. In-flight sorties survive save/load via the skyfaller's own Scribe entries.

---

## Project layout

```
About/                          Mod metadata; depends on VF + Harmony.
1.7/
  Defs/
    Skyfallers.xml              ROCKET_BombingPass + ROCKET_FallingBomb.
    SoundDefs.xml               Mod sounds (engine, hardpoint, beep).
    Stats/                      ROCKET_TargetingAbility (vestigial; skill-driven now).
    RecordDefs/                 Pilot record defs.
    ResearchDefs/               ROCKET_JetAircraft, ROCKET_StealthAircraft.
    ThingCategoryDefs/          Bombs category.
    ThingDefs_Items/            Bombs, strafing missile ammo and their projectiles.
  Mods/Rimatomics/              Tactical nuke + ordinance patch (loaded only with Rimatomics).
    VehicleDefs/                Falcon, Kraken (inherits Falcon), Spirit, Apache + buildables.
  Patches/
    Vehicle_Patches.xml         VVE Warbird & Falcon upgrades (mod-gated).
    LTS_Ammunition_Patch.xml    LTS Ammunition compat (mod-gated).
  Assemblies/                   Shipped DLL.
Source/AirstrikeMod/            C# source (csproj + .cs files).
Source/Art/                     Scripts that generate the Kraken sprites, bomb icons and missile textures.
Languages/English/Keyed/        Translatable strings.
Textures/                       Vehicle, item, and UI textures.
Sounds/                         WAV files referenced by SoundDefs.
```

## Building from source

1. Install the .NET 10 SDK (RimWorld 1.7 runs on .NET 10).
2. Open `Source/AirstrikeMod/AirstrikeMod.csproj` in Rider or Visual Studio, or run `dotnet build -c Release`.
3. RimWorld itself is referenced through the `Krafs.Rimworld.Ref` NuGet package, so no game install is needed. Vehicle Framework is referenced from `<VehicleFrameworkDir>` (its `1.7/Assemblies`) - edit that property, or pass `-p:VehicleFrameworkDir=...`, if your workshop folder isn't at the default Steam location.
4. Output goes to `Source/Assemblies/AirstrikeMod.dll`. Copy it to `1.7/Assemblies/` for release.

Target framework `net10.0`, language version `latest`.

## Dependencies

- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)
- [Vehicle Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3014915404)
