# AGENTS.md

Authoritative guidance for developers working on the Magitek routine.

---

## Quick Start

- **WINDOWS 11 DEVELOPMENT ENVIRONMENT REQUIRED** - This project targets Windows 11 and uses Windows-specific paths and APIs.
- Restore NuGet packages: `dotnet restore Magitek\Magitek.sln`
- Build: `dotnet build Magitek\Magitek.sln`
  - **Note:** The solution file is in the `Magitek` subdirectory, not the workspace root.
  - On Windows PowerShell, use semicolon separators: `cd Magitek; dotnet build Magitek.sln`
- Magitek targets **.NET 8.0 (net8.0-windows8.0)** and is loaded inside RebornBuddy.
- C# Language Version: **C# 10** (enforced via `<LangVersion>10</LangVersion>` in Magitek.csproj).
- Preferred IDE: Visual Studio 2022 or JetBrains Rider with the Windows workload.

### Runtime tips

- Use the provided VSCode/Cursor task to build `Magitek.sln`, copy the updated `Magitek.dll` into your RebornBuddy `Routines\Magitek` folder, and launch RebornBuddy.
- Test changes via a normal RebornBuddy session (attach to FFXIV and let it load Magitek as the combat routine).
- Settings are stored alongside RebornBuddy's normal character settings (via `JsonSettings.CharacterSettingsDirectory`) and can be adjusted through the in‑game Magitek Settings UI (`BaseSettings.Instance.ShowSettingsModal`).

---

## FFXIV Job Context

### Job Naming Conventions

Magitek automates **Final Fantasy XIV combat** for all jobs (classes). Understanding when to use full names vs. abbreviations is critical:

- **Full job names** (Machinist, WhiteMage, DarkKnight, RedMage, etc.):
  - Folder names: `Logic/Machinist/`, `Models/WhiteMage/`, `Rotations/DarkKnight/`
  - Namespaces: `Magitek.Logic.Machinist`, `Magitek.Models.Sage`
  - Class names: `public static class Machinist`, `WhiteMageSettings`
  - Settings file paths: `"/Magitek/Machinist/MachinistSettings.json"`

- **Three-letter job abbreviations** (MCH, WHM, DRK, RDM, etc.):
  - Spell region blocks in `Utilities/Spells.cs`: `#region MCH`, `#region WHM`, `#region AST`
  - Comments and documentation referring to FFXIV job codes
  - User-facing UI text (sometimes)

**Pattern:** Use **full names** for all code structure (folders, classes, namespaces). Use **abbreviations** only in `Spells.cs` regions and FFXIV-specific comments.

### Job Abbreviations Reference

**Tanks:** PLD (Paladin), WAR (Warrior), DRK (DarkKnight), GNB (Gunbreaker)  
**Healers:** WHM (WhiteMage), SCH (Scholar), AST (Astrologian), SGE (Sage)  
**Melee DPS:** MNK (Monk), DRG (Dragoon), NIN (Ninja), SAM (Samurai), RPR (Reaper), VPR (Viper)  
**Ranged Phys:** BRD (Bard), MCH (Machinist), DNC (Dancer)  
**Ranged Mag:** BLM (BlackMage), SMN (Summoner), RDM (RedMage), PCT (Pictomancer)  
**Limited:** BLU (BlueMage)

**Multi-word jobs** (WhiteMage, BlackMage, RedMage, DarkKnight, BlueMage) are **single words with no spaces** in code.

### PvE vs PvP Separation

FFXIV has completely separate ability systems for PvE and PvP with **different spell IDs**:

- **PvE spells** are in job-specific regions in `Utilities/Spells.cs`:
  ```csharp
  #region MCH
  public static readonly SpellData BlastCharge = DataManager.GetSpellData(7410);
  public static readonly SpellData Wildfire = DataManager.GetSpellData(2878);
  #endregion
  ```

- **PvP spells** are in a separate `#region PVP` at the bottom of `Spells.cs` with "Pvp" suffix:
  ```csharp
  #region PVP
  //MCH
  public static readonly SpellData BlastChargePvp = DataManager.GetSpellData(29402);
  public static readonly SpellData WildfirePvp = DataManager.GetSpellData(29403);
  #endregion
  ```

- **Each job has a dedicated `Pvp.cs` file** in their `Logic/` folder containing PvP-specific rotation logic.

- **Never mix PvE and PvP spell IDs.** Always use the appropriate spell variant (with or without "Pvp" suffix) based on context.

### FFXIV Combat Fundamentals

Understanding FFXIV-specific combat mechanics is essential for writing effective rotation logic:

#### GCD vs oGCD (Global Cooldown vs off-Global Cooldown)
- **GCD abilities** trigger the ~2.5s global cooldown shared by most weaponskills and spells (e.g., Stone, Heavy Swing, Fire).
- **oGCD abilities** are instant-cast abilities that don't trigger the GCD (e.g., Bloodbath, Wildfire, Feint).
- **Weaving:** The practice of using oGCDs between GCD abilities during animation lock windows.
- Use `GlobalCooldown.CanWeave(maxWeaveCount)` to check if there's time to weave oGCDs before the next GCD comes up.
- Pattern: Check `CanWeave()` before executing oGCD abilities to avoid clipping the GCD.

#### Job Resources and Gauges
- Each job has unique resources tracked via `ActionResourceManager.<JobName>` (e.g., `ActionResourceManager.Machinist.Battery`, `ActionResourceManager.WhiteMage.Lily`).
- Resources are typically checked in logic files before casting resource-spending abilities.
- Examples: Battery (MCH), Kenki (SAM), Blood (DRK), Lily (WHM), Aetherflow (SCH/SGE).

#### Positionals (Melee DPS)
- Many melee attacks have **positional requirements** for bonus damage: Rear (behind target) or Flank (side of target).
- Tracked via `PositionalState` enum: `None`, `Front`, `Flank`, `Rear`, `NotAvailable`.
- The bot tracks current position via `Core.Me.CurrentTarget.IsBehind` / `IsFlanking`.
- Use True North (role action) to ignore positional requirements temporarily.

#### Party Composition and Targeting
- **Light party:** 4 players (1 tank, 1 healer, 2 DPS) in dungeons.
- **Full party:** 8 players (2 tanks, 2 healers, 4 DPS) in raids.
- Use `Group.CastableAlliesWithin30` for targeting party members within healing range.
- Check `Globals.InParty` to determine if grouped; `PartyManager.NumMembers` for party size.

#### AoE Thresholds
- FFXIV combat heavily revolves around target counts for AoE vs single-target priority.
- Most jobs switch to AoE rotation at **3+ enemies** (verified per job on The Balance Discord).
- Count enemies via `Combat.Enemies.Count(x => x.WithinSpellRange(range))`.
- Settings often expose `AoeEnemyCount` thresholds for user customization.

#### Role Actions
- **Tank:** Rampart, Provoke, Shirk, Reprisal, Interject, Low Blow, Arm's Length.
- **Healer:** Lucid Dreaming, Swiftcast, Esuna, Rescue, Surecast, Repose.
- **Melee DPS:** Second Wind, Bloodbath, Feint, Leg Sweep, Arm's Length, True North.
- **Physical Ranged DPS:** Second Wind, Bloodbath, Feint, Peloton, Head Graze, Foot Graze, Leg Graze, Arm's Length.
- **Magical Ranged DPS:** Addle, Swiftcast, Lucid Dreaming, Surecast.
- Role actions live in `Logic/Roles/` (Tank.cs, Healer.cs, PhysicalDps.cs, MagicDps.cs).

