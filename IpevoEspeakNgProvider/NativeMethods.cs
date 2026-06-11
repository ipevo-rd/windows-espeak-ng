// windows-espeak-ng — eSpeak NG packaging and phonemizer for Windows.
// Copyright (C) 2026 IPEVO Inc.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Runtime.InteropServices;

namespace Ipevo.Windows.EspeakNg.Provider;

/// <summary>
/// libespeak-ng (espeak-ng.dll) 的最小 P/Invoke 介面，僅暴露「文字 → IPA 音素串」所需函式。
/// 函式名稱沿用 espeak-ng 原始 C 名以便對照官方文件，違反專案 camelCase 慣例屬刻意例外。
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>
    /// libespeak-ng DLL 名稱 (Windows 為 espeak-ng.dll)。
    /// </summary>
    private const string DllName = "espeak-ng";

    /// <summary>
    /// espeak_Initialize 的 output 參數：同步模式 (本程序不取 PCM，純為符合 API 必填)。
    /// </summary>
    public const int AudioOutputSynchronous = 2;

    /// <summary>
    /// espeak_TextToPhonemes 的 textmode 參數：UTF-8 字串。
    /// </summary>
    public const int EspeakCharsUtf8 = 1;

    /// <summary>
    /// espeak_TextToPhonemes 的 phonememode 參數：輸出 IPA 字元 (不加 0x80 TIE)，對齊 Piper 訓練端。
    /// </summary>
    public const int EspeakPhonemesIpa = 0x02;

    /// <summary>
    /// espeak_ERROR 的成功值 (EE_OK)。
    /// </summary>
    public const int EspeakOk = 0;

    /// <summary>
    /// 初始化 espeak-ng；必須在其他函式之前呼叫一次。
    /// </summary>
    /// <param name="output">音訊輸出模式；本程序傳 <see cref="AudioOutputSynchronous"/>。</param>
    /// <param name="bufLength">音訊緩衝長度 (毫秒)，傳 0 取預設。</param>
    /// <param name="path">含 espeak-ng-data 的「父」目錄。</param>
    /// <param name="options">位元旗標，傳 0 取預設。</param>
    /// <returns>取樣率 (Hz)；失敗回負值。</returns>
    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int espeak_Initialize(int output, int bufLength, string path, int options);

    /// <summary>
    /// 依名稱選擇 espeak voice；換語言或口音時呼叫。
    /// </summary>
    /// <param name="name">voice 名稱，例如 "en-us"、"de"。</param>
    /// <returns><see cref="EspeakOk"/> 成功；其餘為 espeak_ERROR。</returns>
    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int espeak_SetVoiceByName(string name);

    /// <summary>
    /// 將文字翻成音素串，每次呼叫吐出一個 clause 的音素，並把 <paramref name="textPtr"/> 推進到下個 clause。
    /// 全文消耗完畢時 <paramref name="textPtr"/> 會被設為 <see cref="IntPtr.Zero"/>。
    /// </summary>
    /// <param name="textPtr">指向 UTF-8 文字的指標 (ref，espeak 會原地推進)。</param>
    /// <param name="textMode">字元編碼，傳 <see cref="EspeakCharsUtf8"/>。</param>
    /// <param name="phonemeMode">音素格式，傳 <see cref="EspeakPhonemesIpa"/>。</param>
    /// <returns>指向 UTF-8 音素字串的內部 buffer 指標；須立即拷出。文字耗盡時回 <see cref="IntPtr.Zero"/>。</returns>
    [LibraryImport(DllName, EntryPoint = "espeak_TextToPhonemes")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr espeak_TextToPhonemes(ref IntPtr textPtr, int textMode, int phonemeMode);
}
