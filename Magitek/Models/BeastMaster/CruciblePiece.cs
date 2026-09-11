using System.Collections.Generic;

namespace Magitek.Models.BeastMaster
{
    /// <summary>
    /// One enemy of the Crucible of the Unbroken (Resources/BeastMasterCruciblePieces.json, from the 7.56 client data:
    /// the XBMBattleDetail sheet). The sheet is the board: its element is the weakness the board shows, its five
    /// ratings are the board stars (checked against twenty board reads, 2026-09-11), and its row groups the pieces of
    /// one encounter. Every piece has its own BNpcName, so a piece is known the moment it is targeted.
    /// </summary>
    public class CruciblePiece
    {
        public int Encounter { get; set; }
        public uint BNpcName { get; set; }
        public string Name { get; set; }
        public string Weakness { get; set; }
        public int Strength { get; set; }
        public int Intelligence { get; set; }
        public int PhysicalResistance { get; set; }
        public int MagicResistance { get; set; }
        public int Constitution { get; set; }
        public int Resist { get; set; }
        public int Board { get; set; }
        // From the board's own entry (hand-curated): which statuses land on this piece, and a player note.
        public List<string> VulnerableTo { get; set; } = new List<string>();
        public string Note { get; set; }
        public List<CruciblePieceAction> Actions { get; set; } = new List<CruciblePieceAction>();

        public bool IsBoss => Strength >= 5;
        public bool HasWeakness => !string.IsNullOrEmpty(Weakness);
    }

    public class CruciblePieceAction
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public float CastSeconds { get; set; }
        // Nullable: a move the board lists that the sheet does not has no cast data, and a null into an int fails the
        // whole deserialization, which left the library empty on its first run (2026-09-11).
        public int? CastType { get; set; }
        public int? Range { get; set; }
        public int? EffectRange { get; set; }

        // From the board's entry: who the move targets, whether Soul Crush can stop it, its damage type and shape,
        // the status it applies (on the player, the piece itself or its allies) and whether that status can be nullified.
        public string Target { get; set; }
        public bool? Interruptible { get; set; }
        public string DamageType { get; set; }
        public string Shape { get; set; }
        public string Status { get; set; }
        public bool? Nullifiable { get; set; }
        public string StatusOn { get; set; }
        // The board lists a piece's basic attack among its moves (the Voidmancer's Water); it is not an event.
        public bool Basic { get; set; }

        /// <summary>A cast Soul Crush is worth spending on: the board says it can be stopped, or, unknown, it is long enough to try.</summary>
        public bool WorthInterrupting => Interruptible == true || (Interruptible == null && CastSeconds >= 3f);
        public bool LongCast => CastSeconds >= 3f;
    }
}