#### Combo Chains
- Many abilities require **previous abilities in sequence** to unlock or deal full damage.
- The game tracks combo state automatically; check `ActionManager.LastSpell` for combo tracking.
- Break if too much time passes (~30s) or if interrupted by another GCD.

#### Auras (Buffs and Debuffs)
- Use `Core.Me.HasAura(Auras.AuraName)` to check for buffs/debuffs.
- Aura IDs are centralized in `Utilities/Auras.cs`.
- Check `IsDebuff` property to distinguish debuffs from buffs.
- Many rotation decisions depend on aura presence (e.g., "cast this if target has DoT").

#### Tankbusters and Defensive Cooldowns
- **Tankbuster:** Heavy single-target attack on the tank requiring mitigation.
- **Raidwide AoE:** Damage to entire party requiring shields/healing.
- Boss-specific logic lives in `BossDictionary.json` with fight phases and mechanic timings.
- Defensive cooldowns are orchestrated via `CommonFightLogic` to avoid stacking inefficiently.

#### Enmity (Aggro) and Tank Stance
- Tanks use **tank stance** (Grit, Iron Will, Defiance, Royal Guard) to generate 10x enmity.
- Toggle stance via dedicated abilities; check `Core.Me.HasAura(Auras.Grit)` for stance status.
- Provoke instantly gives top enmity + extra; Shirk transfers enmity to another player.

#### Limit Break
- Party-wide resource that fills during combat; shared among all party members.
- Three bars maximum; effect scales with bar count.
- Tanks (mitigation), Healers (AoE heal), DPS (damage) have different LB effects.
- Check via `PartyManager.LimitBreakUnits` and cast with appropriate LB spell.

#### Resurrection Mechanics
- Healers and Red Mage can resurrect fallen party members in combat.
- Use Swiftcast for instant-cast resurrection (high priority).
- Without Swiftcast, resurrection has an ~8s cast time (risky during mechanics).

#### Movement and Slidecast
- Many spells have **cast times** and require standing still.
- **Slidecast window:** The last ~0.5s of a cast where you can move without interrupting.
- Instant-cast abilities (or oGCDs) can be used while moving.
- Respect `MovementManager.IsMoving` checks when casting abilities with cast times.

#### Range vs Radius (Critical Targeting Distinction)
- **`WithinSpellRange(range)`**: Edge-to-edge distance accounting for `CombatReach` of both player and target. Use for ALL range checks.
  ```csharp
  // Calculates: Distance2D - Core.Me.CombatReach - target.CombatReach <= range
  if (target.WithinSpellRange(25)) // 25y single-target spell
  
  // ALSO use for AoE enemy counting:
  var nearbyEnemies = Combat.Enemies.Count(r => r.WithinSpellRange(5)); // 5y AoE
  ```

- **`EnemiesNearby(distance)`**: Radius check from a point (player or target). Counts enemies within X yalms.
  ```csharp
  var nearbyEnemies = target.EnemiesNearby(5); // 5y radius around target
  ```

- **`EnemiesInCone(maxdistance)`**: Cone-shaped AoE (frontal attacks). Uses radians from player heading.
  ```csharp
  if (Core.Me.EnemiesInCone(8) >= 3) // Flamethrower, etc.
  ```

- **`spell.Radius`**: Some AoE spells have a Radius property (circular AoE around target).
  ```csharp
  var bestAoeTarget = Enemies.OrderBy(x => x.EnemiesNearby(spell.Radius).Count()).First();
  ```

- **`Distance(unit)` / `Distance2D(unit)`**: Raw center-to-center distance. Rarely used directly; prefer `WithinSpellRange`.

**Pattern:** 
- **ALWAYS use `WithinSpellRange`** for range checks (single-target AND AoE enemy counting)
- **NEVER use manual calculations** like `r.Distance(Core.Me) <= 5 + r.CombatReach`
- Use radius/cone checks for specialized AoE targeting
- Never use raw `Distance` for spell range checks

---

## Cached Collections and Utilities

Magitek maintains **performance-critical cached collections** that are refreshed each frame. **Always use these instead of reimplementing**:

### Combat.Enemies (`Utilities/Combat.cs`)
- **`Combat.Enemies`**: Cached list of valid combat targets (refreshed each frame).
  ```csharp
  var nearbyEnemies = Combat.Enemies.Count(x => x.WithinSpellRange(5));
  ```
- **Never** query `GameObjectManager` directly for enemies; use `Combat.Enemies`.

### Combat Timers
- **`Combat.CombatTime`**: Stopwatch tracking time in combat.
- **`Combat.OutOfCombatTime`**: Stopwatch tracking time out of combat.
- **`Combat.MovingInCombatTime`**: Stopwatch tracking time moving in combat.
- **`Combat.NotMovingInCombatTime`**: Stopwatch tracking time stationary in combat.

### Group Collections (`Utilities/Group.cs`)
Frame-cached party/alliance targeting helpers (updated via `Group.UpdateAllies()`):
- **`Group.CastableAlliesWithin30`**: All party members within 30y heal range.
- **`Group.CastableTanks`**: Tanks in party within range.
- **`Group.CastableHealers`**: Healers in party within range.
- **`Group.CastableDps`**: DPS in party within range.
- **`Group.RawPartyMembers`**: All party members (no range filter).
- Use these for healing/buffing targeting instead of manually filtering `PartyManager`.

### Globals (`Utilities/Globals.cs`)
Combat state flags (computed properties, very cheap):
- **`Globals.InParty`**: Are we in a party?
- **`Globals.PartyInCombat`**: Is any party member in combat?
- **`Globals.InActiveDuty`**: Are we in an active duty/instance?
- **`Globals.OnPvpMap`**: Are we on a PvP map?
- **`Globals.InSanctuaryOrSafeZone`**: Are we in a sanctuary (prevents instant buff casting)?
- **`Globals.HealTarget`**: Current heal target (set by rotation).
- **`Globals.AnimationLockMs`**: Animation lock duration (610-770ms, user-configurable).

### Helper Functions
- **`Combat.IsBoss()`**: Is current target a boss? (checks boss flag + "single enemy in duty").
- **`Combat.SmartAoeTarget(spell, setting)`**: Finds best AoE target by enemy density around `spell.Radius`.

**Performance Pattern:** These collections are frame-cached (via `FrameCachedObject`) or manually maintained. Querying `GameObjectManager` or `PartyManager` directly on every pulse is expensive—use the cached helpers.

---

## Testing & Validation

- Run `dotnet build` before handing work over; the solution does not currently ship automated tests.
- Exercise the routine in-game (training dummy + dungeon) when touching rotations or fight logic.
- For UI-only work, open the relevant `UserControl` in the XAML designer to confirm layout.

---

## Project Architecture

Magitek is a C# WPF MVVM application that drives FFXIV combat automation via RebornBuddy.
The `MagitekLoader` project is a lightweight RebornBuddy addon (`IAddonProxy<CombatRoutine>`) that auto-updates and loads the `Magitek` combat routine (`Magitek.Magitek` in `Magitek.dll`):
- Auto-update checks GitHub releases (`https://github.com/MagitekRB/MagitekRoutine/releases/latest/`) for `Magitek.zip` and `Version.txt`.
- Loads `Magitek.dll` dynamically via reflection, targeting the `Magitek.Magitek` type.
- Day-to-day development happens inside the `Magitek` project; MagitekLoader only requires changes if the loading mechanism changes.

