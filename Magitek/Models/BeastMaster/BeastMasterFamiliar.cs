using System.Collections.Generic;

namespace Magitek.Models.BeastMaster
{
    /// <summary>
    /// One familiar of the Beastmaster bestiary (Resources/BeastMasterFamiliars.json, from the 7.56 client data):
    /// what its Trick is (the instinctual skill and its compass affinity), what its Tempered Release does, and which
    /// kinship Borrow takes from it. The routine reads the summoned familiar's entry by name.
    /// </summary>
    public class BeastMasterFamiliar
    {
        public int PetId { get; set; }
        public string Name { get; set; }
        public string Kinship { get; set; }
        public int Level { get; set; }
        public int FeedCap { get; set; }
        public FamiliarAbility Trick { get; set; } = new FamiliarAbility();
        public FamiliarAbility TemperedRelease { get; set; } = new FamiliarAbility();
    }

    public class FamiliarAbility
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Affinity { get; set; }
        public List<int> Potencies { get; set; } = new List<int>();
        public int CastType { get; set; }
        public int Range { get; set; }
        public int EffectRange { get; set; }
        public string Description { get; set; }
    }

    /// <summary>The Inner Compass: clockwise order. Executing the next affinity within 7 s is an intentional combo.</summary>
    public static class Affinity
    {
        public const string Volant = "Volant";
        public const string Rampant = "Rampant";
        public const string Durant = "Durant";
        public const string Eldritch = "Eldritch";

        public static readonly string[] Clockwise = { Volant, Rampant, Durant, Eldritch };

        public static string Next(string affinity)
        {
            var i = System.Array.IndexOf(Clockwise, affinity);
            return i < 0 ? null : Clockwise[(i + 1) % 4];
        }

        public static string Previous(string affinity)
        {
            var i = System.Array.IndexOf(Clockwise, affinity);
            return i < 0 ? null : Clockwise[(i + 3) % 4];
        }
    }
}
