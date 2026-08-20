using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.BlackMage;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.BlackMage;

namespace Magitek.Logic.BlackMage
{
    internal static class Buff
    {
        public static async Task<bool> Triplecast()
        {
            // Spell must be unlocked and user setting enabled
            if (!Spells.Triplecast.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.TripleCast)
                return false;

            // Do not pop Triplecast if Flare Star is ready, as it is an instant cast and would waste a charge
            if (AstralSoulStacks == 6)
                return false;

            // Cooldown/charge availability check
            if (Spells.Triplecast.Cooldown != TimeSpan.Zero && Spells.Triplecast.Charges == 0)
                return false;

            // Do not pop Triplecast if we already have instant cast buffs active
            if (Core.Me.HasAura(Auras.Triplecast) || Core.Me.HasAura(Auras.Swiftcast))
                return false;

            // Restrict Triplecast entirely to Astral Fire to maximize damage output
            if (AstralStacks == 0)
                return false;

            // Do not pop Triplecast if we are out of MP and about to transition, unless we need it to move
            if (Core.Me.CurrentMana < 4800 && !MovementManager.IsMoving)
                return false;

            // Emergency Movement: Pop Triplecast if moving while fully stacked in Astral Fire
            if ((MovementManager.IsMoving && AstralStacks == 3))
                return await Spells.Triplecast.Cast(Core.Me);

            // Setting check: Prevent using the last charge for stationary damage if the user saves it for moving
            if (BlackMageSettings.Instance.TripleCastWhileMoving && Spells.Triplecast.Charges <= 1)
                return false;

            return await Spells.Triplecast.Cast(Core.Me);
        }

        public static async Task<bool> Swiftcast()
        {
            // Standard API readiness check
            if (!Spells.Swiftcast.IsKnownAndReady())
                return false;

            // Do not pop Swiftcast if we already have instant cast buffs active
            if (Core.Me.HasAura(Auras.Triplecast) || Core.Me.HasAura(Auras.Swiftcast))
                return false;

            // Restrict Swiftcast strictly to Astral Fire to maximize damage
            if (AstralStacks == 0)
                return false;

            // Never pop Swiftcast if out of MP; next action will be Blizzard III (Ice transition) which already casts fast
            if (Core.Me.CurrentMana < 1600 && !MovementManager.IsMoving)
                return false;

            // Use Swiftcast strictly to maintain uptime during Movement
            if (MovementManager.IsMoving)
                return await Spells.Swiftcast.Cast(Core.Me);

            // Use Swiftcast for high-cost, long-cast spells at the end of the Fire phase (specifically Despair)
            if (Core.Me.CurrentMana > 800 && Core.Me.CurrentMana <= 2400)
                return await Spells.Swiftcast.Cast(Core.Me);

            return false;
        }