### ViewModels (ViewModels/*/)
- Use the singleton pattern with lazy initialization: `private static SomeViewModel _instance; public static SomeViewModel Instance => _instance ?? (_instance = new SomeViewModel());`
- Add `[AddINotifyPropertyChangedInterface]` attribute (enables automatic INotifyPropertyChanged via PropertyChanged.Fody).
- Expose job settings as properties with **both** `{ get; set; }` (e.g., `public SageSettings SageSettings { get; set; } = SageSettings.Instance;`).
  - **Important:** Use `{ get; set; }` not `{ get; }` so PropertyChanged.Fody can inject change notifications for two-way binding.

### Settings (Models/*/)
- Inherit from the appropriate role base (`JsonSettings`, `HealerSettings`, `TankSettings`, etc.) and implement `IRoutineSettings`.
- Add `[AddINotifyPropertyChangedInterface]` attribute (inherited from role bases in most cases, but verify).
- Follow the singleton pattern with `Instance` property: `public static SomeSettings Instance { get; set; } = new SomeSettings();`
- Decorate every serialized property with `[Setting]` and `[DefaultValue(...)]` attributes.
- Group properties with `#region` blocks (`Combat`, `Buffs`, `Heals`, etc.).
- Constructor pattern:
  ```csharp
  public SomeSettings() : base(CharacterSettingsDirectory + "/Magitek/Some/SomeSettings.json") { }
  public static SomeSettings Instance { get; set; } = new SomeSettings();
  ```

### Logic (Logic/*/)
- Files are typically `internal static class`es exposing `static async Task<bool>` methods (or `static Task<bool>` for synchronous helpers).
- **Exception:** A few legacy classes (e.g., WhiteMage.Heal, HealFightLogic classes) are non-static instance classes; prefer the static pattern for all new logic files.
- Always check settings first, then spell availability, then cast. Return `true` only when the action fires; return `false` for all guard conditions.
- Keep logic split per topic (`SingleTarget.cs`, `AoE.cs`, `Buff.cs`, etc.).
- Use `Spells.*` helpers defined in `Utilities/Spells.cs` with extension methods from `Extensions/SpellDataExtensions.cs`: `IsKnownAndReady`, `Cast`, `CastAura`, etc.—never call `ActionManager.CanCast` directly.
- Guard early: settings → target validity → aura/CD gating → action.

### Job-Specific Shared Utilities (Utilities/Routines/<Job>.cs)

**CRITICAL:** When you need shared code that multiple Logic files will use, **DO NOT** put it at the top of a Logic file or duplicate it across files. Instead, use `Utilities/Routines/<Job>.cs`.

#### Purpose

`Utilities/Routines/<Job>.cs` contains **shared utility functions, cached variables, and helper calculations** that are used across multiple Logic files (`SingleTarget.cs`, `AoE.cs`, `Buff.cs`, etc.) and Rotations. This prevents code duplication and provides a centralized location for job-specific utilities.

#### What Belongs in Utilities/Routines/<Job>.cs

✅ **DO put these in Utilities/Routines/<Job>.cs:**
- **Cached variables** that need periodic refresh (e.g., `AoeEnemies5Yards`, `AoeEnemies30Yards`)
- **Helper calculation methods** that don't cast spells (e.g., `WillOvercapPolyglot()`, `MaxPolyglotCount`, `IsAurasForComboActive()`)
- **Static collections/arrays** used by multiple Logic files (e.g., `DefensiveSpells[]`, `Defensives[]`)
- **Level-based property calculations** (e.g., `MaxCartridge`, `HeartOfCorundum` based on level)
- **Shared state objects** (e.g., `GlobalCooldown` WeaveWindow instances)
- **Constants** specific to the job (e.g., item IDs like `Ether`, `HiEther`)
- **Refresh methods** for cached variables (e.g., `RefreshVars()`)

❌ **DO NOT put these in Utilities/Routines/<Job>.cs:**
- Spell-casting logic (belongs in `Logic/<Job>/`)
- Methods that return `Task<bool>` and actually cast spells
- Settings checks or spell availability checks (these belong in Logic files)

#### Usage Pattern

**1. Access via namespace alias in Logic files:**
```csharp
using BlackMageRoutine = Magitek.Utilities.Routines.BlackMage;

namespace Magitek.Logic.BlackMage
{
    internal static class SingleTarget
    {
        public static async Task<bool> Xenoglossy()
        {
            // Use the shared utility
            if (BlackMageRoutine.WillOvercapPolyglot())
                return await Spells.Xenoglossy.Cast(Core.Me.CurrentTarget);
            
            // ... rest of logic
        }
    }
}
```

**2. Access directly in Rotations:**
```csharp
using BlackMageRoutine = Magitek.Utilities.Routines.BlackMage;

namespace Magitek.Rotations
{
    public static class BlackMage
    {
        public static async Task<bool> PvP()
        {
            // Refresh cached variables before use
            BlackMageRoutine.RefreshVars();
            
            // ... rest of rotation
        }
    }
}
```

#### Example Structure

```csharp
namespace Magitek.Utilities.Routines
{
    internal static class BlackMage
    {
        // Cached variables (refreshed periodically)
        public static int AoeEnemies5Yards;
        public static int AoeEnemies30Yards;
        
        // Refresh method for cached variables
        public static void RefreshVars()
        {
            AoeEnemies5Yards = Combat.Enemies.Count(x => x.WithinSpellRange(5) && x.IsTargetable && x.IsValid && !x.HasAnyAura(Auras.Invincibility) && x.NotInvulnerable());
            AoeEnemies30Yards = Combat.Enemies.Count(x => x.WithinSpellRange(30) && x.IsTargetable && x.IsValid && !x.HasAnyAura(Auras.Invincibility) && x.NotInvulnerable());
        }
        
        // Helper calculation methods
        public static bool WillOvercapPolyglot()
        {
            if (PolyglotCount >= MaxPolyglotCount)
                return true;
            // ... calculation logic
        }
        
        // Level-based properties
        public static int MaxPolyglotCount
        {
            get
            {
                if (Core.Me.ClassLevel >= 98) return 3;
                if (Core.Me.ClassLevel >= 80) return 2;
                if (Core.Me.ClassLevel >= 70) return 1;
                return 0;
            }
        }
        
        // Constants
        public static readonly uint Ether = 4555;
        public static readonly uint HiEther = 4556;
    }
}
```

#### Common Mistakes to Avoid

❌ **BAD**: Duplicating helper code at the top of a Logic file
```csharp
namespace Magitek.Logic.BlackMage
{
    internal static class SingleTarget
    {
        // ❌ DON'T DO THIS - belongs in Utilities/Routines/BlackMage.cs
        private static int MaxPolyglotCount
        {
            get
            {
                if (Core.Me.ClassLevel >= 98) return 3;
                // ...
            }
        }
        
        public static async Task<bool> Xenoglossy()
        {
            // ...
        }
    }
}
```

✅ **GOOD**: Using the shared utility from Utilities/Routines
```csharp
using BlackMageRoutine = Magitek.Utilities.Routines.BlackMage;

namespace Magitek.Logic.BlackMage
{
    internal static class SingleTarget
    {
        public static async Task<bool> Xenoglossy()
        {
            // ✅ Use the shared utility
            if (BlackMageRoutine.WillOvercapPolyglot())
                return await Spells.Xenoglossy.Cast(Core.Me.CurrentTarget);
            // ...
        }
    }
}
```

#### When to Create or Update Utilities/Routines/<Job>.cs

- **Creating new shared utilities**: If you find yourself writing the same calculation or helper in multiple Logic files, move it to `Utilities/Routines/<Job>.cs`.
- **Adding cached variables**: If multiple Logic files need the same enemy count or state calculation, cache it in `Utilities/Routines/<Job>.cs` and refresh it in the Rotation's `PvP()` method or via a frame update.
- **Level-based calculations**: If you have level-dependent properties or spell selection logic, centralize it in `Utilities/Routines/<Job>.cs`.

