---
name: commit-and-pr
description: Magitek's commit message, branch naming, and pull request conventions. Use when committing changes, creating a branch, or opening a PR for this repo. Commit messages here are user-facing patch notes, so the format is not cosmetic.
allowed-tools: Bash(git:*), Bash(gh:*)
---

# Magitek Commit & PR

Commit messages are **user-facing patch notes**. They flow:

    commit message → PR title → GitHub Release changelog → in-app news feed and Discord

The Magitek UI (`Controls/CurrentNews.xaml`) pulls GitHub Releases — the release name becomes the title and the "What's Changed" PR list becomes the body. Every PR title reaches end users in the app and on Discord. Write for players, not developers.

## Commit message format

```
[SCOPE] - [User-facing description]
```

### Scope prefixes

- **Job-specific**: `GNB`, `MCH`, `WHM`, `DRK`, `PLD`, `WAR`, `SCH`, `SGE`, `AST`, `BRD`, `DNC`, `NIN`, `SAM`, `MNK`, `DRG`, `RPR`, `VPR`, `BLM`, `SMN`, `RDM`, `PCT`, `BLU`
- **Multiple jobs**: `RPR & PCT`, `WHM & SGE`
- **Role-wide**: `TANKS`, `HEALERS`, `MELEE`, `RANGED`, `ALL`
- **PvP**: `MCH PVP`, `All PVP`, `PVP ALL`
- **General**: `Core` (framework), `FightLogic` (encounters), `OC` (Occult Crescent), `Magitek` (general)

### Writing rules

- Write for players, not developers.
- Describe what changed, not how it was implemented.
- No file names, code references, zone IDs, spell IDs, or internal identifiers.
- Focus on impact: what does this change do for the user?

### Examples

| Good | Bad |
|------|-----|
| `GNB - Fix low level Optimized Burst rotation down to level 60` | `Fix SingleTarget.cs line 45` |
| `FightLogic - Add all bosses for Windurst: The Third Walk` | `FightLogic - Add bosses for ZoneId 1368` |
| `MCH - Add experimental options for guaranteed 6GCD wildfires` | `Refactor buff checks` |

### Multiple changes

Separate with ` - `: `ALL - Update cooldown calculation. - PCT - Improve rotation alignment.`

Prefer separate commits for unrelated changes.

## Branch naming

Lowercase, hyphenated, 2-4 words.

| Pattern | Examples |
|---------|----------|
| Job-specific | `gnb-brutal-shell`, `whm-seraph-strike` |
| PvP | `mch-pvp-drill`, `all-pvp-guard` |
| FightLogic | `fightlogic-windurst`, `fightlogic-tank-def` |
| Fixes | `fix-padding`, `fix-memread-ondeath` |

### Branch logic

1. On `master`: always create a new branch.
2. On a feature branch matching the change scope: keep it.
3. On a feature branch that does not match the scope: create a new branch off the current one.

## Pull request

**Title:** the commit message, verbatim.

**Body:** written for the reviewer, not for players. It never reaches the release notes, so there is no cost to being specific in it. Keep it empty for trivial changes. For anything touching rotation logic, fight logic, or shared code, include:

- **Tested:** the job, level, and content you ran it in — "BLM Lv100, Aetherdrome dummy". If you did not run it in game, write **Not tested in game.** That is an accepted answer and far more useful than silence.
- **Sources:** links backing any claim about how an ability behaves. The in-game tooltip, the official job guide, The Balance, or Icy Veins. A confident sentence is not a source.
- **Anything you removed:** guards, conditions, or comments you deleted, and why. Existing guards are usually there because something went wrong without them.

Agent attribution is fine — `Co-Authored-By` trailers and mentions of the tool used are welcome, and knowing which agent produced a change helps review. Keep it out of the *title*, which is player-facing.

```
gh pr create --title "<commit message>" --body "<verification block, or empty>"
gh pr merge <number> --auto --merge
```

**Always use merge commits.** Never squash or rebase.

## PR size

Smaller PRs get reviewed faster. One behavior change per PR where you can manage it — if the title needs an "and", it is probably two PRs. Keep renames, moves, and formatting separate from logic changes. None of this is a hard limit; if a change has to land in one piece, say so in the body.

## Workflow

1. `git status` and `git diff --stat`.
2. Determine the scope prefix.
3. If on `master`, create a branch per the naming rules.
4. Stage specific files. Never `git add -A` unless the whole worktree has been reviewed.
5. Commit with the user-facing message.
6. Build: `dotnet build Magitek\Magitek.sln`.
7. Push with `-u`.
8. Open the PR with the commit message as the title and the body filled in as above.
