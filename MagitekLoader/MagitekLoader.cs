using Clio.Utilities;
using ff14bot.AClasses;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using System.Windows.Media;
using TreeSharp;
using Action = TreeSharp.Action;

namespace MagitekLoader;

/// <summary>
/// Proxy Combat Routine that delegates to the actual Magitek instance.
/// This allows hot-reload by swapping the internal instance while
/// RebornBuddy keeps a stable reference to the proxy.
/// </summary>
internal class MagitekProxy : CombatRoutine
{
    internal CombatRoutine? ActualRoutine { get; set; }

    public override string Name => ActualRoutine?.Name ?? "Magitek (Loading...)";
    public override bool WantButton => ActualRoutine?.WantButton ?? false;
    public override float PullRange => ActualRoutine?.PullRange ?? 25f;
    public override ClassJobType[] Class => ActualRoutine?.Class ?? new[] { ClassJobType.Adventurer };
    public override CapabilityFlags SupportedCapabilities => ActualRoutine?.SupportedCapabilities ?? CapabilityFlags.None;

    public override void Initialize()
    {
        ActualRoutine?.Initialize();
    }

    public override void ShutDown()
    {
        ActualRoutine?.ShutDown();
    }

    public override void Pulse()
    {
        ActualRoutine?.Pulse();
    }

    public override void OnButtonPress()
    {
        ActualRoutine?.OnButtonPress();
    }

    public override Composite RestBehavior => ActualRoutine?.RestBehavior ?? new TreeSharp.Action();
    public override Composite PreCombatBuffBehavior => ActualRoutine?.PreCombatBuffBehavior ?? new TreeSharp.Action();
    public override Composite PullBehavior => ActualRoutine?.PullBehavior ?? new TreeSharp.Action();
    public override Composite HealBehavior => ActualRoutine?.HealBehavior ?? new TreeSharp.Action();
    public override Composite CombatBuffBehavior => ActualRoutine?.CombatBuffBehavior ?? new TreeSharp.Action();
    public override Composite CombatBehavior => ActualRoutine?.CombatBehavior ?? new TreeSharp.Action();
}

/// <summary>
/// Collectible AssemblyLoadContext for hot-reloading Magitek.dll
/// </summary>
internal class MagitekLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public MagitekLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Let RebornBuddy assemblies resolve from the main context
        if (assemblyName.Name == "ff14bot" ||
            assemblyName.Name == "TreeSharp" ||
            assemblyName.Name == "GreyMagic" ||
            assemblyName.Name == "Clio.Utilities" ||
            assemblyName.Name?.StartsWith("RebornBuddy") == true ||
            assemblyName.Name?.StartsWith("System") == true ||
            assemblyName.Name?.StartsWith("Microsoft") == true ||
            assemblyName.Name == "Newtonsoft.Json" ||
            assemblyName.Name == "PresentationFramework" ||
            assemblyName.Name == "PresentationCore" ||
            assemblyName.Name == "WindowsBase")
        {
            return null; // Use default resolution
        }

        // Try to resolve using the dependency resolver
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}

public class CombatRoutineLoader : IAddonProxy<CombatRoutine>
{
    private const string ProjectName = "Magitek";
    private const string ProjectMainType = "Magitek.Magitek";
    private const string ProjectAssemblyName = "Magitek.dll";
    private const string ZipUrl = "https://github.com/MagitekRB/MagitekRoutine/releases/latest/download/Magitek.zip";
    private const string VersionUrl = "https://github.com/MagitekRB/MagitekRoutine/releases/latest/download/Version.txt";
    private static readonly Color LogColor = Colors.CornflowerBlue;

    private static readonly string GreyMagicAssembly = Path.Combine(Environment.CurrentDirectory, @"GreyMagic.dll");

    private string _currentDirectory = string.Empty;
    private string _projectAssembly = string.Empty;
    private string _versionPath = string.Empty;
    private string _tempDir = string.Empty;

    private static string? _latestVersion;
    private static readonly object Locker = new object();

    // Hot reload support
    private static FileSystemWatcher? _fileWatcher;
    private static DateTime _lastReloadTime = DateTime.MinValue;
    private static bool _isReloading = false;
    private static WeakReference? _loadContextRef;
    private static string? _currentTempAssembly; // Track current temp file

