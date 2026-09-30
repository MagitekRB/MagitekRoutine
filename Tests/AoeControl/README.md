# AoE control regression checks

The tests compile the production control against detached RebornBuddy fixtures.
They check that the ordinary AoE switch and temporary botbase restrictions use
the same rules, including masked actions, damaging heals, off-target damage and
territory preparation. They cannot validate rotation timing or autonomous pets.

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project Tests/AoeControl/AoeControl.csproj
```

An optional JSON action capture can be passed after `--`. Captures are not needed
for the regression cases and are not checked in. These tests do not cast actions
or establish that every job can complete an ordered-kill encounter.
