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
        public List<CruciblePieceAction> Actions { get; set; } = new List<CruciblePieceAction>();

        public bool IsBoss => Strength >= 5;
        public bool HasWeakness => !string.IsNullOrEmpty(Weakness);
    }

    public class CruciblePieceAction
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public float CastSeconds { get; set; }
        public int CastType { get; set; }
        public int Range { get; set; }
        public int EffectRange { get; set; }

        /// <summary>A cast long enough for Soul Crush to matter (the routine reads interruptibility live).</summary>
        public bool LongCast => CastSeconds >= 3f;
    }
}
