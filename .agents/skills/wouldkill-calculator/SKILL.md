---
name: wouldkill-calculator
description: Architecture of Magitek's PvP damage prediction (WouldKillWithPotency in Logic/Roles/CommonPvp.cs) and the per-patch audit checklist for keeping it correct. Use when adding a PvP aura, mitigation, shield, or job damage modifier, when changing WouldKillWithPotency or the potencies passed at its call sites, when a kill-shot ability fires at the wrong time, or when auditing patch notes for PvP balance changes.
---

# PvP WouldKill Calculator

`Magitek/Logic/Roles/CommonPvp.cs` houses `WouldKillWithPotency` and three dictionaries that drive it. It centralizes PvP damage prediction so kill-shot abilities — Eagle Eye Shot, Smite, role finishers — can prefer real kills over HP-threshold heuristics.

When adding new auras or damage modifiers from a patch, **drop them into the right dictionary**. Do not reimplement the calculation.

## The three dictionaries

- **`TargetAuras`** (`uint -> multiplier`): auras on the target affecting damage they take. Both vulnerabilities (>1.0) and mitigation (<1.0). Includes Guard (0.10), Rampart (0.50), Bravery (0.75), Phalanx (0.67). Applied multiplicatively.
- **`SelfAuras`** (`uint -> multiplier`): auras on self affecting damage we deal. Buffs (>1.0) and debuffs (<1.0, e.g. Rust 0.67, Scarlet Flame 0.50). Battle High I-V live here.
- **`AbsorbAuras`** (`uint -> potency`): flat shield potencies subtracted after multipliers, capped at 0. Holy Sheltron (8000), Stem the Tide (13200).

## Per-mode modifiers

- **`FrontlineJobModifiers`** (`ClassJobType -> (dealt, taken)`): applied only in Frontline.
- **`RivalWingsJobModifiers`** (`ClassJobType -> taken`): applied only in Hidden Gorge / Rival Wings.

## Special-case branches — not in the dictionaries

These are inline in `WouldKillWithPotency` because they need conditional logic the dictionaries cannot express:

- **Mesotes / Lype**: target has Mesotes and we lack Lype, return false (we cannot damage them).
- **Pressure Point** on target: adds a flat +12000 potency.
- **Debana** (SAM-only +15%), **Noxious Gnash** (VPR-only +25%), **Kuzushi** (SAM-only +25%) — gated on `Core.Me.CurrentJob` because we cannot tell whether *we* applied the debuff or an ally did.

## The rule that trips people up

A self-buff that reduces *damage taken* — for example SMN Mountain Buster's 15% mitigation added in 7.5 — belongs in **`TargetAuras`**, not `SelfAuras`. From the calculator's perspective, "the target is an enemy SMN with that buff up" means the target takes less damage from us. `SelfAuras` is only for damage **we deal**.

## Known limitation

The calculation does not know which spell is being evaluated, so per-spell behaviors are not modelled. From 7.4:

- PLD Shield Smite no longer applies its vulnerability when the target has Guard — instead Guard's strength is halved.
- SCH Chain Stratagem behaves the same way.

A proper fix would add a `castingSpell` parameter so the calculation could suppress vulnerabilities and adjust Guard per spell. Until then, these casts may slightly underestimate damage against Guarded enemies.

## Patch audit checklist

When auditing patch notes, **do not just read the "PvP Actions" section**. Lodestone splits PvP balance changes across five sections, and all five touch this calculation:

1. **PvP Actions** (per-job action tables) — feeds `TargetAuras`, `SelfAuras`, `AbsorbAuras`, and the potencies passed in at call sites.
2. **Frontline** (per-job adjustments table) — feeds `FrontlineJobModifiers`. Lines like "Damage Taken: Increased from -50% to -45%" live here, not in the action tables. Easy to miss because the section is labelled "Frontline", not "PvP".
3. **Rival Wings / Hidden Gorge** (per-job damage taken table) — feeds `RivalWingsJobModifiers`.
4. **Crystalline Conflict** — no per-job modifiers in code today. If a patch adds CC-specific per-job adjustments, that needs a new dictionary.
5. **Battle System** — sometimes carries role-action and Guard-style global changes. 7.5's Guard change (90% → 99%) lived under "All Jobs / Role Actions" inside PvP Actions, but global tweaks can hide here too.

## Conversion shortcuts

- `Damage Taken: -50% → -45%` means the multiplier goes `0.50 → 0.55`. Lower means more mitigation.
- `Damage Dealt: -10% → -5%` means the multiplier goes `0.90 → 0.95`. Lower means more of a nerf.
- "Reduces damage taken by 25%" is a `0.75` multiplier on the target side.
- "Increases damage dealt by 15%" is a `1.15` multiplier on the self side.
- "Absorbs 10,000 potency" goes in `AbsorbAuras` as a flat value, never a multiplier.
