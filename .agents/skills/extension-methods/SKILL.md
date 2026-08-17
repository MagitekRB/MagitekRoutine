---
name: extension-methods
description: Catalog of Magitek's extension methods for auras, ranges, targeting, role checks, and spell readiness. Use before adding any aura, distance, enemy-count, targeting, role, readiness, or timing check — and before querying CharacterAuras, GameObjectManager, PartyManager, or ActionManager directly — to see whether a tested helper already exists.
---

# Extension Methods

**Before implementing custom logic or manually accessing game objects, always check the extension methods** in `Magitek/Extensions/`. Many common operations already have optimized, tested helpers that handle edge cases and follow Magitek patterns.

## Available extension namespaces

**`Magitek.Extensions.GameObjectExtensions`** — extensions on `GameObject` and `Character`:

- **Aura checks:**
  - `HasAura(uint spell, bool isMyAura = false, int msLeft = 0)`: Check if unit has aura with optional minimum time remaining
  - `HasAuraCharge(uint spell, bool isMyAura = false)`: Check if unit has aura with charge value
  - `HasAnyAura(uint[]/List<uint> auras, bool isMyAura = false, int msLeft = 0)`: Check if unit has any of the specified auras
  - `HasAllAuras(List<uint> auras, bool areMyAuras = false, int msLeft = 0)`: Check if unit has all specified auras
  - `CountAuras(List<uint> auras, bool isMyAura = false, int msLeft = 0)`: Count matching auras on unit
  - `HasDispellableBuff()`: Check if an enemy carries a dispellable beneficial status (what a dispel can actually strip)
- **Range and distance:**
  - `WithinSpellRange(float/double range)`: Edge-to-edge distance check accounting for CombatReach (use for ALL range checks)
  - `EnemiesNearby(float distance)`: Get enemies within radius (uses cached Combat.Enemies)
  - `EnemiesNearbyOoc(float distance)`: Get enemies within radius (out of combat, uses GameObjectManager)
  - `EnemiesNearbyWithMyAura(float distance, uint aura)`: Get nearby enemies with your aura
- **Targeting and validation:**
  - `ValidAttackUnit()`: Check if unit is a valid attack target
  - `NotInvulnerable()`: Check if unit is not invulnerable
  - `ThoroughCanAttack()`: Comprehensive attack validity check
  - `BeingTargeted()`: Check if unit is being targeted
  - `BeingTargetedBy(GameObject other)`: Check if unit is targeted by specific unit
- **Role checks:**
  - `IsTank(bool mainTank = false)`: Check if unit is a tank
  - `IsMainTank()`: Check if unit is main tank
  - `IsHealer()`: Check if unit is a healer
  - `IsDps()`: Check if unit is DPS
  - `IsRangedPhysicalDps()`: Check if unit is ranged physical DPS
  - `IsMelee()`: Check if unit is melee
  - `IsRanged()`: Check if unit is ranged
- **Combat timing:**
  - `TimeInCombat()`: Get time unit has been in combat
  - `CombatTimeLeft()`: Get remaining combat time for target dummy
- **Items:**
  - `UseItem(uint itemId, bool lookForMedicated = false)`: Use item on unit

**`Magitek.Extensions.SpellDataExtensions`** — extensions on `SpellData`:

- `IsKnownAndReady(int ms = 0)`: Check if spell is known and ready (with optional time window)
- `Cast(GameObject target)`: Cast spell with proper error handling
- `CastAura(GameObject target, uint auraId)`: Cast spell and wait for aura application
- `Masked()`: Get actual ability that will execute (for combo/state-based abilities)
- `CooldownToNextCharge()`: Calculate time until next charge is available

**`Magitek.Extensions.CharacterExtensions`** — character-specific helpers for targeting and validation.

**`Magitek.Extensions.PlayerExtensions`** — player-specific utilities and state checks.

**`Magitek.Extensions.CollectionExtensions`** — collection manipulation and filtering helpers.

**`Magitek.Extensions.JobHelperExtensions`** — job-specific utility methods.

**`Magitek.Extensions.PetSpellDataExtensions`** — pet spell casting and validation helpers.

**`Magitek.Extensions.XivDbItemExtensions`** — item data access and validation.

## Pattern: check extensions before manual implementation

**Bad** — manually accessing auras and calculating time:

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

**Good** — using the extension method with its built-in time check:

```csharp
double gcdDurationMs = Spells.KeenEdge.AdjustedCooldown.TotalMilliseconds;
int minTimeFor4Gcds = (int)(gcdDurationMs * 4);
if (Core.Me.HasAura(Auras.NoMercy, false, minTimeFor4Gcds))
    return false; // Has 4+ GCDs remaining
```

## When to check

**Always check extension methods when:**

- Checking for auras, buffs, or debuffs (use `HasAura` with the `msLeft` parameter)
- Validating spell readiness (use `IsKnownAndReady`)
- Checking distances or ranges (use `WithinSpellRange`, `EnemiesNearby`, `EnemiesInCone`)
- Accessing character auras or properties
- Performing common game object operations

**How to find them:**

1. Look in `Magitek/Extensions/` for the relevant extension file.
2. Check method signatures for overloads with extra parameters — `HasAura` with `msLeft` is the one most often missed.
3. Review existing code in similar logic files for usage patterns.
4. When in doubt, search the codebase for the same operation to see which extension it uses.

**Why it matters:**

- **Consistency**: all code uses the same helpers, which reduces bugs.
- **Performance**: extensions often use cached collections or optimized queries.
- **Maintainability**: changes to game APIs only need updating in one place.
- **Edge cases**: extensions handle null checks and validation automatically.
