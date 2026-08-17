---
name: fightlogic
description: Add or modify encounters in FightLogicEncounters.cs — zones, bosses, and their tankbusters, AoEs, and knockbacks. Use when touching FightLogicEncounters.cs at all, adding boss or dungeon fight logic, converting encounter JSON, changing an existing encounter's mechanics, or making Magitek react to a specific fight.
---

# FightLogic Encounters

Adds an encounter to `Magitek/Utilities/FightLogicEncounters.cs` from JSON input.

## Input format

```json
{
    "ZoneId": 1314,
    "Name": "Mistwake",
    "Expansion": "Dawntrail",
    "EncounterType": "Dungeons",
    "Enemies": [
        {
            "Id": 14270,
            "Name": "Treno Catoblepas",
            "TankBusters": [43329],
            "Aoes": [43327, 43331, 44825],
            "AoeLockOns": null,
            "Knockbacks": null,
            "SharedTankBusters": null,
            "BigAoes": null
        }
    ]
}
```

## Key rules

1. **ZoneIds are hardcoded.** Use the numeric value directly (`ZoneId = 1314`), not `ZoneId.ConstantName`.
2. **Expansion must match the enum exactly:** `ARealmReborn`, `Heavensward`, `Stormblood`, `Shadowbringers`, `Endwalker`, `Dawntrail`.
3. **EncounterType selects the region.** Common values: `Dungeons`, `Extreme Trials`, `Normal Raids`, `Alliance Raids`, `Heavyweight Raids`, `Savage Raids`, `Trials`, `Ultimate Raids`, `Unreals`, `Eureka`, `Bozja`.
4. **Region format:** `#region [Expansion]: [EncounterType]`.
5. **Insert location:** before the `#endregion` of the matching region.
6. **Null handling:** `null` in JSON becomes `null` in C#; an empty array `[]` becomes `new List<uint>()`.

## Generated code format

```csharp
new Encounter {
    ZoneId = 1314,
    Name = "Mistwake",
    Expansion = FfxivExpansion.Dawntrail,
    Enemies = new List<Enemy> {
        new Enemy {
            Id = 14270,
            Name = "Treno Catoblepas",
            TankBusters = new List<uint> {
                43329, // Thunder III
            },
            Aoes = new List<uint> {
                43327, // Earthquake
                43331, // Thunder II
                44825, // Ray of Lightning
            },
            AoeLockOns = null,
            Knockbacks = null,
            SharedTankBusters = null,
            BigAoes = null
        },
    }
},
```

## Workflow

1. Parse the JSON: ZoneId, Name, Expansion, EncounterType, Enemies.
2. Find `#region [Expansion]: [EncounterType]` in `FightLogicEncounters.cs`.
3. Locate the last encounter before that region's `#endregion`.
4. Generate the C# with correct indentation.
5. Insert before the `#endregion`, with a blank line before it if it is not the first entry in the region.
6. Format lists: `null` stays `null`; `[]` becomes `new List<uint>()`; values go one per line.
7. Add spell name comments where known. Nice to have, not required.

## Code style

- 4-space indentation.
- Each property on its own line.
- Multi-item lists: one item per line. Single-item lists may be inline.
- Trailing comma after the encounter's closing brace.

## Error handling

- If the region does not exist, create it following the pattern of the existing regions.
- If the Expansion value does not match the enum, use what was given and flag it as needing manual correction.
- If the EncounterType does not match an existing region, use it as-is and flag it.
- Validate that ZoneId, Name, Expansion, and Enemies are all present.

## Commit scope

Encounter additions use the `FightLogic` scope prefix, and the message names the duty rather than the zone id — `FightLogic - Add all bosses for Windurst: The Third Walk`. See the `commit-and-pr` skill.
