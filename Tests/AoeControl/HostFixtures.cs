// Detached host fixtures make leases, masking, targeting and zoning repeatable.
// These values are not memory offsets and do not certify live job rotations.
namespace ff14bot.Enums
{
    public enum ClassJobType { Adventurer, Paladin, Warrior, DarkKnight, Gunbreaker, WhiteMage, Scholar, Astrologian, Sage, Monk, Dragoon, Ninja, Samurai, Reaper, Viper, Bard, Machinist, Dancer, BlackMage, Summoner, RedMage, Pictomancer, BlueMage, BeastMaster }
}
namespace ff14bot.Objects
{
    public class GameObject { public bool IsValid = true; public uint ObjectId; }
    public class BattleCharacter : GameObject { public bool CanAttack; }
    public class Player : BattleCharacter { public GameObject CurrentTarget; }
    public class SpellData
    {
        public uint Id; public int RawCastType = 1; public float Radius; public int EffectRange;
        public bool GroundTarget; public SpellData Effective;
    }
}
namespace ff14bot { public static class Core { public static Objects.Player Me = new() { ObjectId = 1 }; } }
namespace ff14bot.Managers
{
    public enum CapabilityFlags { Aoe }
    public static class RoutineManager { public static bool Disallowed; public static bool IsAnyDisallowed(CapabilityFlags flag) => Disallowed; }
    public static class WorldManager { public static bool InPvP; public static ushort ZoneId; }
    public static class DutyManager { public static bool InInstance; }
}
namespace Magitek.Extensions
{
    public static class SpellExtensions { public static ff14bot.Objects.SpellData Masked(this ff14bot.Objects.SpellData spell) => spell.Effective ?? spell; }
}
namespace Magitek.Models.Account
{
    public sealed class BaseSettings { public static BaseSettings Instance = new(); public bool EnableAoe = true; }
}
