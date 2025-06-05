using System;
using ff14bot;
using ff14bot.Managers;
using System;
using GreyMagic;

namespace Magitek.Utilities.Agents
{
    /// <summary>
    /// Agent for MKD Support Job List functionality in Occult Crescent
    /// Handles phantom job switching at knowledge crystals
    /// 
    /// Memory Structure Reference:
    /// https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/UI/Agent/AgentMKDSupportJobList.cs
    /// 
    /// Job ID Data Source (update PhantomJobId enum from this table):
    /// https://github.com/xivapi/ffxiv-datamining/blob/master/csv/MKDSupportJob.csv
    /// </summary>
    internal static class AgentMKDSupportJobList
    {
        /// <summary>
        /// Agent ID for MKDSupportJobList
        /// </summary>
        private const int AgentId = 468;

        /// <summary>
        /// Memory pattern for ChangeSupportJob function
        /// </summary>
        private const string ChangeSupportJobPattern = "Search 40 53 48 83 EC ? 0F B6 DA E8 ? ? ? ? 48 85 C0 74 ? 38 58";

        // Cached values - initialized once and reused
        private static IntPtr _changeSupportJobFunc = IntPtr.Zero;
        private static IntPtr _agentPointer = IntPtr.Zero;
        private static bool _initialized = false;
        private static readonly object _initLock = new object();

        /// <summary>
        /// Initialize the cached function address and agent pointer
        /// Uses proper disposal and caching as recommended by RB dev
        /// </summary>
        private static void Initialize()
        {
            if (_initialized) return;

            lock (_initLock)
            {
                if (_initialized) return;

                try
                {
                    // Get and cache the agent pointer
                    var agent = AgentModule.GetAgentInterfaceById(AgentId);
                    if (agent != null && agent.IsValid)
                    {
                        _agentPointer = agent.Pointer;
                    }
                    else
                    {
                        return;
                    }

                    // Find and cache the ChangeSupportJob function with proper disposal
                    using (var pf = new PatternFinder(Core.Memory))
                    {
                        // Use FindSingle as recommended by RB dev (has don't rebase option)
                        _changeSupportJobFunc = pf.FindSingle(ChangeSupportJobPattern, true);

                        if (_changeSupportJobFunc == IntPtr.Zero)
                        {
                            return;
                        }
                    } // PatternFinder automatically disposed here

                    _initialized = true;
                }
                catch (Exception ex)
                {
                    // Silent failure - if initialization fails, phantom job switching won't work
                }
            }
        }

        /// <summary>
        /// Switch to the specified phantom job using cached values and memory injection
        /// Optimized with caching as recommended by RB dev
        /// </summary>
        /// <param name="jobId">The phantom job ID to switch to (1-12)</param>
        /// <returns>True if the switch was successful, false otherwise</returns>
        public static bool SwitchToPhantomJob(byte jobId)
        {
            try
            {
                // Initialize on first use
                Initialize();

                if (!_initialized || _agentPointer == IntPtr.Zero || _changeSupportJobFunc == IntPtr.Zero)
                {
                    return false;
                }

                // Validate address (replicate CallInjectedWraper validation)
                if (_changeSupportJobFunc.ToInt64() < Core.Memory.ImageBase.ToInt64())
                {
                    return false;
                }

                // Call the cached function with cached agent pointer
                lock (Core.Memory.Executor.AssemblyLock)
                {
                    var result = Core.Memory.CallInjected64<IntPtr>(_changeSupportJobFunc, _agentPointer, jobId);

                    // Return value 0x1 indicates success, anything else is failure
                    return result.ToInt64() == 0x1;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Alternative method: Open the interface using Toggle()
        /// </summary>
        /// <param name="jobId">The phantom job ID (currently unused, just opens interface)</param>
        /// <returns>True if the interface was opened</returns>
        public static bool OpenPhantomJobInterface(byte jobId)
        {
            try
            {
                var agent = AgentModule.GetAgentInterfaceById(AgentId);

                if (agent == null || !agent.IsValid)
                {
                    return false;
                }

                agent.Toggle();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the agent and function are available
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
                try
                {
                    Initialize();
                    return _initialized && _agentPointer != IntPtr.Zero && _changeSupportJobFunc != IntPtr.Zero;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Force re-initialization (useful if game state changes)
        /// </summary>
        public static void Reset()
        {
            lock (_initLock)
            {
                _initialized = false;
                _agentPointer = IntPtr.Zero;
                _changeSupportJobFunc = IntPtr.Zero;
            }
        }


    }

    /// <summary>
    /// Phantom job IDs based on MKDSupportJob.csv data
    /// 
    /// Data Source (MUST be kept in sync with this table):
    /// https://github.com/xivapi/ffxiv-datamining/blob/master/csv/MKDSupportJob.csv
    /// 
    /// Correct job mapping from CSV (0-12):
    /// 0=Freelancer, 1=Knight, 2=Berserker, 3=Monk, 4=Ranger, 5=Samurai,
    /// 6=Bard, 7=Geomancer, 8=TimeMage, 9=Cannoneer, 10=Chemist, 11=Oracle, 12=Thief
    /// </summary>
    public enum PhantomJobId : byte
    {
        Freelancer = 0,
        Knight = 1,
        Berserker = 2,
        Monk = 3,
        Ranger = 4,
        Samurai = 5,
        Bard = 6,
        Geomancer = 7,
        TimeMage = 8,
        Cannoneer = 9,
        Chemist = 10,
        Oracle = 11,
        Thief = 12
    }
}