**Summary:** `Utilities/Routines/<Job>.cs` is the **shared utilities layer** for job-specific code that multiple Logic files need. Always check if a utility already exists before duplicating code in Logic files.

### Spells (Utilities/Spells.cs)
- All spell definitions are centralized in `Magitek.Utilities.Spells` as `public static readonly SpellData` fields.
- Spells are organized by role and job using `#region` blocks (e.g., `#region WHM`, `#region DPS Role`).
- Spell data is retrieved via `DataManager.GetSpellData(spellId)`.
- Extension methods for spells (`IsKnownAndReady`, `Cast`, `CastAura`, etc.) live in `Extensions/SpellDataExtensions.cs`.
- When adding a new spell, add it to the appropriate region in `Spells.cs` and use the existing extension methods to cast it.
- **CRITICAL - Level Sync Compatibility**: When implementing spell logic, **use `Spells.SpellName.IsKnown()` (or `IsKnownAndReady()`)** for spell availability, especially when logic depends on other spells or auras. FFXIV level sync can make higher-level spells unavailable, causing rotation deadlocks. Use `LevelAcquired` only for pure level/trait checks (resource caps, trait unlocks), forced level sync simulation, or other-player level checks. See "Level Sync and Spell Dependencies" section below for detailed patterns and examples.

### Rotations (Rotations/*/)
- Job rotations are `public static` classes (e.g., `Magitek.Rotations.WhiteMage`) exposing the standard rotation methods (`Rest`, `PreCombatBuff`, `Pull`, `Heal`, `CombatBuff`, `Combat`, `PvP`) as `static Task<bool>`.
- The `IRotation` contract is implemented by `RotationComposites` (in `Utilities/Managers/RotationManager.cs`), which uses reflection to dynamically invoke job-specific rotation methods.
- Reflection dispatch: `RotationComposites.ExecuteRotationMethod` looks up methods by name (e.g., "Heal") from the job's rotation class (mapped via `RotationClassMap`). Reflected methods are cached in `MethodCache` for performance.
- **Critical:** Method names must exactly match `IRotation` interface method names (`Rest`, `PreCombatBuff`, `Pull`, `Heal`, `CombatBuff`, `Combat`, `PvP`). Methods must be `public static` and return `Task<bool>`.
- Each method is priority-based: `if (await Logic.SomeAction()) return true;`. Methods can be async (`async Task<bool>`) or synchronous (`Task<bool>` returning `Task.FromResult(false)`).
- Respect job toggles (e.g., `if (!WhiteMageSettings.Instance.DoDamage) return false;`).

### Embedded Resources (Resources/*/)
- The project embeds several JSON resources as `EmbeddedResource` entries in the csproj:
  - `ActionList.json`, `StatusList.json`: Spell/action and status effect data.
  - `BossDictionary.json`, `BossNames.json`: Boss fight metadata for encounter-specific logic.
  - `Toggles/*Toggles.json`: Job-specific toggle configurations (Bard, Dancer, Machinist, Dragoon, Reaper, Sage, Warrior, Pictomancer).
- These resources are loaded at runtime and should not be modified during normal development.
- When adding boss-specific logic, consult `BossDictionary.json` for boss IDs and fight phases.

---

## Rotation Integration Guidelines

- **Preserve priority guards**: Rotations rely on layered guard clauses (resource checks, aura windows, queued cooldowns). Reuse existing helpers when adding new abilities or offer opt-out settings so burst sequencing remains intact.
- **Reuse targeting/utilities**: Jobs expose helpers like `Group.CastableAlliesWithin30`, `Group.CastableTanks`, and `Utilities.Routines.[Job]` collections (see "Job-Specific Shared Utilities" section). Run new heals/defensives through those helpers instead of bespoke loops.
- **Honor defensive orchestration**: Tanks and defensive DPS features funnel through centralized helpers (`UseDefensives()`, `CommonFightLogic` methods). Always call them so limits such as "max simultaneous defensives" keep working.
- **Mind legacy layout debt**: Some XAML views still contain Grid-based layouts. Treat them as technical debt—keep StackPanel spacing for new UI and only refactor old Grids when already editing that block.

---

## XAML & UI Rules

- All job views live under `Magitek/Views/UserControls/<Job>/`.
- Pattern:
  ```xml
  <UserControl.DataContext>
      <Binding Source="{x:Static viewModels:BaseSettings.Instance}"/>
  </UserControl.DataContext>
  ```
  - `BaseSettings` is the root ViewModel that exposes all job-specific settings (e.g., `WhiteMageSettings`, `SageSettings`).
  - Bind to job settings via `{Binding WhiteMageSettings.PropertyName, Mode=TwoWay}`.
  
  ```xml
  <UserControl.Resources>
      <ResourceDictionary Source="/Magitek;component/Styles/Magitek.xaml"/>
  </UserControl.Resources>
  <ScrollViewer VerticalScrollBarVisibility="Auto">
      <StackPanel Margin="10">
          <controls:SettingsBlock ...>
              <!-- StackPanel content -->
          </controls:SettingsBlock>
      </StackPanel>
  </ScrollViewer>
  ```
- Use `controls:SettingsBlock` to group related options.
- **CRITICAL**: Default spacing uses StackPanels with `Margin="5"` wrapping each control. Use horizontal StackPanels (`Orientation="Horizontal"`) for checkbox + numeric pairs. See `.cursor/rules/xaml-spacing.mdc` for detailed examples.
- Grid layouts are legacy technical debt. Only use Grids when complex multi-column alignment is absolutely necessary; keep parent StackPanel margins and limit Grid scope to the specific complex row. Prefer StackPanels for all new settings blocks.

### Internationalization

- Never hardcode text. Reference strings via `{x:Static properties:Resources.ResourceName}`.
- When adding text, update both `Magitek/Properties/Resources.resx` (English) and `Resources.zh-CN.resx` (Chinese).
- The `Resources.Designer.cs` file is only auto-generated when run in visual studio, but when run in cursor/claude it needs to be manually added else the build fails with "Cannot find the static member" errors after adding resources:
  1. Open `Properties/Resources.Designer.cs`.
  2. Manually add the property in alphabetical order following the existing pattern.
  3. Rebuild to verify.
- Use `Generic_` prefixes for shared text; use `[JobName]_Content_` / `[JobName]_Text_` for job-specific phrases.

---

## Resource & Shared Component Management

- Follow `.cursor/rules/resource-management.mdc` for step-by-step localization updates.
- Shared UI (e.g., PvP utilities) belongs under `Views/UserControls/Common/` and should accept settings via binding or dependency properties. See `.cursor/rules/shared-components.mdc` for patterns.

---

## Spell Casting Patterns

- Always call the `Spells.*` helpers with extension methods:
  ```csharp
  if (!Spells.SomeSpell.IsKnownAndReady())
      return false;
  return await Spells.SomeSpell.Cast(target);
  ```
- Use aura helpers (`HasAnyAura`, `CastAura`) for buffs/DoTs.
- **Always check spell availability** using `Spells.SomeSpell.IsKnown()` (or `IsKnownAndReady()`) before assuming a spell exists. Use `LevelAcquired` only for pure level/trait checks, forced level sync simulation, or other-player level checks.
- Async methods should return `Task<bool>`; synchronous helpers return `Task.FromResult(false)`.

