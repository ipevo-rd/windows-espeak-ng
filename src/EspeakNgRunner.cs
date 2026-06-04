using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ipevo.Windows.EspeakNg;

/// <summary>
/// 以獨立行程方式呼叫 espeak-ng.exe，維持與 GPL 程式碼的行程隔離。
/// </summary>
/// <remarks>
/// 實際啟動行程的邏輯只存在於 <see cref="RunAsync(string, string?, string?, CancellationToken)"/>；
/// 同步的 <see cref="Run(string, string?, string?)"/> 委派至非同步版本，於執行緒集區上等待結果，
/// 避免重複的行程啟動程式碼，亦避免擷取呼叫端同步內容造成死結。
/// </remarks>
public static class EspeakNgRunner
{
    /// <summary>
    /// 同步呼叫 espeak-ng.exe。
    /// </summary>
    /// <param name="arguments">
    /// 傳給執行檔的命令列參數，格式同在終端機 espeak-ng 之後輸入的內容；
    /// 詳見 <see cref="RunAsync(string, string?, string?, CancellationToken)"/> 的說明與範例。
    /// </param>
    /// <param name="executablePath">執行檔路徑；傳入 null 時自動定位。</param>
    /// <param name="dataPath">設給 ESPEAK_DATA_PATH 的路徑；傳入 null 時自動定位。</param>
    /// <returns>呼叫結果。</returns>
    public static EspeakNgResult Run(
        string arguments,
        string? executablePath = null,
        string? dataPath = null)
    {
        return Task
            .Run(() => RunAsync(arguments, executablePath, dataPath))
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 非同步呼叫 espeak-ng.exe；這是唯一實際啟動行程的進入點。
    /// </summary>
    /// <remarks>
    /// 未傳入路徑時，以 <see cref="EspeakNgLocator.Locate(string?)"/> 自動定位執行檔與資料夾。
    /// 執行檔不存在時不丟例外，改回傳 <see cref="EspeakNgResult.Success"/> 為 false 的結果。
    /// 取消時會終止行程並丟出 <see cref="OperationCanceledException"/>。
    /// </remarks>
    /// <param name="arguments">
    /// 傳給執行檔的命令列參數字串，內容即終端機 espeak-ng 之後輸入的部分（不含 espeak-ng 本身）。
    /// 整串會原樣交給 <see cref="ProcessStartInfo.Arguments"/>，故含空白的文字需自行用雙引號包住。
    /// 常用選項：
    /// <list type="bullet">
    ///   <item><description>-v &lt;voice&gt;：指定語言或語音，如 -v en、-v cmn。</description></item>
    ///   <item><description>-q：不發音（僅做處理，常與 -x 併用）。</description></item>
    ///   <item><description>-x：輸出 espeak 音素記法到標準輸出；--ipa 則輸出 IPA 音素。</description></item>
    ///   <item><description>-w &lt;file&gt;：合成為 WAV 檔（此時標準輸出無音素）。</description></item>
    ///   <item><description>結尾接欲處理的文字，含空白時以雙引號包住。</description></item>
    /// </list>
    /// 範例：
    /// <list type="bullet">
    ///   <item><description>取英語音素：-q -x -v en "Hello world"</description></item>
    ///   <item><description>取 IPA 音素：-q --ipa -v en "Hello world"</description></item>
    ///   <item><description>合成中文為 WAV：-v cmn -w out.wav "你好"</description></item>
    /// </list>
    /// 完整選項可執行 espeak-ng --help，或參閱上游文件。
    /// </param>
    /// <param name="executablePath">執行檔路徑；傳入 null 時自動定位。</param>
    /// <param name="dataPath">設給 ESPEAK_DATA_PATH 的路徑；傳入 null 時自動定位。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>呼叫結果。</returns>
    public static async Task<EspeakNgResult> RunAsync(
        string arguments,
        string? executablePath = null,
        string? dataPath = null,
        CancellationToken cancellationToken = default)
    {
        if (executablePath is null || dataPath is null)
        {
            EspeakNgLocation location = EspeakNgLocator.Locate();
            executablePath ??= location.ExecutablePath;
            dataPath ??= location.DataPath;
        }

        if (!File.Exists(executablePath))
        {
            return new EspeakNgResult(false, -1, string.Empty, string.Empty, $"Executable not found: {executablePath}");
        }

        ProcessStartInfo startInfo = new(executablePath)
        {
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.Environment["ESPEAK_DATA_PATH"] = dataPath;

        using Process process = new() { StartInfo = startInfo };
        process.Start();

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        string stdout = await stdoutTask.ConfigureAwait(false);
        string stderr = await stderrTask.ConfigureAwait(false);

        return new EspeakNgResult(process.ExitCode == 0, process.ExitCode, stdout, stderr, null);
    }

    /// <summary>
    /// 嘗試終止行程，忽略終止過程中的例外。
    /// </summary>
    /// <param name="process">欲終止的行程。</param>
    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
        catch
        {
            // 行程可能已自行結束，終止失敗不影響呼叫端取消語意。
        }
    }
}
