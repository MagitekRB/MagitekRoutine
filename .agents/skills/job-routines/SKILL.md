---
name: job-routines
description: Where job-specific shared code belongs in Magitek — cached enemy counts, level-based property calculations, helper methods used by more than one Logic file. Use before writing any helper, constant, or calculation in a Logic file (it may already exist in Utilities/Routines/<Job>.cs), when tempted to put a calculation at the top of a Logic file, or when adding cached variables for a rotation.
---

# Job-Specific Shared Utilities

**When you need shared code that multiple Logic files will use, do not put it at the top of a Logic file or duplicate it across files.** It goes in `Utilities/Routines/<Job>.cs`.

## Purpose

`Utilities/Routines/<Job>.cs` contains shared utility functions, cached variables, and helper calculations used across multiple Logic files (`SingleTarget.cs`, `AoE.cs`, `Buff.cs`) and Rotations. It prevents duplication and gives job-specific utilities one home.

Check whether a utility already exists here before writing a new one.

## What belongs here

**Do put these in `Utilities/Routines/<Job>.cs`:**

- **Cached variables** that need periodic refresh (`AoeEnemies5Yards`, `AoeEnemies30Yards`)
- **Helper calculation methods** that don't cast spells (`WillOvercapPolyglot()`, `MaxPolyglotCount`, `IsAurasForComboActive()`)
- **Static collections and arrays** used by multiple Logic files (`DefensiveSpells[]`, `Defensives[]`)
- **Level-based property calculations** (`MaxCartridge`, `HeartOfCorundum` by level)
- **Shared state objects** (`GlobalCooldown` WeaveWindow instances)
- **Constants** specific to the job (item IDs like `Ether`, `HiEther`)
- **Refresh methods** for cached variables (`RefreshVars()`)

**Do not put these here:**

- Spell-casting logic — that belongs in `Logic/<Job>/`
- Methods returning `Task<bool>` that actually cast spells
- Settings checks or spell availability checks — those belong in Logic files

## Usage

**In Logic files, via a namespace alias:**

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

**Directly in Rotations:**

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

## Example structure

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

## Common mistake

**Bad** — duplicating helper code at the top of a Logic file:

```csharp
namespace Magitek.Logic.BlackMage
{
    internal static class SingleTarget
    {
        // Belongs in Utilities/Routines/BlackMage.cs
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

**Good** — using the shared utility:

```csharp
using BlackMageRoutine = Magitek.Utilities.Routines.BlackMage;

namespace Magitek.Logic.BlackMage
{
    internal static class SingleTarget
    {
        public static async Task<bool> Xenoglossy()
        {
            if (BlackMageRoutine.WillOvercapPolyglot())
                return await Spells.Xenoglossy.Cast(Core.Me.CurrentTarget);
            // ...
        }
    }
}
```

## When to create or update one

- **New shared utility**: if you find yourself writing the same calculation in more than one Logic file, move it here.
- **Cached variables**: if multiple Logic files need the same enemy count or state calculation, cache it here and refresh it in the Rotation's `PvP()` method or via a frame update.
- **Level-based calculations**: centralize level-dependent properties and spell selection here.