**When Adding New Spell Logic:**
- **Check spell dependencies**: If your spell logic assumes another spell exists, verify it with `IsKnown()` checks on the dependent spell.
- **Provide fallbacks**: Always have a fallback path when dependent spells aren't available (level sync scenarios).
- **Test at multiple levels**: Consider how the logic behaves at the spell's acquisition level, below it, and in level-synced content.
- See "Level Sync and Spell Dependencies" section for comprehensive patterns and examples.

### Extension Methods and Utilities

**CRITICAL:** Before implementing custom logic or manually accessing game objects, **always check extension methods** in the `Extensions/` folder. Many common operations have optimized, tested helper methods that handle edge cases and follow Magitek patterns.

#### Available Extension Namespaces

**`Magitek.Extensions.GameObjectExtensions`** - Extensions on `GameObject` and `Character`:
- **Aura Checks:**
  - `HasAura(uint spell, bool isMyAura = false, int msLeft = 0)`: Check if unit has aura with optional minimum time remaining
  - `HasAuraCharge(uint spell, bool isMyAura = false)`: Check if unit has aura with charge value
  - `HasAnyAura(uint[]/List<uint> auras, bool isMyAura = false, int msLeft = 0)`: Check if unit has any of the specified auras
  - `HasAllAuras(List<uint> auras, bool areMyAuras = false, int msLeft = 0)`: Check if unit has all specified auras
  - `CountAuras(List<uint> auras, bool isMyAura = false, int msLeft = 0)`: Count matching auras on unit
  - `HasDispellableAura()`: Check if unit has a dispellable debuff
- **Range and Distance:**
  - `WithinSpellRange(float/double range)`: Edge-to-edge distance check accounting for CombatReach (use for ALL range checks)
  - `EnemiesNearby(float distance)`: Get enemies within radius (uses cached Combat.Enemies)
  - `EnemiesNearbyOoc(float distance)`: Get enemies within radius (out of combat, uses GameObjectManager)
  - `EnemiesNearbyWithMyAura(float distance, uint aura)`: Get nearby enemies with your aura
- **Targeting and Validation:**
  - `ValidAttackUnit()`: Check if unit is a valid attack target
  - `NotInvulnerable()`: Check if unit is not invulnerable
  - `ThoroughCanAttack()`: Comprehensive attack validity check
  - `BeingTargeted()`: Check if unit is being targeted
  - `BeingTargetedBy(GameObject other)`: Check if unit is targeted by specific unit
- **Role Checks:**
  - `IsTank(bool mainTank = false)`: Check if unit is a tank
  - `IsMainTank()`: Check if unit is main tank
  - `IsHealer()`: Check if unit is a healer
  - `IsDps()`: Check if unit is DPS
  - `IsRangedPhysicalDps()`: Check if unit is ranged physical DPS
  - `IsMelee()`: Check if unit is melee
  - `IsRanged()`: Check if unit is ranged
- **Combat Timing:**
  - `TimeInCombat()`: Get time unit has been in combat
  - `CombatTimeLeft()`: Get remaining combat time for target dummy
- **Items:**
  - `UseItem(uint itemId, bool lookForMedicated = false)`: Use item on unit

**`Magitek.Extensions.SpellDataExtensions`** - Extensions on `SpellData`:
- `IsKnownAndReady(int ms = 0)`: Check if spell is known and ready (with optional time window)
- `Cast(GameObject target)`: Cast spell with proper error handling
- `CastAura(GameObject target, uint auraId)`: Cast spell and wait for aura application
- `Masked()`: Get actual ability that will execute (for combo/state-based abilities)
- `CooldownToNextCharge()`: Calculate time until next charge is available

**`Magitek.Extensions.CharacterExtensions`** - Extensions on `Character`:
- Character-specific helpers for targeting and validation

**`Magitek.Extensions.PlayerExtensions`** - Extensions on player character:
- Player-specific utilities and state checks

**`Magitek.Extensions.CollectionExtensions`** - Extensions on collections:
- Collection manipulation and filtering helpers

**`Magitek.Extensions.JobHelperExtensions`** - Job-specific helpers:
- Job-specific utility methods

**`Magitek.Extensions.PetSpellDataExtensions`** - Extensions for pet spells:
- Pet spell casting and validation helpers

**`Magitek.Extensions.XivDbItemExtensions`** - Extensions for FFXIV database items:
- Item data access and validation

#### Pattern: Check Extensions Before Manual Implementation

**❌ BAD**: Manually accessing auras and calculating time
```csharp
var noMercyAura = Core.Me.CharacterAuras.FirstOrDefault(r => r.Id == Auras.NoMercy);
if (noMercyAura != null)
{
    double timeRemaining = noMercyAura.TimespanLeft.TotalMilliseconds;
    double gcdsRemaining = timeRemaining / gcdDurationMs;
    if (gcdsRemaining >= 4)
        return false;
}
```

**✅ GOOD**: Using extension method with built-in time check
```csharp
double gcdDurationMs = Spells.KeenEdge.AdjustedCooldown.TotalMilliseconds;
int minTimeFor4Gcds = (int)(gcdDurationMs * 4);
if (Core.Me.HasAura(Auras.NoMercy, false, minTimeFor4Gcds))
    return false; // Has 4+ GCDs remaining
```

#### When to Check Extensions

**Always check extension methods when:**
- Checking for auras/buffs/debuffs (use `HasAura` with `msLeft` parameter)
- Validating spell readiness (use `IsKnownAndReady`)
- Checking distances/ranges (use `WithinSpellRange`, `EnemiesNearby`, `EnemiesInCone`)
- Accessing character auras or properties
- Performing common game object operations

**How to Find Extensions:**
1. Look in `Magitek/Extensions/` folder for relevant extension files
2. Check method signatures for overloads with additional parameters (e.g., `HasAura` with `msLeft`)
3. Review existing code in similar logic files for extension usage patterns
4. When in doubt, search the codebase for similar operations to see what extensions are used

**Benefits:**
- **Consistency**: All code uses the same helpers, reducing bugs
- **Performance**: Extensions often use cached collections or optimized queries
- **Maintainability**: Changes to game APIs only need updates in one place
- **Edge Cases**: Extensions handle null checks, validation, and edge cases automatically

### Level Sync and Spell Dependencies

**CRITICAL:** FFXIV's level sync system temporarily reduces player level in lower-level content, making higher-level spells unavailable. **Always check spell availability with `IsKnown()` when spells have dependencies** to prevent rotation deadlocks. Use `LevelAcquired` only for pure level/trait checks (e.g., resource caps or trait unlocks) or forced level sync simulation.

**This is especially important when:**
- Adding new spell implementations
- Implementing spell logic that depends on other spells
- Checking for auras that come from higher-level spells
- Calculating resource requirements based on available abilities
- Making rotation priority decisions based on spell availability

#### Level Sync System
- **Level Sync**: Players are temporarily reduced to a maximum level in dungeons/trials (e.g., level 90 player → level 50 in old content).
- **Spell Availability**: Only spells that are actually known at the synced level are usable (use `IsKnown()`).
- **Job Gauge Changes**: Max resources, ability upgrades, and combo paths change based on synced level.

#### When to Check IsKnown vs LevelAcquired

**MANDATORY: Check spell availability in these scenarios:**

1. **Spell logic depends on another spell existing:**
   ```csharp
   // BAD: Assumes DoubleDown exists
   if (Cartridge < 2) // Need 2 for Gnashing Fang + Double Down
       return false;
   
   // GOOD: Checks if DoubleDown is available
   bool hasDoubleDown = Spells.DoubleDown.IsKnown();
   int requiredCartridges = hasDoubleDown ? 2 : 1; // Adjust requirement
   ```

