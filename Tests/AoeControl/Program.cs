using System.Text.Json;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Models.Account;
using Magitek.Utilities;

// Exercise the production control through both public toggles and the RB override.
// Representative action geometry is explicit so these cases run without the client.
var checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition)
        throw new Exception(message);
}

var target = new BattleCharacter { ObjectId = 2, CanAttack = true };
var other = new BattleCharacter { ObjectId = 3, CanAttack = true };
Core.Me.CurrentTarget = target;
var fastBlade = new SpellData { Id = 9 };
var cure = new SpellData { Id = 120 };
var confiteor = new SpellData { Id = 16459, RawCastType = 2, Radius = 5, EffectRange = 5 };
var assize = new SpellData { Id = 3571, RawCastType = 2, Radius = 20, EffectRange = 20 };
var masked = new SpellData { Id = 7503, Effective = confiteor };
var shadow = new SpellData { Id = 16472 };

Check(AoeControl.Allows(confiteor, target), "Enabled AoE must allow splash finishers");
foreach (var useBotbase in new[] { false, true })
{
    AoeControl.Set(useBotbase);
    RoutineManager.Disallowed = useBotbase;
    Check(!AoeControl.Enabled, "Both sources disable the same AoE control");
    Check(!AoeControl.Allows(confiteor, target), "Confiteor must obey AoE control");
    Check(!AoeControl.Allows(assize, Core.Me), "Self-targeted Assize must obey AoE control");
    Check(!AoeControl.Allows(masked, target), "Use the masked action's geometry");
    Check(AoeControl.Allows(fastBlade, target), "Single-target damage remains available");
    Check(AoeControl.Allows(cure, Core.Me), "Single-target healing remains available");
    Check(AoeControl.Allows(fastBlade, other) == !useBotbase, "Only ordered kills restrict off-target damage");
    Check(!AoeControl.Allows(null, target), "Missing action must not bypass disabled AoE");
    Check(!AoeControl.Allows(new SpellData { Id = 9, RawCastType = 0 }, target), "Unknown geometry is rejected");
    Check(!AoeControl.Allows(new SpellData { Id = 9, Radius = float.NaN }, target), "Invalid geometry is rejected");
    Check(!AoeControl.Allows(new SpellData { Id = 9, GroundTarget = true }, target), "Ground casts require AoE");
    Check(!AoeControl.Allows(fastBlade, new GameObject { IsValid = false }), "Invalid target is rejected");
    foreach (var id in new uint[] { 44, 36923, 16472, 2864, 16501, 7427, 25831, 36992,
        25802, 25803, 25804, 25805, 25806, 25807, 25838, 25839, 25840, 25800, 3581,
        3639, 2270, 25837, 7439, 8324, 7418 })
        Check(!AoeControl.Allows(new SpellData { Id = id }, Core.Me), "Lasting damage is rejected: " + id);
}

Check(BaseSettings.Instance.EnableAoe, "Botbase restriction must not change the saved preference");
AoeControl.Toggle();
Check(!BaseSettings.Instance.EnableAoe, "Toggle changes the saved preference during a restriction");
RoutineManager.Disallowed = false;
Check(!AoeControl.Enabled, "Releasing a restriction preserves a user-disabled preference");
AoeControl.Enable();
Check(AoeControl.Allows(confiteor, target), "Re-enabling restores area casts");
RoutineManager.Disallowed = true;
AoeControl.Enable();
Check(!AoeControl.Enabled, "Enable cannot override the botbase");
WorldManager.InPvP = true;
Check(AoeControl.Allows(confiteor, target), "PvE botbase restriction must not apply in PvP");
WorldManager.InPvP = false;
RoutineManager.Disallowed = false;

AoeControl.Prepare(1069);
WorldManager.ZoneId = 177;
Check(AoeControl.Allows(shadow, Core.Me), "Preparation must not affect another territory");
WorldManager.ZoneId = 1069;
DutyManager.InInstance = true;
Check(!AoeControl.Allows(shadow, Core.Me), "Prepare before summons can outlive the restriction");
Check(AoeControl.Allows(confiteor, target), "Preparation leaves ordinary AoE available");
AoeControl.Prepare(0);
Check(AoeControl.Allows(shadow, Core.Me), "Stop/release clears preparation");

var jobs = Enum.GetValues<ClassJobType>().Where(AoeControl.Supports).ToArray();
Check(jobs.Length == 21, "Advertise the 21 normal combat jobs");
foreach (var job in new[] { ClassJobType.Adventurer, ClassJobType.BlueMage, ClassJobType.BeastMaster, (ClassJobType)999 })
    Check(!AoeControl.Supports(job), "Do not advertise limited or unknown jobs: " + job);

// Optional first-hand client capture supplements the repeatable regressions.
if (args.Length > 0)
{
    var rows = JsonSerializer.Deserialize<ActionRow[]>(File.ReadAllText(args[0]));
    AoeControl.Disable();
    foreach (var job in jobs)
        Check(rows.Any(r => r.Job == job.ToString()), "Capture includes " + job);
    foreach (var row in rows.Where(r => r.CastType != 1 || r.Radius != 0 || r.EffectRange != 0 || r.Ground))
    {
        var spell = new SpellData { Id = row.Id, RawCastType = row.CastType,
            Radius = row.Radius, EffectRange = row.EffectRange, GroundTarget = row.Ground };
        Check(!AoeControl.Allows(spell, target), "Captured area action obeys Disable: " + row.Name);
    }
    AoeControl.Enable();
}
Console.WriteLine($"Passed {checks} AoE control checks. No live casts performed.");

internal sealed record ActionRow(uint Id, string Name, int CastType, float Radius, int EffectRange, bool Ground, string Job);
