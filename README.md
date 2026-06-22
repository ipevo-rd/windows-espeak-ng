# Ipevo.Windows.EspeakNg

GPLv3, Windows x64 套件:把 [eSpeak NG](https://github.com/espeak-ng/espeak-ng) `1.52.0` 與一支常駐音素提供者打包成可部署的內容套件,供 .NET 專案以**獨立行程**方式呼叫,取得與 Piper 對齊的音素。

- 套件 ID:`Ipevo.Windows.EspeakNg`(**內容套件,不含任何 managed dll**)
- 內含 eSpeak NG release tag `1.52.0`(未修改)
- 平台:Windows x64
- 授權:**GPLv3**(見 [授權](#授權))

---

## 為什麼是 GPL

本套件 bundle 了 eSpeak NG(GPLv3)的二進位,並含一支**連結 libespeak-ng** 的執行檔 `IpevoEspeakNgProvider`,因此整包受 GPLv3 約束、在此開源。

設計上以**行程邊界**隔離 GPL:消費端**不連結**本套件任何程式碼(本套件根本不提供 managed dll),只是以 `Process` 啟動其中的執行檔、透過 stdin/stdout 通訊。依 GPL FAQ,這屬「單純聚集(mere aggregation)」,呼叫端自身的程式不會成為 GPL 衍生著作。

### 此架構的依據

- **官方專案的同型先例**:eSpeak NG 官方 org 底下的 [espeak-ng-ios-app](https://github.com/espeak-ng/espeak-ng-ios-app/blob/master/LICENSE.md) 採用相同策略——其 Audio Unit Extension 靜態連結 libespeak-ng 因而繼承 GPLv3,而前端 UI「does not linked with libespeak-ng … communicates with Audio Unit with XPC」,故前端維持 MIT 授權。本套件的 `IpevoEspeakNgProvider`(連結 dll、GPLv3)對應其 Extension;消費端走 stdin/stdout 對應其前端走 XPC,法律論點同一套,僅 IPC 機制不同。
- **授權不會放寬**:社群曾於 [Issue #2131](https://github.com/espeak-ng/espeak-ng/issues/2131) 請求改為 LGPL(理由正是「多數人只用來做音素生成,GPL 限制商用」),結果為 *Closed as not planned*。GPL 是 eSpeak NG 的長期前提,行程隔離是必要設計而非過渡權宜。

> 注意:消費端若**再散布**本套件的二進位給第三方,仍須履行 GPLv3 義務(隨附 `espeak-ng/source.zip` 與 `COPYING`、提供第三方聲明)。GPL 的隔離保護的是「呼叫端程式碼的授權」,不是「免除散布義務」。

---

## 套件內容

安裝後,build targets 會把整個 `espeak-ng/` 資料夾以 `PreserveNewest` 複製到消費端輸出目錄:

```
<輸出目錄>/
└── espeak-ng/
    ├── IpevoEspeakNgProvider.exe   # 常駐音素提供者 (Native AOT，連結 libespeak-ng)
    ├── espeak-ng.exe               # 標準 eSpeak NG CLI
    ├── espeak-ng.dll               # libespeak-ng 引擎 (native)
    ├── espeak-ng-data/             # 語言與音素資料
    ├── COPYING                     # GPLv3 授權全文
    └── source.zip                  # 完整對應原始碼 (espeak-ng 1.52.0 + libsonic + BUILD.md)
```

本套件**不提供 lib/ 下的 managed assembly**;消費端無 dll 可參考,只會叫用上述 exe。

---

## 使用方式 (消費端)

消費端以行程方式叫用,不連結任何東西。

### IpevoEspeakNgProvider — 取對齊音素 (建議)

常駐行程,逐行協定 (UTF-8):

- stdin 每行一個請求:`<voice>\t<text>`
- stdout 每行一個回應:該句音素 (各 clause 以單一空白接起)

它內部呼叫 `espeak_TextToPhonemes`(翻譯階段 / API 路徑),輸出與 piper-phonemize / Piper 訓練端對齊。回傳為 espeak 原始音素 (可能含 `(lang)` 旗標、未 NFD);piper 用的後處理 (去旗標、NFD) 由消費端負責。

provider 固定使用**自己執行檔所在目錄**作為資料目錄 (自帶與所連結 dll 同版的 `espeak-ng-data`),不讀取外部 `ESPEAK_DATA_PATH`,避免被指到版本不符的資料而與模型失準。消費端無須設定任何環境變數。

最小 C# 範例 (消費端自有碼,不連結本套件):

```csharp
var exe = Path.Combine(AppContext.BaseDirectory, "espeak-ng", "IpevoEspeakNgProvider.exe");
var psi = new ProcessStartInfo(exe)
{
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    UseShellExecute = false,
    StandardInputEncoding = new UTF8Encoding(false),
    StandardOutputEncoding = new UTF8Encoding(false),
};

using var p = Process.Start(psi)!;
p.StandardInput.AutoFlush = true;
p.StandardInput.WriteLine("en-us\tHello world");
string phonemes = p.StandardOutput.ReadLine();   // həlˈoʊ wˈɜːld
```

> 為什麼用獨立 provider 而非 `espeak-ng.exe --ipa`:CLI 的 `--ipa` 走 `espeak_Synth` 合成路徑,會多套合成階段的同位音/聲調 (如英語的顎化滑音 ʲ、義語 r→ɾ、越語聲調數字),與訓練端的 `espeak_TextToPhonemes` 不一致。provider 走 API 路徑,精準對齊。

### espeak-ng.exe — 一般 espeak CLI

標準 eSpeak NG 命令列工具,需要時直接以行程叫用。它與 provider 不同,會讀取 `ESPEAK_DATA_PATH`;若資料不在預設位置,啟動時以環境變數指向含 `espeak-ng-data` 的父目錄 (即部署的 `espeak-ng` 資料夾)。

---

## 從原始碼建置

`espeak-ng/source.zip` 內含 `BUILD.md`,說明如何離線重建 `espeak-ng.dll` / `espeak-ng.exe` / 資料 (MSVC + CMake + Ninja,以隨附 libsonic 取代 FetchContent)。

`IpevoEspeakNgProvider`(本 repo 的子專案)以 Native AOT 建置,P/Invoke `espeak-ng.dll` 呼叫 `espeak_TextToPhonemes`:

```
dotnet publish IpevoEspeakNgProvider -c Release -r win-x64
```

---

## 授權

- **eSpeak NG / libespeak-ng / espeak-ng.exe / espeak-ng-data**:GPLv3,著作權歸原作者 (見 `espeak-ng/COPYING`)。
- **IpevoEspeakNgProvider**:IPEVO 撰寫,連結 libespeak-ng,故依 **GPLv3** 授權;完整對應原始碼見 `espeak-ng/source.zip` 與本 repo。
- **libsonic**(靜態連結於 espeak 引擎內):Apache-2.0,授權與原始碼包含於 `source.zip`(`sonic/LICENSE`)。

整包以 **GPL-3.0-or-later** 發布。

---

## 免責

以上 GPL 合規說明為工程實務上的通行理解,非法律意見。正式發布前建議由法務確認。