2. **Rotation priority relies on spell combinations:**
   ```csharp
   // BAD: Holds GCD for a spell that might not exist
   if (Spells.HighLevelSpell.IsKnownAndReady())
       return false; // Deadlock at low levels!
   
   // GOOD: Only holds if spell is actually available
   bool hasHighLevelSpell = Spells.HighLevelSpell.IsKnown();
   if (hasHighLevelSpell && Spells.HighLevelSpell.IsKnownAndReady())
       return false;
   ```

3. **Resource spending depends on available spenders:**
   ```csharp
   // BAD: Prevents combo completion waiting for unavailable AoE spender
   if (Cartridge >= MaxCartridge) // Can't finish combo - deadlock!
       return false;
   
   // GOOD: Allow combo if AoE spender doesn't exist
   bool hasFatedCircle = Spells.FatedCircle.IsKnown();
   if (Cartridge >= MaxCartridge && hasFatedCircle)
       return false; // Only block if we have the spender
   ```

4. **Auras from higher-level spells:**
   ```csharp
   // BAD: Checks for aura from spell that doesn't exist
   if (Core.Me.HasAura(Auras.HighLevelBuff))
       return await SomeAction();
   
   // GOOD: Check level first
   if (Spells.HighLevelBuff.IsKnown() && 
       Core.Me.HasAura(Auras.HighLevelBuff))
       return await SomeAction();
   ```

#### Common Patterns

**Pattern 1: Ability Upgrade Chains**
```csharp
// Gunbreaker: DoubleDown (90) requires Gnashing Fang (60)
// If we have DoubleDown, we by definition have Gnashing Fang
bool hasDoubleDown = Spells.DoubleDown.IsKnown();
bool hasGnashingFang = Spells.GnashingFang.IsKnown();

if (hasDoubleDown)
    requiredCartridges = 2; // Have both
else if (hasGnashingFang)
    requiredCartridges = 1; // Only Gnashing Fang
else
    requiredCartridges = 0; // Neither available
```

**Pattern 2: AoE vs Single-Target Split**
```csharp
// Only prefer AoE spender if it exists
int enemyCount = Combat.Enemies.Count(r => r.WithinSpellRange(5));
bool hasAoeSpender = Spells.AoeSpender.IsKnown();

// If AoE spender exists, skip single-target spender in AoE
if (enemyCount >= AoeThreshold && hasAoeSpender)
    return false; // Use AoE spender instead

// Otherwise allow single-target spender in AoE (fallback)
return await Spells.SingleTargetSpender.Cast(target);
```

**Pattern 3: Resource Cap Prevention**
```csharp
// Don't block combo if resource spender doesn't exist
bool hasResourceSpender = Spells.ResourceSpender.IsKnown();

// At max resources, prevent overcapping ONLY if spender exists
if (Resource >= MaxResource && hasResourceSpender)
    return false; // Let spender use resources first

// Otherwise allow combo to complete (prevents deadlock)
return await Spells.ComboFinisher.Cast(target);
```

**Pattern 4: Conditional Ability Availability**
```csharp
// Use level-appropriate spell variant
public static SpellData GetBestDefensive()
{
    // Return highest-level version available
      if (Spells.UpgradedDefensive.IsKnown())
          return Spells.UpgradedDefensive;
      else if (Spells.BasicDefensive.IsKnown())
          return Spells.BasicDefensive;
    else
        return null; // No defensive available
}
```

#### Real-World Example: Gunbreaker No Mercy

**Problem:** At level 90, player has 2 cartridges and No Mercy ready, but rotation deadlocked:
- Can't cast No Mercy (needs GCD to create weave window)
- Can't cast GCD (waiting for No Mercy to use cartridges)

**Root Cause:** Logic assumed DoubleDown existed, requiring 2 cartridges for No Mercy. At level 90, DoubleDown exists but might not in synced content.

**Solution:**
```csharp
// Cartridge requirement depends on available burst abilities
bool hasDoubleDown = Spells.DoubleDown.IsKnown();
bool hasGnashingFang = Spells.GnashingFang.IsKnown();

int requiredCartridges = 0;

if (enemyCount >= AoeThreshold)
{
    // AoE: Only need cartridges if DoubleDown is available
    if (hasDoubleDown)
        requiredCartridges = 1;
}
else
{
    // Single-target: Adjust based on available abilities
    if (hasDoubleDown)
        requiredCartridges = 2; // Both Gnashing Fang and DoubleDown
    else if (hasGnashingFang)
        requiredCartridges = 1; // Only Gnashing Fang
    // If neither available, use Burst Strike or other fillers
}

if (Cartridge < requiredCartridges)
    return false;
```

#### Testing Level Sync

**Manual Testing:**
- Test rotations in level-synced content (low-level dungeons/trials)
- Verify no deadlocks at:
  - Job start level (30 for most jobs, 60/70 for expansion jobs)
  - Major ability unlock levels (50, 60, 70, 80, 90, 100)
  - Between major ability unlocks

**Common Deadlock Scenarios:**
- Holding GCD for spell that doesn't exist
- Blocking resource generation when resource spender unavailable
- Requiring resources for non-existent abilities
- Checking auras from spells above current level

**Summary:** Always use `IsKnown()` (or `IsKnownAndReady()`) checks when implementing spell dependencies. Provide fallback logic for lower levels to prevent rotation deadlocks in level-synced content. Use `LevelAcquired` only for trait/level gating or forced sync simulation.

#### Checklist for New Spell Implementations

When adding a new spell or modifying spell logic, verify:

- [ ] **Dependency checks**: If the spell requires another spell to be effective, check `IsKnown()` for that dependency
- [ ] **Aura checks**: If checking for auras from higher-level spells, verify the spell exists first
- [ ] **Resource calculations**: If calculating resource requirements based on available abilities, check each ability's `IsKnown()`
- [ ] **Priority logic**: If rotation priority changes based on spell availability, check `IsKnown()` before making decisions
- [ ] **Fallback paths**: Ensure there's always a valid action path even when dependencies are unavailable
- [ ] **Level boundaries**: Test logic at key level boundaries (spell acquisition level, level sync points)

**Example Implementation Checklist:**
```csharp
// ✅ GOOD: Checks dependency before using
bool hasUpgrade = Spells.UpgradedAbility.IsKnown();
if (hasUpgrade && Spells.UpgradedAbility.IsKnownAndReady())
    return await Spells.UpgradedAbility.Cast(target);
else
    return await Spells.BasicAbility.Cast(target); // Fallback

// ❌ BAD: Assumes upgrade exists
if (Spells.UpgradedAbility.IsKnownAndReady())
    return await Spells.UpgradedAbility.Cast(target);
// No fallback - deadlock if upgrade unavailable!
```

### Masked Actions (Combo and State-Based Abilities)

The `.Masked()` extension method returns the **actual ability** that will execute when a hotbar button is pressed, accounting for combo state, job gauge upgrades, and ability replacements:

```csharp
public static SpellData Masked(this SpellData spell)
{
    return ActionManager.GetMaskedAction(spell.Id);
}
```

**When to use `Masked()`:**

1. **Combo chains**: Abilities that change during combo sequences
   ```csharp
   // Summoner: Gemshine becomes Ruby Rite, Topaz Rite, or Emerald Rite
   var gemshine = Spells.Gemshine.Masked();
   if (gemshine == Spells.RubyRite) { /* AoE logic */ }
   ```

2. **Job gauge upgrades**: Buttons that transform based on job state
   ```csharp
   // Pictomancer: Motif buttons change based on available motifs
   var creature = Spells.CreatureMotif.Masked();
   if (creature.IsKnownAndReady()) return await creature.Cast(Core.Me);
   ```