    private static volatile bool _loaded = false;
    private static CombatRoutine? _product;
    private static MagitekProxy? _proxy; // Stable proxy that RebornBuddy keeps

    // Cached lifecycle methods
    private static MethodInfo? _initializeMethod;
    private static MethodInfo? _shutdownMethod;
    private static MethodInfo? _pulseMethod;
    private static MethodInfo? _buttonMethod;

    private readonly HttpClient _client = new HttpClient();

    public async Task<CombatRoutine?> Load(string directory)
    {
        _currentDirectory = directory;
        _projectAssembly = Path.Combine(directory, $"{ProjectAssemblyName}");
        _versionPath = Path.Combine(directory, "Version.txt");
        _tempDir = Path.Combine(directory, "Temp");

        await AutoUpdate();

        // Create proxy once - RebornBuddy will keep this reference
        if (_proxy == null)
        {
            _proxy = new MagitekProxy();
        }

        // Load the actual Magitek instance into the proxy
        LoadProduct();

        return _proxy;
    }

    private void RedirectAssembly()
    {
        AppDomain.CurrentDomain.AssemblyResolve += Handler;
        AppDomain.CurrentDomain.AssemblyResolve += GreyMagicHandler;
    }

    Assembly? Handler(object sender, ResolveEventArgs args)
    {
        var name = Assembly.GetEntryAssembly()?.GetName().Name;
        var requestedAssembly = new AssemblyName(args.Name);
        return requestedAssembly.Name != name ? null : Assembly.GetEntryAssembly();
    }

    Assembly? GreyMagicHandler(object sender, ResolveEventArgs args)
    {
        var requestedAssembly = new AssemblyName(args.Name);
        return requestedAssembly.Name != "GreyMagic" ? null : Assembly.LoadFrom(GreyMagicAssembly);
    }

    private static Assembly? LoadAssembly(string path, MagitekLoadContext context)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        Assembly? assembly = null;
        try
        {
            // Load from bytes to avoid locking the file
            // This allows the original DLL to be overwritten while loaded!
            byte[] assemblyBytes = File.ReadAllBytes(path);

            // Also load PDB for debugging if available
            var pdbPath = Path.ChangeExtension(path, ".pdb");
            if (File.Exists(pdbPath))
            {
                byte[] pdbBytes = File.ReadAllBytes(pdbPath);
                assembly = context.LoadFromStream(new MemoryStream(assemblyBytes), new MemoryStream(pdbBytes));
            }
            else
            {
                assembly = context.LoadFromStream(new MemoryStream(assemblyBytes));
            }
        }
        catch (Exception e)
        {
            Logging.WriteException(e);
        }

