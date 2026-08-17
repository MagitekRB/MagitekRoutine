# Contributing

## Commit And PR Workflow

Commit messages are user-facing patch notes. They flow:

```text
commit message -> PR title -> GitHub Release changelog -> in-app news feed and Discord
```

The Magitek UI pulls GitHub Releases. Release names become titles, and the "What's Changed" PR list becomes user-visible content. Write commit messages and PR titles for players, not just developers.

### Commit Message Format

```text
[SCOPE] - [User-facing description]
```

Use the same text as the PR title.

### Scope Prefixes

- Job-specific: `GNB`, `MCH`, `WHM`, `DRK`, `PLD`, `WAR`, `SCH`, `SGE`, `AST`, `BRD`, `DNC`, `NIN`, `SAM`, `MNK`, `DRG`, `RPR`, `VPR`, `BLM`, `SMN`, `RDM`, `PCT`, `BLU`
- Multiple jobs: `RPR & PCT`, `WHM & SGE`
- Role-wide: `TANKS`, `HEALERS`, `MELEE`, `RANGED`, `ALL`
- PvP: `MCH PVP`, `All PVP`, `PVP ALL`
- General: `Core` for framework/runtime changes, `FightLogic` for encounter logic, `Magitek` for general application changes

### Writing Rules

- Write for end users.
- Describe what changed, not how it was implemented.
- Keep the message clear and concise.
- Avoid file names, code references, zone IDs, spell IDs, and internal identifiers.
- Prefer separate commits for unrelated changes.

Good examples:

```text
GNB - Fix low level Optimized Burst rotation down to level 60
FightLogic - Add all bosses for Windurst: The Third Walk
MCH - Add experimental options for guaranteed 6GCD wildfires
```

Bad examples:

```text
Fix SingleTarget.cs line 45
Refactor buff checks
Update rotation logic
```

For multiple related changes in one commit, separate entries with ` - `:

```text
ALL - Update cooldown calculation. - PCT - Improve rotation alignment. - SMN - Fix Crimson Strike usage
```

### Branch Naming

Use lowercase, hyphenated branch names. Keep them short, usually 2-4 words.

Examples:

```text
gnb-brutal-shell
whm-seraph-strike
mch-pvp-drill
all-pvp-guard
fightlogic-windurst
fix-memread-ondeath
```

Branch selection:

1. If on `master`, create a new branch.
2. If on a feature branch matching the change scope, keep it.
3. If on a feature branch that does not match the change scope, create a new branch from the current branch.

### PR Creation

- PR title should exactly match the commit message.
- PR body is written for the reviewer. See [PR Body](#pr-body) below.
- **Always use merge commits.** Never squash or rebase. Use `--merge` with `gh pr merge`.

Typical commands:

```powershell
gh pr create --title "<commit message>" --body " "
gh pr merge <number> --auto --merge
```

### PR Body

The title is written for players. The body is written for the reviewer, and it never reaches the release notes, so there is no cost to being specific in it.

Keep it empty for trivial changes. For anything touching rotation logic, fight logic, or shared code, include:

- **Tested:** the job, level, and content you ran it in — "BLM Lv100, Aetherdrome dummy", "SGE Lv90, Alexander normal". If you did not run it in game, write **Not tested in game.**
- **Sources:** links backing any claim about how an ability behaves. See [Claims About the Game](#claims-about-the-game).
- **Anything you removed:** guards, conditions, or comments you deleted, and why.

"Not tested in game" is an accepted answer, and a much better one than silence. Nobody has every job geared at every level. Knowing which parts still need checking is more useful than assuming they were checked.

### PR Size

Smaller PRs get reviewed faster. That is the whole of this section — it is about review throughput, not about the quality of the work.

Review capacity here is small and the surface is twenty-two jobs wide. A PR that does one thing can be read, reasoned about, and merged in a sitting. A PR that does six tends to sit, which is a worse outcome for you than being asked to split it.

Things that help:

- One behavior change per PR where you can manage it. If the title needs an "and", it is probably two PRs.
- Renames, moves, and formatting in their own PR, separate from logic changes.
- Adding a shared helper and converting call sites to it reads much more easily as two PRs: the helper first, then the migration.
- Changes to `AGENTS.md` or `CONTRIBUTING.md` are easier to discuss on their own.

None of this is a hard limit. If a change genuinely has to land in one piece, say so in the body and it will be reviewed as-is.

### Claims About the Game

Statements about how the game behaves — what an ability costs, what it upgrades into, when it becomes available, which buff it applies — need a source or a first-hand observation. In review these turn out to be wrong more often than the code does.

What counts:

- The in-game tooltip or the official job guide.
- [The Balance](https://www.thebalanceffxiv.com/) or [Icy Veins](https://www.icy-veins.com/ffxiv/) for rotation priority.
- Something you saw in game, phrased as an observation: "Medica II masked to Medica III at Lv96 on my character."

A confident sentence is not a source. When the reasoning behind a change is wrong, the change usually is too — even when it happens to fix the symptom.

### Standard Steps

1. Run `git status` and inspect the diff.
2. Determine the scope prefix and branch name.
3. Create or switch branches according to the branch rules.
4. Stage specific files. Do not use `git add -A` unless the whole worktree has been intentionally reviewed.
5. Commit with the user-facing message.
6. Run `dotnet build Magitek\Magitek.sln`.
7. Push with `git push -u origin <branch>`.
8. Open the PR with the commit message as the title, and fill in the body as described above.

## Optional Commit Template

This repo includes `.gitmessage` as a local commit-message template. To enable it:

```powershell
git config commit.template .gitmessage
```