3. **PvP role actions**: Role action button changes based on selected action
   ```csharp
   // Check which role action is currently selected
   var selectedAction = Spells.PvPRoleAction.Masked();
   ```

4. **Validating spell is base form**: Check if an ability is ready to use (not replaced)
   ```csharp
   // Warrior: Ensure Blota is not upgraded to Primal Wrath
   if (Spells.BlotaPvp.Masked() != Spells.BlotaPvp)
       return false; // Blota is replaced, don't use
   ```

**Pattern:** Use `Masked()` when you need to know which ability will **actually execute** when the button is pressed, not just which button ID is being checked. Store the result in a variable if checking it multiple times.

### SpellData Timing Properties (Cooldown, AdjustedCooldown, AdjustedCastTime)

SpellData exposes several timing-related properties that serve different purposes. Understanding when to use each is critical for accurate rotation logic:

#### Cooldown (TimeSpan)
**Purpose:** The **remaining time** until the spell is available to cast again.

- **Dynamic value** that counts down in real-time from the full cooldown to zero.
- Always represents "time left" not "total cooldown duration".
- Returns `TimeSpan.Zero` when the spell is ready to use.

**Usage:** Checking if a spell is ready or almost ready, measuring time remaining on cooldowns, coordinating ability timing.

```csharp
// Check if a spell is off cooldown
if (Spells.NoMercy.Cooldown > TimeSpan.Zero)
    return false; // Still on cooldown

// Get remaining cooldown time in seconds
double noMercyCooldown = Spells.NoMercy.Cooldown.TotalSeconds;

// Hold ability if another cooldown is coming up soon
if (Spells.Bloodfest.Cooldown.TotalMilliseconds >= 118000)
    return false; // Bloodfest has more than 118s remaining
```

**Best Practice:** Use the `IsReady()` or `IsKnownAndReady()` extension methods instead of checking `Cooldown` directly for standard readiness checks:
```csharp
if (Spells.NoMercy.IsKnownAndReady())
    return await Spells.NoMercy.Cast(Core.Me.CurrentTarget);
```

#### AdjustedCooldown (TimeSpan)
**Purpose:** The **base cooldown duration** of the spell adjusted for the player's current Skill Speed or Spell Speed stats.

- **Static value** (for current stat set) representing the full cooldown duration the spell will have when cast.
- Accounts for stat modifiers (Skill Speed for weaponskills, Spell Speed for spells).
- Does **not** represent time remaining—it's the total duration.

**Usage:** Calculating GCD speed, planning ability timing windows, coordinating burst sequences, charge-based spell calculations.

```csharp
// Get current GCD speed (adjusted for Skill Speed)
double gcdSpeed = Spells.KeenEdge.AdjustedCooldown.TotalSeconds;

// Get Gnashing Fang's actual cooldown accounting for Skill Speed
double gnashingFangCooldown = Spells.GnashingFang.AdjustedCooldown.TotalSeconds;

// Calculate opener timing based on GCD count
if (Combat.CombatTime.ElapsedMilliseconds < 
    Spells.SpinningEdge.AdjustedCooldown.TotalMilliseconds * OpenerBurstAfterGCD - 770)
    return false;
```

**Critical for Charge-Based Spells:**
For spells with multiple charges (e.g., `MaxCharges > 1`), the `CooldownToNextCharge()` helper uses both `Cooldown` (remaining) and `AdjustedCooldown` (per-charge duration) to calculate when the next charge will be available:

```csharp
// From SpellDataExtensions.cs
var remainingCooldownTime = spell.Cooldown.TotalMilliseconds - 
    (spell.AdjustedCooldown.TotalMilliseconds * (spell.MaxCharges - (uint)Math.Floor(spell.Charges) - 1));
```

#### Charges (float)
**Purpose:** The **current number of charges available** for charge-based spells, represented as a floating-point value.

- **Floating-point format**: The integer portion represents the number of full charges available, while the decimal portion represents the **percentage progress into the next charge** (0.0 to 0.99...).
- **Similar to Cooldown/Milliseconds**: Just as `Cooldown` represents remaining time as a `TimeSpan`, `Charges` represents charge availability as a float where the decimal portion is like a percentage-based countdown to the next charge.
- **Dynamic value** that updates in real-time as charges regenerate.

**Usage:** Checking available charges, determining if a charge-based spell can be used, calculating charge regeneration progress.

```csharp
// Check if spell has at least one full charge available
if (spell.Charges >= 1.0f)
    return await spell.Cast(target);

// Check if spell has at least one charge (including partial)
if (spell.Charges > 0.0f)
    return await spell.Cast(target);

// Get the number of full charges (integer portion)
uint fullCharges = (uint)Math.Floor(spell.Charges);

// Get the progress toward next charge (decimal portion, 0.0 to 0.99...)
double chargeProgress = spell.Charges - Math.Floor(spell.Charges);

// Example: If Charges = 1.75, that means:
// - 1 full charge is available
// - 75% progress toward the next charge (2nd charge)
```

**Important Notes:**
- Always use `Math.Floor()` or cast to integer when you need the count of full charges available.
- The decimal portion (0.0 to 0.99...) represents how close you are to gaining the next charge, similar to how `Cooldown` counts down to zero.

#### AdjustedCastTime (TimeSpan)
**Purpose:** The **cast time** of the spell adjusted for the player's current Spell Speed stat.

- **Static value** (for current stat set) representing how long the spell takes to cast.
- Returns `TimeSpan.Zero` for instant-cast spells or abilities.
- Accounts for Spell Speed stat modifiers (does not account for Swiftcast or other instant-cast buffs).

**Usage:** Determining if a spell can be cast while moving, calculating precast timing windows, validating slidecasting.

```csharp
// Check if spell can be cast while moving
if (MovementManager.IsMoving && spell.AdjustedCastTime > TimeSpan.Zero && !Core.Me.HasAura(Auras.Swiftcast))
    return false;

// Calculate precast timing for motif abilities
var castTime = Spells.CreatureMotif.AdjustedCastTime.TotalMilliseconds;
var precastCooldown = (castTime + Globals.AnimationLockMs + BaseSettings.Instance.UserLatencyOffset) / 
    Spells.PomMuse.AdjustedCooldown.TotalMilliseconds;

// Set spell cast time tracking
Casting.SpellCastTime = spell.AdjustedCastTime;
```

#### Quick Reference Table

| Property | Type | Purpose | When to Use |
|----------|------|---------|-------------|
| **Cooldown** | TimeSpan | Time **remaining** until ready (dynamic) | Check if spell is ready now or soon |
| **AdjustedCooldown** | TimeSpan | **Total duration** of cooldown (static*) | Calculate GCD speed, plan timing windows |
| **AdjustedCastTime** | TimeSpan | **Cast time** duration (static*) | Check if instant-cast, plan precasts |

*Static for the current stat set; changes only when Skill Speed/Spell Speed stats change.

#### Common Patterns

**Checking GCD availability for weaving:**
```csharp
// Check if we're in late weave window
if (gcd.Cooldown.TotalMilliseconds <= (Globals.AnimationLockMs + BaseSettings.Instance.UserLatencyOffset))
    return true; // Late weave window

// Check if spell fits in weave window
bool spellFitsInWindow = gcd.Cooldown.TotalMilliseconds <= 
    gcd.AdjustedCooldown.TotalMilliseconds - (targetWeaveCount * Globals.AnimationLockMs + BaseSettings.Instance.UserLatencyOffset);
```