        return assembly;
    }

    /// <summary>
    /// Simple direct load for production (no temp files, no hot-reload, no overhead)
    /// </summary>
    private CombatRoutine? LoadDirect()
    {
        RedirectAssembly();

        if (!File.Exists(_projectAssembly))
        {
            Log($"Magitek.dll not found at: {_projectAssembly}");
            return null;
        }

        Assembly? assembly;
        try
        {
            assembly = Assembly.LoadFrom(_projectAssembly);
        }
        catch (Exception e)
        {
            Log($"Failed to load assembly: {e.Message}");
            Logging.WriteException(e);
            return null;
        }

        Type? baseType;
        try
        {
            baseType = assembly.GetType(ProjectMainType);
        }
        catch (Exception e)
        {
            Log(e.ToString());
            return null;
        }

        CombatRoutine? routine;
        try
        {
            routine = (CombatRoutine?)Activator.CreateInstance(baseType);
        }
        catch (Exception e)
        {
            Log(e.ToString());
            Log("Could not load Magitek. This can be due to a new version of Rebornbuddy being released. An update should be ready soon.");
            return null;
        }

        if (routine != null)
        {
            Log(ProjectName + " was loaded successfully.");
        }
        else
        {
            Log("Could not load " + ProjectName + ". This can be due to a new version of Rebornbuddy being released. An update should be ready soon.");
        }

        return routine;
    }

    /// <summary>
    /// Load with collectible AssemblyLoadContext for hot-reload (dev mode only)
    /// Loads DIRECTLY from original DLL - no temp files needed!
    /// AssemblyLoadContext loads into memory, doesn't lock for writing.
    /// </summary>
    private CombatRoutine? LoadFromTemp()
    {
        RedirectAssembly();

        // Create a new collectible AssemblyLoadContext
        // Load directly from original file - it will be loaded into memory, not locked!
        var loadContext = new MagitekLoadContext(_projectAssembly);
        _loadContextRef = new WeakReference(loadContext, trackResurrection: true);

        // Load from original file (AssemblyLoadContext loads into memory - original file can still be overwritten!)
        var assembly = LoadAssembly(_projectAssembly, loadContext);
        if (assembly == null)
        {
            return null;
        }

        Type? baseType;
        try
        {
            baseType = assembly.GetType(ProjectMainType);
        }
        catch (Exception e)
        {
            Log(e.ToString());
            return null;
        }

        CombatRoutine? routine;
        try
        {
            routine = (CombatRoutine?)Activator.CreateInstance(baseType);
        }
        catch (Exception e)
        {
            Log(e.ToString());
            return null;
        }

        if (routine != null)
        {
            Log(ProjectName + " was loaded successfully (hot-reload mode).");
        }
        else
        {
            Log("Could not load " + ProjectName + ". This can be due to a new version of Rebornbuddy being released. An update should be ready soon.");
        }

        return routine;
    }

    /// <summary>
    /// Hot reload Magitek.dll without restarting RebornBuddy
    /// Only available in dev mode (pre-* or test-* versions)
    /// </summary>
    private void ReloadProduct()
    {
        lock (Locker)
        {
            // Safety check: hot-reload only in dev mode
            if (!IsDevVersion())
            {
                Log("Hot-reload attempted on production version - ignoring.");
                return;
            }

            if (_isReloading)
            {
                Log("Reload already in progress, skipping...");
                return;
            }

            _isReloading = true;

            try
            {
                Log("Hot-reloading Magitek.dll (direct load)...");

                // 1. Close open settings windows to prevent UI assembly mismatch crashes
                try
                {
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        var actualRoutine = _proxy?.ActualRoutine;
                        if (actualRoutine != null)
                        {
                            var formProperty = actualRoutine.GetType().GetProperty("Form",
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                            if (formProperty != null)
                            {
                                var form = formProperty.GetValue(null) as System.Windows.Window;
                                if (form != null && form.IsVisible)
                                {
                                    Log("Closing settings window before reload...");
                                    form.Close();
                                }
                            }
                        }
                    });
                }
                catch (Exception e)
                {
                    Log($"Error closing settings window: {e.Message}");
                }

                // 2. Call ShutDown on the current instance (cleans up TreeHooks, event handlers, etc.)
                if (_product != null && _shutdownMethod != null)
                {
                    try
                    {
                        _shutdownMethod.Invoke(_product, null);
                        Log("Shutdown method called successfully.");
                    }
                    catch (Exception e)
                    {
                        Log($"Error calling ShutDown: {e.Message}");
                    }
                }

                // 3. Clear proxy reference to old instance
                if (_proxy != null)
                {
                    _proxy.ActualRoutine = null;
                    Log("Proxy cleared - old instance detached.");
                }

                // 4. Clear all references to the old assembly
                _product = null;
                _initializeMethod = null;
                _shutdownMethod = null;
                _pulseMethod = null;
                _buttonMethod = null;
                _loaded = false;

                // 5. Unload the old AssemblyLoadContext (releases old temp file lock)
                if (_loadContextRef != null && _loadContextRef.IsAlive)
                {
                    var oldContext = _loadContextRef.Target as MagitekLoadContext;
                    if (oldContext != null)
                    {
                        oldContext.Unload();
                    }
                }

                // 6. Force garbage collection to clean up the old assembly
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // 7. Small delay to help GC complete
                System.Threading.Thread.Sleep(300);

                // 8. Load the new version (directly from original file, updates proxy)
                LoadProduct();

                Log("Hot-reload complete! New code loaded directly from Magitek.dll.");

                // 9. RebornBuddy continues using the proxy which now delegates to the new instance
                // when the routine is selected, and will continue calling Pulse
            }
            catch (Exception e)
            {
                Log($"Hot-reload failed: {e.Message}");
                Logging.WriteException(e);
            }
            finally
            {
                _isReloading = false;
                _lastReloadTime = DateTime.Now;
            }
        }
    }

    private void LoadProduct()
    {
        lock (Locker)
        {
            if (_product != null && !_isReloading)
            {
                return; // Already loaded and not reloading
            }

            // Dev versions use temp file + collectible AssemblyLoadContext for hot-reload
            // Production versions use simple direct load (no overhead)
            if (IsDevVersion())
            {
                _product = LoadFromTemp();
            }
            else
            {
                _product = LoadDirect();
            }

            _loaded = true;

            if (_product == null)
            {
                return;
            }

            // Update the proxy to point to the new instance
            if (_proxy != null)
            {
                _proxy.ActualRoutine = _product;
            }

            // Cache lifecycle methods
            var productType = _product.GetType();
            _initializeMethod = productType.GetMethod("Initialize");
            _shutdownMethod = productType.GetMethod("ShutDown");
            _pulseMethod = productType.GetMethod("Pulse");
            _buttonMethod = productType.GetMethod("OnButtonPress");

            // Call Initialize on the new instance
            if (_initializeMethod != null)
            {
                try
                {
                    _initializeMethod.Invoke(_product, null);
                    Log($"{ProjectName} initialized after load.");
                }
                catch (Exception e)
                {
                    Log($"Error calling Initialize: {e.Message}");
                    Logging.WriteException(e);
                }
            }

            // Set up file watcher for hot reload (only in dev mode: pre-* or test-* versions)
            if (_fileWatcher == null && IsDevVersion())
            {
                SetupFileWatcher();
            }
        }
    }

    /// <summary>
    /// Set up FileSystemWatcher to detect DLL changes for hot reload
    /// Only enabled for dev versions (pre-*, test-*) to avoid issues in production
    /// </summary>
    private void SetupFileWatcher()
    {
        try
        {
            // Watch the original DLL location
            _fileWatcher = new FileSystemWatcher
            {
                Path = _currentDirectory,
                Filter = ProjectAssemblyName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnDllChanged;

            Log($"🔥 Hot-reload ENABLED (dev version). Watching: {_projectAssembly}");
        }
        catch (Exception e)
        {
            Log($"Failed to set up hot-reload watcher: {e.Message}");
        }
    }

    /// <summary>
    /// Clean up old temp files from previous loads/sessions
    /// </summary>
    private void CleanupOldTempFiles()
    {
        try
        {
            if (!Directory.Exists(_tempDir))
                return;

            var tempFiles = Directory.GetFiles(_tempDir, "Magitek_*.dll");
            var currentFile = _currentTempAssembly;

            foreach (var file in tempFiles)
            {
                // Don't delete the current temp file
                if (!string.IsNullOrEmpty(currentFile) &&
                    string.Equals(file, currentFile, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    File.Delete(file);

                    // Also delete associated PDB
                    var pdb = Path.ChangeExtension(file, ".pdb");
                    if (File.Exists(pdb))
                    {
                        File.Delete(pdb);
                    }
                }
                catch
                {
                    // File might still be locked by GC - ignore and try next time
                }
            }
        }
        catch (Exception e)
        {
            Log($"Failed to clean up temp files: {e.Message}");
        }
    }

    /// <summary>
    /// Handle DLL file changes
    /// </summary>
    private void OnDllChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce: ignore if already reloading or too soon after last reload
        if (_isReloading || (DateTime.Now - _lastReloadTime).TotalSeconds < 3)
        {
            return;
        }

        Log($"Detected change in {ProjectAssemblyName}");

        // Disable watcher temporarily to prevent cascade
        if (_fileWatcher != null)
        {
            _fileWatcher.EnableRaisingEvents = false;
        }

        // Delay slightly to ensure file write is complete
        System.Threading.Thread.Sleep(500);

        Task.Run(() =>
        {
            ReloadProduct();

            // Re-enable watcher after reload
            if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = true;
            }
        });
    }

    /// <summary>
    /// Cleanup on disposal
    /// </summary>
    public static void Cleanup()
    {
        if (_fileWatcher != null)
        {
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Dispose();
            _fileWatcher = null;
        }
    }

    private static void Log(string message)
    {
        message = "[Auto-Updater][" + ProjectName + "] " + message;
        Logging.Write(LogColor, message);
    }

    private string? GetLocalVersion()
    {
        if (!File.Exists(_versionPath))
            return null;

        try
        {
            var version = File.ReadAllText(_versionPath).Trim();
            return version;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Check if this is a development version (enables hot-reload)
    /// </summary>
    private bool IsDevVersion()
    {
        var version = GetLocalVersion();
        if (version == null)
            return false;

        // Dev versions use "pre-" or "test-" prefix
        return version.StartsWith("pre-") || version.StartsWith("test-");
    }

    private async Task AutoUpdate()
    {
        var stopwatch = Stopwatch.StartNew();
        var local = GetLocalVersion();
        _latestVersion = await GetLatestVersion();

        if (local == _latestVersion || _latestVersion == null || (local != null && (local.StartsWith("pre-") || local.StartsWith("test-"))))
        {
            return;
        }

        Log($"Updating to Version: {_latestVersion}.");
        var bytes = await DownloadLatestVersion();

        if (bytes == null || bytes.Length == 0)
        {
            Log("[Error] Bad product data returned.");
            return;
        }

        if (!await Clean(_currentDirectory))
        {
            Log("[Error] Could not clean directory for update.");
            return;
        }

        if (!Extract(bytes, _currentDirectory))
        {
            Log("[Error] Could not extract new files.");
            return;
        }

        try
        {
            await File.WriteAllTextAsync(_versionPath, _latestVersion);
        }
        catch (Exception e)
        {
            Log(e.ToString());
        }

        stopwatch.Stop();
        Log($"Update complete in {stopwatch.ElapsedMilliseconds} ms.");
    }

    private async Task<string?> GetLatestVersion()
    {
        try
        {
            var response = await _client.GetAsync(VersionUrl);

            if (!response.IsSuccessStatusCode)
                return null;

            return (await response.Content.ReadAsStringAsync()).Trim();
        }
        catch (Exception e)
        {
            Log(e.Message);
            return null;
        }
    }

    private async Task<bool> Clean(string directory)
    {
        foreach (var file in new DirectoryInfo(directory).GetFiles())
        {
            bool deleted = false;
            int attempts = 0;
            while (!deleted && attempts < 3)
            {
                try
                {
                    file.Delete();
                    deleted = true;
                }
                catch (Exception e)
                {
                    Log($"[Error] Could not delete file {file.Name}: {e.Message}");
                    attempts++;
                    await Task.Delay(1000);
                }
            }
            if (!deleted)
            {
                Log($"[Error] Failed to delete file {file.Name} after 3 attempts.");
                return false;
            }
        }

        foreach (var dir in new DirectoryInfo(directory).GetDirectories())
        {
            bool deleted = false;
            int attempts = 0;
            while (!deleted && attempts < 3)
            {
                try
                {
                    dir.Delete(true);
                    deleted = true;
                }
                catch (Exception e)
                {
                    Log($"[Error] Could not delete directory {dir.Name}: {e.Message}");
                    attempts++;
                    await Task.Delay(1000);
                }
            }
            if (!deleted)
            {
                Log($"[Error] Failed to delete directory {dir.Name} after 3 attempts.");
                return false;
            }
        }

        return true;
    }

    private static bool Extract(byte[] files, string directory)
    {
        using var stream = new MemoryStream(files);
        var zip = new FastZip();

        try
        {
            zip.ExtractZip(stream, directory, FastZip.Overwrite.Always, null, null, null, false, true);
        }
        catch (Exception e)
        {
            Log(e.ToString());
            return false;
        }

        return true;
    }

    private async Task<byte[]?> DownloadLatestVersion()
    {
        try
        {
            var response = await _client.GetAsync(ZipUrl);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception e)
        {
            Log(e.Message);
            return null;
        }
    }
}
