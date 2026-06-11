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
using System.Text;

namespace Ipevo.Windows.EspeakNg.Provider;

/// <summary>
/// 常駐的 espeak-ng 音素提供者：以行程隔離方式被主程式呼叫，內部連結 libespeak-ng，
/// 透過 espeak_TextToPhonemes (API 路徑) 取得與 Piper 訓練端對齊的音素。
///
/// 協定 (逐行、UTF-8)：
///   stdin  每行一個請求：&lt;voice&gt;\t&lt;text&gt;
///   stdout 每行一個回應：該句音素 (各 clause 以單一空白接起)；無音素或請求格式錯誤時回空行。
///
/// 回傳為 espeak 原始音素輸出 (含可能的 (lang) 旗標、未做 NFD)；
/// piper 對齊用的後處理 (去語言旗標、NFD) 由呼叫端負責，保持本程序為純 espeak 音素器。
/// </summary>
internal static class Program
{
    /// <summary>
    /// 上次成功設定的 voice；與本次相同時略過 espeak_SetVoiceByName。
    /// </summary>
    private static string? lastVoice;

    /// <summary>
    /// 初始化 espeak-ng，然後進入 stdin/stdout 逐行迴圈直到 stdin 關閉。
    /// </summary>
    /// <remarks>
    /// 資料目錄固定為**執行檔所在目錄**：provider 自帶與所連結 dll 同版的 espeak-ng-data 於自身旁，
    /// 直接用自己的，不吃外部 ESPEAK_DATA_PATH，避免被指到不相符版本的資料而與模型失準。
    /// </remarks>
    /// <returns>0 正常結束；1 初始化失敗。</returns>
    private static int Main()
    {
        var dataPath = AppContext.BaseDirectory;

        var rc = NativeMethods.espeak_Initialize(NativeMethods.AudioOutputSynchronous, 0, dataPath, 0);
        if (rc < 0)
        {
            Console.Error.WriteLine($"espeak_Initialize failed (rc={rc}); data path: '{dataPath}'.");
            return 1;
        }

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        using var reader = new StreamReader(Console.OpenStandardInput(), utf8);
        using var writer = new StreamWriter(Console.OpenStandardOutput(), utf8) { AutoFlush = true };

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            writer.WriteLine(Handle(line));
        }

        return 0;
    }

    /// <summary>
    /// 解析一行請求 (&lt;voice&gt;\t&lt;text&gt;)，必要時切換 voice，回傳該句音素字串。
    /// 格式錯誤、空 voice/text、或 voice 設定失敗時回空字串。
    /// </summary>
    /// <param name="line">一行請求。</param>
    /// <returns>音素字串或空字串。</returns>
    private static string Handle(string line)
    {
        var tab = line.IndexOf('\t');
        if (tab < 0) return string.Empty;

        var voice = line[..tab];
        var text = line[(tab + 1)..];
        if (voice.Length == 0 || text.Length == 0) return string.Empty;

        if (!string.Equals(voice, lastVoice, StringComparison.Ordinal))
        {
            if (NativeMethods.espeak_SetVoiceByName(voice) != NativeMethods.EspeakOk) return string.Empty;
            lastVoice = voice;
        }

        return Phonemize(text);
    }

    /// <summary>
    /// loop 呼叫 espeak_TextToPhonemes 直到文字耗盡，回各 clause 音素以單一空白接起的字串。
    /// </summary>
    /// <param name="text">要轉音素的文字。</param>
    /// <returns>音素字串 (clause 間以空白分隔)。</returns>
    private static string Phonemize(string text)
    {
        var buffer = Marshal.StringToCoTaskMemUTF8(text);
        try
        {
            var pointer = buffer;
            var builder = new StringBuilder();

            // 防呆上限：espeak 正常會在文字耗盡後把指標設 null；異常時硬性限制 4096 個 clause。
            const int safetyLimit = 4096;
            for (var i = 0; i < safetyLimit && pointer != IntPtr.Zero; i++)
            {
                var resultPointer = NativeMethods.espeak_TextToPhonemes(
                    ref pointer,
                    NativeMethods.EspeakCharsUtf8,
                    NativeMethods.EspeakPhonemesIpa);

                if (resultPointer == IntPtr.Zero) break;

                var clause = Marshal.PtrToStringUTF8(resultPointer);
                if (!string.IsNullOrEmpty(clause))
                {
                    if (builder.Length > 0) builder.Append(' ');
                    builder.Append(clause);
                }
            }

            return builder.ToString();
        }
        finally
        {
            Marshal.FreeCoTaskMem(buffer);
        }
    }
}
