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
                        Logger.WriteInfo($"[AgentMKDSupportJobList] Agent pointer cached: 0x{_agentPointer.ToInt64():X}");
                    }
                    else
                    {
                        Logger.WriteInfo($"[AgentMKDSupportJobList] Agent {AgentId} is not valid or available");
                        return;
                    }

                    // Find and cache the ChangeSupportJob function with proper disposal
                    using (var pf = new PatternFinder(Core.Memory))
                    {
                        // Use FindSingle as recommended by RB dev (has don't rebase option)
                        _changeSupportJobFunc = pf.FindSingle(ChangeSupportJobPattern, true);

                        if (_changeSupportJobFunc != IntPtr.Zero)
                        {
                            Logger.WriteInfo($"[AgentMKDSupportJobList] ChangeSupportJob function found and cached: 0x{_changeSupportJobFunc.ToInt64():X}");
                        }
                        else
                        {
                            Logger.WriteInfo($"[AgentMKDSupportJobList] ChangeSupportJob function not found");
                            return;
                        }
                    } // PatternFinder automatically disposed here

                    _initialized = true;
                    Logger.WriteInfo($"[AgentMKDSupportJobList] Initialization completed successfully");
                }
                catch (Exception ex)
                {
                    Logger.WriteInfo($"[AgentMKDSupportJobList] Initialization failed: {ex.Message}");
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
                    Logger.WriteInfo($"[AgentMKDSupportJobList] Not properly initialized - Agent: 0x{_agentPointer.ToInt64():X}, Function: 0x{_changeSupportJobFunc.ToInt64():X}");
                    return false;
                }

                // Validate address (replicate CallInjectedWraper validation)
                if (_changeSupportJobFunc.ToInt64() < Core.Memory.ImageBase.ToInt64())
                {
                    Logger.WriteInfo($"[AgentMKDSupportJobList] Address is not in the game process");
                    return false;
                }

                Logger.WriteInfo($"[AgentMKDSupportJobList] Switching to phantom job {jobId}");
                Logger.WriteInfo($"[AgentMKDSupportJobList] Using cached Agent: 0x{_agentPointer.ToInt64():X}, Function: 0x{_changeSupportJobFunc.ToInt64():X}");

                // Call the cached function with cached agent pointer
                lock (Core.Memory.Executor.AssemblyLock)
                {
                    var result = Core.Memory.CallInjected64<IntPtr>(_changeSupportJobFunc, _agentPointer, jobId);

                    // Return value 0x1 indicates success, anything else is failure
                    bool success = result.ToInt64() == 0x1;
                    if (success)
                    {
                        Logger.WriteInfo($"[AgentMKDSupportJobList] Phantom job switch to {jobId} succeeded");
                    }
                    else
                    {
                        Logger.WriteInfo($"[AgentMKDSupportJobList] Phantom job switch to {jobId} failed (likely job not unlocked)");
                    }

                    return success;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteInfo($"[AgentMKDSupportJobList] Error switching to phantom job {jobId}: {ex.Message}");
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
                    Logger.WriteInfo($"[AgentMKDSupportJobList] Agent {AgentId} is not valid or available");
                    return false;
                }

                Logger.WriteInfo($"[AgentMKDSupportJobList] Opening phantom job interface");
                agent.Toggle();

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteInfo($"[AgentMKDSupportJobList] Error opening interface: {ex.Message}");
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
                Logger.WriteInfo($"[AgentMKDSupportJobList] Reset completed - will re-initialize on next use");
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