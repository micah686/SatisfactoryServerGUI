using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SatisfactoryServerManager.Helpers;

public static class SteamCmdHelper
{
    private static readonly HttpClient HttpClientInstance = new();
    private const string WindowsUrl = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
    private const string LinuxUrl = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz";
    
    public static async Task<string> InstallAsync(string installDir)
    {
        Directory.CreateDirectory(installDir);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string zipPath = Path.Combine(installDir, "steamcmd.zip");
            await DownloadAsync(WindowsUrl, zipPath);

            ZipFile.ExtractToDirectory(zipPath, installDir, overwriteFiles: true);
            File.Delete(zipPath);

            return Path.Combine(installDir, "steamcmd.exe");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string tarPath = Path.Combine(installDir, "steamcmd_linux.tar.gz");
            await DownloadAsync(LinuxUrl, tarPath);

            await RunProcessAsync("tar", $"-xzf \"{tarPath}\" -C \"{installDir}\"");
            File.Delete(tarPath);

            return Path.Combine(installDir, "steamcmd.sh");
        }

        throw new PlatformNotSupportedException("SteamCMD install only supports Windows and Linux here.");
    }
    private static async Task DownloadAsync(string url, string outputPath)
    {
        using var response = await HttpClientInstance.GetAsync(url);
        response.EnsureSuccessStatusCode();

        await using var fs = File.Create(outputPath);
        await response.Content.CopyToAsync(fs);
    }
    
    private static async Task<int> RunProcessAsync(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi)!;

        process.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();
        return process.ExitCode;
    }

}