        public static async Task<bool> LeyLines()
        {
            // Spell must be unlocked and user setting enabled
            if (!Spells.LeyLines.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.LeyLines)
                return false;

            // Check if user has restricted LeyLines to boss encounters only
            if (BlackMageSettings.Instance.LeyLinesBossOnly && !Core.Me.CurrentTarget.IsBoss())
                return false;

            // Do not drop LeyLines while moving
            if (MovementManager.IsMoving)
                return false;

            // Prevent double-casting if LeyLines are already deployed
            if (Core.Me.HasAura(Auras.LeyLines))
                return false;

            // Do not deploy LeyLines until our DoTs are safely applied
            if (!Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                return false;

            // Restrict LeyLines entirely to Astral Fire to maximize damage
            if (AstralStacks != 3 || UmbralStacks > 0)
                return false;

            // Safety check against the Circle Of Power buff
            if (Core.Me.HasAura(Auras.CircleOfPower))
                return false;

            // Weave window optimization: Cast LeyLines after transitioning into Fire 3 with Umbral Hearts or Triplecast
            if (Casting.LastSpellWas(Spells.Fire3) && (UmbralHearts == 3 || Core.Me.HasAura(Auras.Triplecast)))
                return await Spells.LeyLines.Cast(Core.Me);

            // Weave window optimization (AoE): Cast LeyLines after transitioning into High Fire II
            if (Casting.LastSpellWas(Spells.HighFireII) && (UmbralHearts == 3 || Core.Me.HasAura(Auras.Triplecast)))
                return await Spells.LeyLines.Cast(Core.Me);

            return await Spells.LeyLines.Cast(Core.Me);
        }

        private static readonly uint[] ThunderAuras =
        {
            Auras.Thunder,
            Auras.Thunder2,
            Auras.Thunder3,
            Auras.Thunder4,
            Auras.HighThunder,
            Auras.HighThunder2
        };

        public static async Task<bool> Retrace()
        {
            // Spell must be unlocked and ready
            if (!Spells.Retrace.IsKnown())
                return false;

            if (!Spells.Retrace.IsKnownAndReady())
                return false;

            if (Spells.Retrace.Cooldown != TimeSpan.Zero)
                return false;

            // We must have LeyLines active to teleport back to them
            if (!Core.Me.HasAura(Auras.LeyLines))
                return false;

            // If we already have the CircleOfPower buff, we are standing in them; no need to teleport
            if (Core.Me.HasAura(Auras.CircleOfPower))
                return false;

            // Do not attempt to Retrace if currently moving
            if (MovementManager.IsMoving)
                return false;

            // Only use during combat encounters
            if (!Core.Me.InCombat)
                return false;

            return await Spells.Retrace.Cast(Core.Me);
        }

        public static async Task<bool> UmbralSoul()
        {
            // Spell must be unlocked and setting enabled
            if (!Spells.UmbralSoul.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.UmbralSoul)
                return false;

            if (Spells.UmbralSoul.Cooldown != TimeSpan.Zero)
                return false;

            // Combat safety: Allow Umbral Soul in combat IF moving to recover mana/stacks on the run,
            // otherwise block it so we don't accidentally cast it while standing in front of a target
            if (Core.Me.InCombat && Core.Me.HasTarget && !MovementManager.IsMoving)
                return false;

            // Umbral Soul strictly requires Umbral Ice to be active
            if (UmbralStacks == 0)
                return false;

            // Cast if we need to secure max stacks or max hearts
            if (UmbralStacks < 3 || UmbralHearts < 3)
                return await Spells.UmbralSoul.Cast(Core.Me);

            return false;
        }

        public static async Task<bool> PreCombatUmbralSoul()
        {
            // Spell must be unlocked and setting enabled
            if (!Spells.UmbralSoul.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.UmbralSoul)
                return false;

            if (!Spells.UmbralSoul.IsKnownAndReady())
                return false;

            // This is strictly a pre-combat out-of-combat preparation method
            if (Core.Me.InCombat)
                return false;

            // Umbral Soul strictly requires Umbral Ice to be active
            if (UmbralStacks == 0)
                return false;

            // Maintain max stacks, hearts, and MP between pulls
            if (UmbralStacks < 3 || UmbralHearts < 3 || Core.Me.CurrentMana < Core.Me.MaxMana)
                return await Spells.UmbralSoul.Cast(Core.Me);

            return false;
        }

        public static async Task<bool> ManaFont()
        {
            // Spell must be unlocked and setting enabled
            if (!Spells.ManaFont.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.ManaFont)
                return false;

            // Dawntrail check: Manafont restores MP but MUST be used while in Astral Fire
            if (AstralStacks == 0)
                return false;

            // Do not pop Manafont if Flare Star is fully stacked and ready to fire
            if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                return false;

            // Only pop Manafont when completely out of MP at the end of the AF phase
            if (Core.Me.CurrentMana >= 800)
                return false;

            return await Spells.ManaFont.Cast(Core.Me);
        }

        public static async Task<bool> Transpose()
        {
            // Basic API unlock check
            if (!Spells.Transpose.IsKnown())
                return false;

            return await Spells.Transpose.Cast(Core.Me);
        }

        public static async Task<bool> PreCombatTranspose()
        {
            // Spell must be unlocked and ready
            if (!Spells.Transpose.IsKnown())
                return false;

            if (!Spells.Transpose.IsKnownAndReady())
                return false;

            // This is strictly a pre-combat out-of-combat preparation method
            if (Core.Me.InCombat)
                return false;

            // If we dropped stance entirely, do not Transpose
            if (AstralStacks == 0)
                return false;

            // Do not transpose if our MP is already full
            if (Core.Me.CurrentMana >= Core.Me.MaxMana)
                return false;

            return await Spells.Transpose.Cast(Core.Me);
        }

        public static async Task<bool> Amplifier()
        {
            // Spell must be unlocked and setting enabled
            if (!Spells.Amplifier.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.Amplifier)
                return false;

            // Strictly restrict to active combat
            if (!Core.Me.InCombat)
                return false;

            // Verify our max polyglot stack count based on current character level
            int maxPolyglot = Core.Me.ClassLevel >= 98 ? 3 : (Core.Me.ClassLevel >= 80 ? 2 : 1);
            
            // Do not cast if we are already at the maximum capacity for Polyglot stacks
            if (ActionResourceManager.BlackMage.PolyglotCount >= maxPolyglot)
                return false;

            return await Spells.Amplifier.Cast(Core.Me);
        }
    }
}