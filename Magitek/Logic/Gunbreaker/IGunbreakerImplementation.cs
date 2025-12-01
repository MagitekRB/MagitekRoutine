using System.Threading.Tasks;

namespace Magitek.Logic.Gunbreaker
{
    /// <summary>
    /// Interface for Gunbreaker rotation implementations.
    /// All implementations must provide these rotation methods.
    /// </summary>
    public interface IGunbreakerImplementation
    {
        Task<bool> Rest();
        Task<bool> PreCombatBuff();
        Task<bool> Pull();
        Task<bool> Heal();
        Task<bool> CombatBuff();
        Task<bool> Combat();
        Task<bool> PvP();
    }
}

