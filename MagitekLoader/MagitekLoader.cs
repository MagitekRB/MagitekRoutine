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
using System.Threading.Tasks;
using System.Windows.Media;
using TreeSharp;
using Action = TreeSharp.Action;

namespace MagitekLoader;

public class CombatRoutineLoader : IAddonProxy<CombatRoutine>
{
    
    private const string ProjectMainType = "Magitek.Magitek";
    private const string ProjectAssemblyName = "Magitek.dll";
    private const string ZipUrl = "https://github.com/MagitekRB/MagitekRoutine/releases/latest/download/Magitek.zip";
    private const string VersionUrl = "https://github.com/MagitekRB/MagitekRoutine/releases/latest/download/Version.txt";
    private static readonly Color LogColor = Colors.CornflowerBlue;


    private static readonly string GreyMagicAssembly = Path.Combine(Environment.CurrentDirectory, @"GreyMagic.dll");

    private string _currentDirectory;
    private string _projectAssembly;
    private string _versionPath;

    
    private static string? _latestVersion;

    private readonly HttpClient _client = new HttpClient();
    public async Task<CombatRoutine> Load(string directory)
    {
        _currentDirectory = directory;
        _projectAssembly = Path.Combine(directory, $"{ProjectAssemblyName}");
        _versionPath = Path.Combine(directory, "Version.txt");
        

        await AutoUpdate();



        return Load();
    }


    private static CombatRoutine? Product { get; set; }

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

    private static Assembly? LoadAssembly(string path)
    {
        if (!File.Exists(path)) return null;


        Assembly? assembly = null;
        try
        {
            assembly = Assembly.LoadFrom(path);
        }
        catch (Exception e)
        {
            Logging.WriteException(e);
        }

        return assembly;
    }

    private CombatRoutine? Load()
    {
        RedirectAssembly();

        var assembly = LoadAssembly(_projectAssembly);
        if (assembly == null) return null;

        CombatRoutine bb;
        try
        {
            var baseType = assembly.GetType(ProjectMainType);
            return (CombatRoutine)Activator.CreateInstance(baseType);
        }
        catch (Exception e)
        {
            Log(e.ToString());
            Log("Could not load Magitek This can be due to a new version of Rebornbuddy being released. An update should be ready soon.");
            return null;
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= Handler;
            AppDomain.CurrentDomain.AssemblyResolve -= GreyMagicHandler;
        }
    }

    private void LoadProduct()
    {
        if (Product != null) return;

        Product = Load();

        if (Product == null)
        {
            Log("Failed to load Magitek .");
        }

    }

    private static void Log(string message)
    {
        message = "[Auto-Updater][Magitek] " + message;
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

    private async Task AutoUpdate()
    {
        var stopwatch = Stopwatch.StartNew();
        var local = GetLocalVersion();
        _latestVersion = await GetLatestVersion();

        if (local == _latestVersion || _latestVersion == null || (local != null && (local.StartsWith("pre-") || local.StartsWith("test-"))))
        {
            LoadProduct();
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
        LoadProduct();
    }

    private  async Task<string?> GetLatestVersion()
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

    private async Task<bool>  Clean(string directory)
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