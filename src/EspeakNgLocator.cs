using System;
using System.Collections.Generic;
using System.IO;

namespace Ipevo.Windows.EspeakNg;

/// <summary>
/// 定位隨套件部署的 eSpeak NG 執行檔與語言資料夾，並檢查 GPL 必備檔案是否齊備。
/// </summary>
public static class EspeakNgLocator
{
    /// <summary>
    /// 套件內容部署到輸出目錄後的子資料夾名稱。
    /// </summary>
    private const string PayloadFolderName = "espeak-ng";

    /// <summary>
    /// eSpeak NG 執行檔名稱。
    /// </summary>
    private const string ExecutableFileName = "espeak-ng.exe";

    /// <summary>
    /// 語言資料子資料夾名稱（ESPEAK_DATA_PATH 之下一層）。
    /// </summary>
    private const string DataFolderName = "espeak-ng-data";

    /// <summary>
    /// GPL 合規必備檔案，路徑相對於 payload 根目錄。
    /// </summary>
    private static readonly string[] GplRequiredFiles = ["COPYING", "source.zip"];

    /// <summary>
    /// 以指定基底目錄定位 eSpeak NG 資源並檢查完整性。
    /// </summary>
    /// <remarks>
    /// 套件透過 build/targets 將整個 espeak-ng 資料夾複製到使用者專案的輸出目錄，
    /// 因此預設以 <see cref="AppContext.BaseDirectory"/> 為基底，往下尋找 espeak-ng 資料夾。
    /// ESPEAK_DATA_PATH 應設為 <see cref="EspeakNgLocation.DataPath"/>（即 payload 根目錄），
    /// 執行階段引擎會於其下層尋找 espeak-ng-data。
    /// </remarks>
    /// <param name="baseDirectory">
    /// 內容所在的基底目錄；傳入 null 時使用應用程式輸出目錄。
    /// </param>
    /// <returns>
    /// 包含解析後路徑與檢查結果的 <see cref="EspeakNgLocation"/>。
    /// </returns>
    public static EspeakNgLocation Locate(string? baseDirectory = null)
    {
        string root = Path.Combine(baseDirectory ?? AppContext.BaseDirectory, PayloadFolderName);

        string executablePath = Path.Combine(root, ExecutableFileName);

        string dataDirectory = Path.Combine(root, DataFolderName);

        bool executablePresent = File.Exists(executablePath);

        List<string> missingGplFiles = [];

        foreach (string relativePath in GplRequiredFiles)
        {
            if (!File.Exists(Path.Combine(root, relativePath)))
            {
                missingGplFiles.Add(relativePath);
            }
        }

        return new EspeakNgLocation(
            root,
            executablePath,
            dataDirectory,
            executablePresent,
            missingGplFiles.AsReadOnly());
    }
}
