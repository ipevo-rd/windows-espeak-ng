using System.Collections.Generic;

namespace Ipevo.Windows.EspeakNg;

/// <summary>
/// 描述 eSpeak NG 部署位置的解析結果，包含執行檔與資料夾路徑及 GPL 合規檢查狀態。
/// </summary>
/// <param name="rootDirectory">
/// payload 根目錄，亦即應設給 ESPEAK_DATA_PATH 的路徑。
/// </param>
/// <param name="executablePath">
/// espeak-ng.exe 的完整路徑。
/// </param>
/// <param name="dataDirectory">
/// espeak-ng-data 語言資料夾的完整路徑。
/// </param>
/// <param name="isExecutablePresent">
/// 執行檔是否存在。
/// </param>
/// <param name="missingGplFiles">
/// 缺少的 GPL 必備檔案清單（相對於根目錄），齊備時為空集合。
/// </param>
public sealed class EspeakNgLocation(
    string rootDirectory,
    string executablePath,
    string dataDirectory,
    bool isExecutablePresent,
    IReadOnlyList<string> missingGplFiles)
{
    /// <summary>
    /// payload 根目錄；同時是應設給環境變數 ESPEAK_DATA_PATH 的值。
    /// </summary>
    public string RootDirectory { get; } = rootDirectory;

    /// <summary>
    /// 應設給環境變數 ESPEAK_DATA_PATH 的路徑（等同 <see cref="RootDirectory"/>）。
    /// </summary>
    public string DataPath => this.RootDirectory;

    /// <summary>
    /// espeak-ng.exe 的完整路徑。
    /// </summary>
    public string ExecutablePath { get; } = executablePath;

    /// <summary>
    /// espeak-ng-data 語言資料夾的完整路徑。
    /// </summary>
    public string DataDirectory { get; } = dataDirectory;

    /// <summary>
    /// 執行檔是否存在。
    /// </summary>
    public bool IsExecutablePresent { get; } = isExecutablePresent;

    /// <summary>
    /// 缺少的 GPL 必備檔案清單（相對於根目錄）；齊備時為空集合。
    /// </summary>
    public IReadOnlyList<string> MissingGplFiles { get; } = missingGplFiles;

    /// <summary>
    /// GPL 必備檔案是否全數齊備。
    /// </summary>
    public bool AreGplFilesPresent => this.MissingGplFiles.Count == 0;

    /// <summary>
    /// 部署是否可用且合規：執行檔存在且 GPL 必備檔案齊備。
    /// </summary>
    public bool IsValid => this.IsExecutablePresent && this.AreGplFilesPresent;
}
