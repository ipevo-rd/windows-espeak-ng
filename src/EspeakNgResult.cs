namespace Ipevo.Windows.EspeakNg;

/// <summary>
/// 描述一次 espeak-ng.exe 行程呼叫的結果，包含輸出內容與成敗狀態。
/// </summary>
/// <param name="success">
/// 是否成功（行程順利啟動且離開代碼為 0）。
/// </param>
/// <param name="exitCode">
/// 行程離開代碼；無法啟動時為 -1。
/// </param>
/// <param name="phonemes">
/// 標準輸出內容（音素查詢時即為音素字串）。
/// </param>
/// <param name="standardError">
/// 標準錯誤輸出內容。
/// </param>
/// <param name="errorMessage">
/// 無法啟動行程時的錯誤訊息；正常執行時為 null。
/// </param>
public sealed class EspeakNgResult(
    bool success,
    int exitCode,
    string phonemes,
    string standardError,
    string? errorMessage)
{
    /// <summary>
    /// 是否成功：行程順利啟動且離開代碼為 0。
    /// </summary>
    public bool Success { get; } = success;

    /// <summary>
    /// 行程離開代碼；無法啟動時為 -1。
    /// </summary>
    public int ExitCode { get; } = exitCode;

    /// <summary>
    /// 標準輸出內容；以音素相關參數呼叫時即為音素字串。
    /// </summary>
    public string Phonemes { get; } = phonemes;

    /// <summary>
    /// 標準錯誤輸出內容。
    /// </summary>
    public string StandardError { get; } = standardError;

    /// <summary>
    /// 無法啟動行程時的錯誤訊息（例如執行檔遺失）；正常執行時為 null。
    /// </summary>
    public string? ErrorMessage { get; } = errorMessage;
}
