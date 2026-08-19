---
name: ffxiv-id-lookup
description: Find FFXIV action, status (aura), and item IDs from datamining sources when you cannot read them off a running game client. Use when adding a spell to Spells.cs or an aura to Auras.cs, when verifying an ID after a patch, or when you need to confirm the PvP variant of an action.
---

# FFXIV Spell / Status (Aura) ID Lookup

Use this when the developer cannot be in game to dump IDs. If a live RebornBuddy session is available, read the IDs off the client instead — the client is the authority, since `DataManager` is what `Spells.cs` actually resolves against.

## Primary fallback chain

1. **xivapi/ffxiv-datamining (GitHub master)** — bulk source of truth, lags 1-3 days post-patch.
   - `https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/en/Status.csv`
   - `https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/en/Action.csv`
   - Watch commits by user `xiashtra`. Pre-patch builds are tagged "Pre X.Y"; the full dump lands as "Patch X.Y" or "X.Y" within ~1-3 days of release.
   - PvP actions usually live in IDs > 29000.
2. **Garland Tools API** — fast individual lookups, lags 3-7 days.
   - Search: `https://www.garlandtools.org/api/search.php?text=NAME&type={action|status}&exact=`
   - Detail: `https://www.garlandtools.org/db/doc/{action|status}/en/2/{ID}.json`
   - In search results, `t:2` = PvP, `t:3` = trial/raid, `t:4` = standard PvE.
3. **XIVAPI** — flexible queries when up, but unstable since late 2025 (frequent 500s).
   - `https://xivapi.com/search?indexes={Status|Action}&string=NAME&filters=ClassJob.ID=27,IsPvP=1`
4. **consolegameswiki / Gamer Escape** — human-readable cross-check.
   - `https://ffxiv.consolegameswiki.com/wiki/Patch_X.Y` and `/wiki/{Skill_Name}`

## Quick recipes

- Find new statuses in the 7.x band: `awk -F',' '$1>=4400 && $2!="" {print $1","$2}' Status.csv`
- Resolve an action ID to its self-buff: read `Action.csv` column `StatusGainSelf`.
- Find the PvP variant of an action: grep the name in `Action.csv`, take the row with PvP affinity (column 39 == 27).

## Anchors for cross-validation

If a source disagrees with these, distrust the source.

| Entity | ID |
|---|---|
| SMN PvP Mountain Buster (Action) | 29671 |
| SMN PvP Mountain Buster self-buff (Status, Patch 7.5) | 5531 |
| SMN PvP Radiant Aegis (Status) | 3224 |
| SMN PvP Scarlet Flame (Status) | 3231 |
| DRG PvP Horrid Roar (Status) | 3179 |
| VPR PvP World-swallower (Action) | 39190 |

## Lessons learned

- `Action.csv` column `StatusGainSelf` is **not** reliable. Mountain Buster (29671) had `StatusGainSelf = 0` in the post-7.5 dump even though casting it applies status 5531 — the buff comes from the action's effect bytecode, not a static declaration. When `StatusGainSelf` is 0, grep `Status.csv` by name instead: `awk -F',' 'NR>3 && $2=="Mountain Buster"' Status.csv`.
- Status ranges for new patch content drift higher than expected. The 7.5 SMN buff landed at 5531, not the 5180-5260 range that seemed likely.
- `Status.csv` column 5 (`ParamModifier`) gives the magnitude as a signed integer — `-15` means "reduces ... by 15%" — so you can verify the intended effect without leaving the CSV.
- PvE and PvP actions frequently share a display name. Always confirm which variant an ID belongs to before adding it to `Spells.cs`.