**Coordinating burst windows with cooldown alignment:**
```csharp
// Hold Gnashing Fang if No Mercy will be ready before it comes off cooldown
double noMercyCooldown = Spells.NoMercy.Cooldown.TotalSeconds; // Remaining time
double gnashingFangCooldown = Spells.GnashingFang.AdjustedCooldown.TotalSeconds; // Full cooldown duration

if (noMercyCooldown > 0 && noMercyCooldown < gnashingFangCooldown)
    return false; // Hold Gnashing Fang
```

**Validating instant-cast spells:**
```csharp
// Check if we can cast while running from avoidance
if (AvoidanceManager.IsRunningOutOfAvoid && 
    !(Core.Me.HasAura(Auras.Swiftcast) || spell.AdjustedCastTime <= TimeSpan.Zero))
{
    return false; // Can't cast while moving
}
```

---

## Configuration and Hardcoded Values

**Critical Rule:** Almost all numeric values and thresholds in logic should be configurable via settings with proper UI and internationalization.

### When Configuration is Required

Hardcoded numbers **MUST** become config options unless there are very specific circumstances justifying hardcoding. Create configuration options for:

1. **Thresholds and Conditions**
   - Health percentages for healing/defensive abilities
   - Enemy counts for AoE vs single-target decisions
   - Resource amounts (MP, gauge resources) for ability usage
   - Distance/range checks beyond standard spell ranges
   - Time-based conditions (buff duration, cooldown windows)

2. **Priority and Strategy**
   - Which targets to prioritize (tanks, healers, DPS, self)
   - Burst window timing and coordination
   - Defensive cooldown thresholds
   - Buff/debuff application priorities

3. **User Preferences**
   - Enable/disable toggles for specific abilities
   - Combo route preferences
   - Movement and positioning behavior
   - Smart-casting options

**Implementation Pattern:**

When adding a configurable value, you must:

1. **Add to Settings Model** with `[Setting]` and `[DefaultValue]` attributes:
   ```csharp
   [Setting]
   [DefaultValue(75f)]
   public float SomeAbilityHealthPercent { get; set; }
   ```

2. **Add UI Controls** in the appropriate XAML view using StackPanel spacing:
   ```xml
   <StackPanel Orientation="Horizontal" Margin="5">
       <CheckBox Content="{x:Static properties:Resources.Job_UseSomeAbility}"
                 IsChecked="{Binding JobSettings.UseSomeAbility, Mode=TwoWay}"
                 Margin="5"/>
       <controls:Numeric Value="{Binding JobSettings.SomeAbilityHealthPercent, Mode=TwoWay}"
                        Margin="5" MaxValue="100" MinValue="1" />
   </StackPanel>
   ```

3. **Add Localized Strings** to both `Resources.resx` and `Resources.zh-CN.resx`:
   ```xml
   <!-- Resources.resx -->
   <data name="Job_UseSomeAbility" xml:space="preserve">
     <value>Use Some Ability Below HP%:</value>
   </data>
   
   <!-- Resources.zh-CN.resx -->
   <data name="Job_UseSomeAbility" xml:space="preserve">
     <value>在血量低于 HP% 时使用某技能：</value>
   </data>
   ```

4. **Use in Logic** with clear setting checks:
   ```csharp
   if (!JobSettings.Instance.UseSomeAbility)
       return false;
       
   if (target.CurrentHealthPercent > JobSettings.Instance.SomeAbilityHealthPercent)
       return false;
   ```

### When Hardcoding is Acceptable

Only hardcode values in these specific circumstances:

1. **Game Constants**: Fixed game mechanics that never change
   ```csharp
   // Global Cooldown base duration
   const double BaseGCD = 2.5;
   
   // Maximum party size
   if (PartyManager.NumMembers == 8) // Full party
   ```

2. **Spell IDs and Aura IDs**: Always retrieved from game data
   ```csharp
   public static readonly SpellData Stone = DataManager.GetSpellData(119);
   ```

3. **API-Defined Values**: Values dictated by RebornBuddy/FFXIV APIs
   ```csharp
   if (ActionManager.ComboTimeLeft <= 0) // Combo expired
   ```

4. **Internal Implementation Details**: Non-user-facing calculations
   ```csharp
   // Animation lock buffer for queuing (internal timing)
   const int AnimationLockBuffer = 100;
   ```

5. **Buff Prerequisite Checks**: Checking for required self-buffs (e.g., damage buffs, speed buffs) before executing abilities is acceptable without config options
   ```csharp
   // Check for required buffs before spending resources
   if (!Core.Me.HasAura(Auras.Jinpu) || !Core.Me.HasAura(Auras.Shifu))
       return false; // Don't use expensive abilities without buffs active
   ```

**Note on Animation Lock Timing**: Always use `Globals.AnimationLockMs` instead of hardcoded values (e.g., 770). `Globals.AnimationLockMs` is user-configurable (610-770ms range) and ensures consistency across all rotations.

**Anti-Pattern Examples:**

❌ **BAD**: Hardcoded threshold with no config option
```csharp
if (Core.Me.CurrentHealthPercent < 30)
    return await Spells.Benediction.Cast(Core.Me);
```

✅ **GOOD**: Configurable threshold
```csharp
if (!WhiteMageSettings.Instance.UseBenediction)
    return false;
    
if (Core.Me.CurrentHealthPercent > WhiteMageSettings.Instance.BenedictionHealthPercent)
    return false;
    
return await Spells.Benediction.Cast(Core.Me);
```

❌ **BAD**: Hardcoded AoE count
```csharp
if (Combat.Enemies.Count(x => x.Distance() <= 5) >= 3)
    return await Spells.HolyCircle.Cast(Core.Me);
```

✅ **GOOD**: Configurable AoE count with proper distance check
```csharp
if (!DarkKnightSettings.Instance.UseHolyCircle)
    return false;
    
if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.HolyCircle.Range)) < DarkKnightSettings.Instance.AoeEnemyCount)
    return false;
    
return await Spells.HolyCircle.Cast(Core.Me);
```

**Summary:** If you find yourself typing a number that affects behavior, ask "should a user be able to change this?" If the answer is yes (or even maybe), create a proper config option with UI and i18n.

---

## Common Mistakes to Avoid

1. **Hardcoding English text** in XAML or forgetting Chinese translations.
2. **Hardcoding numeric thresholds** without creating config options, UI controls, and i18n (see "Configuration and Hardcoded Values" section).
3. Using **Grid layouts** for new settings blocks instead of the StackPanel spacing pattern.
4. **Skipping settings checks** before executing logic.
5. **Casting spells** without verifying `IsKnownAndReady`.
6. Making logic classes **non-static/public** or omitting `[AddINotifyPropertyChangedInterface]` on settings/viewmodels.
7. Forgetting to set `DataContext` and resource dictionaries on new views.
8. Calling `ActionManager.CanCast` directly instead of spell extensions.
9. Using `{ get; }` instead of `{ get; set; }` for ViewModel job settings properties (breaks PropertyChanged.Fody two-way binding).
10. Forgetting `[Setting]` attribute on serialized Settings properties (required alongside `[DefaultValue]`).
11. Using C# 11+ features (project targets C# 10).
12. Modifying embedded JSON resources directly instead of using boss fight dictionaries.
13. Using `Masked()` results without checking for null or storing in a variable for repeated checks.
14. **Duplicating shared code** in Logic files instead of using `Utilities/Routines/<Job>.cs` for helper functions, cached variables, and calculations used by multiple Logic files (see "Job-Specific Shared Utilities" section).

**Note on Commented Code**: Temporarily commented code for testing/debugging purposes is acceptable. However, before submitting a PR for review, consider whether the commented code should be removed (if it's obsolete) or uncommented (if it's needed). Use descriptive comments to explain why code is temporarily commented.

---
