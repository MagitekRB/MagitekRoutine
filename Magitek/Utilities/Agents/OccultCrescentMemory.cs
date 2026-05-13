using System;
using ff14bot;
using GreyMagic;
using Magitek.Utilities.Agents;

namespace Magitek.Utilities.Agents
{
    /// <summary>
    /// Reads Occult Crescent state from game memory (phantom job levels, current job, etc.)
    ///
    /// Memory Structure Reference:
    /// https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/InstanceContent/PublicContentOccultCrescent.cs
    ///
    /// PublicContentOccultCrescent layout (size 0x33A0):
    ///   0x3138: OccultCrescentState State
    ///   0x339D: bool StateLoaded
    ///
    /// OccultCrescentState layout (size 0x9C):
    ///   0x00: FixedSizeArray24&lt;uint&gt; _supportJobExperience
    ///   0x60: uint CurrentKnowledge
    ///   0x64: uint NeededKnowledge
    ///   0x68: uint NeededJobExperience
    ///   0x6C: ushort Silver
    ///   0x6E: ushort Gold
    ///   0x76: FixedSizeArray24&lt;byte&gt; _supportJobLevels  (indexed by PhantomJobId)
    ///   0x8E: FixedSizeArray3&lt;byte&gt; _unlockedTeleportBitmask
    ///   0x91: byte CurrentSupportJob
    ///   0x92: byte KnowledgeLevelSync
    ///
    /// How to update after a patch:
    ///   1. Check FFXIVClientStructs PublicContentOccultCrescent.cs for updated offsets
    ///   2. Update GetStatePattern if the MemberFunction signature changed
    ///   3. Update SupportJobLevelsOffset if the struct layout shifted
    ///   4. The pattern is a CALL-SITE pattern (E8 = relative CALL opcode),
    ///      so it must use "Add 1 TraceRelative" to resolve the actual function address.
    ///      Without TraceRelative, FindSingle returns the call-site address, not GetState() itself.
    /// </summary>
    internal static class OccultCrescentMemory
    {
        /// <summary>
        /// Memory pattern for GetState() static function
        ///
        /// Source: FFXIVClientStructs PublicContentOccultCrescent.GetState()
        ///   [MemberFunction("E8 ?? ?? ?? ?? 48 8B E8 48 85 C0 75 12")]
        ///   public static partial OccultCrescentState* GetState();
        ///
        /// This is a call-site pattern (starts with E8 = relative CALL).
        /// "Add 1 TraceRelative" skips past the E8 opcode byte, reads the 4-byte
        /// relative offset, and resolves: address + 4 + rel32 = actual function.
        /// Without this, FindSingle returns the call-site, not the function pointer.
        /// </summary>
        private const string GetStatePattern = "Search E8 ? ? ? ? 48 8B E8 48 85 C0 75 12 Add 1 TraceRelative";

        /// <summary>
        /// Offset of _supportJobLevels within OccultCrescentState struct.
        /// Array of 24 bytes indexed by PhantomJobId (0=Freelancer, 1=Knight, ..., 15=Dancer).
        /// Source: FFXIVClientStructs OccultCrescentState [FieldOffset(0x76)]
        /// </summary>
        private const int SupportJobLevelsOffset = 0x76;

        private static IntPtr _getStateFunc = IntPtr.Zero;
        private static bool _initialized = false;
        private static readonly object _initLock = new object();

        private static void Initialize()
        {
            if (_initialized) return;

            lock (_initLock)
            {
                if (_initialized) return;

                try
                {
                    using (var pf = new PatternFinder(Core.Memory))
                    {
                        _getStateFunc = pf.FindSingle(GetStatePattern, true);
                    }

                    _initialized = true;
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Get the level of a specific phantom job (0-15 from PhantomJobId enum)
        /// Returns 0 if the state can't be read (not in OC, pattern broken, etc.)
        /// </summary>
        public static byte GetSupportJobLevel(PhantomJobId jobId)
        {
            return GetSupportJobLevel((byte)jobId);
        }

        /// <summary>
        /// Get the level of a specific phantom job by raw index
        /// </summary>
        public static byte GetSupportJobLevel(byte jobIndex)
        {
            if (jobIndex >= 24)
                return 0;

            try
            {
                Initialize();

                if (_getStateFunc == IntPtr.Zero)
                    return 0;

                IntPtr statePtr;
                lock (Core.Memory.Executor.AssemblyLock)
                {
                    statePtr = Core.Memory.CallInjected64<IntPtr>(_getStateFunc);
                }

                if (statePtr == IntPtr.Zero)
                    return 0;

                return Core.Memory.Read<byte>(statePtr + SupportJobLevelsOffset + jobIndex);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Check if state is readable (we're in OC and patterns resolved)
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
                try
                {
                    Initialize();
                    return _getStateFunc != IntPtr.Zero;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static void Reset()
        {
            lock (_initLock)
            {
                _initialized = false;
                _getStateFunc = IntPtr.Zero;
            }
        }
    }
}
