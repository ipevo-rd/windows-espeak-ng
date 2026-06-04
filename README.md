# Ipevo.Windows.EspeakNg

將 [eSpeak NG](https://github.com/espeak-ng/espeak-ng)(GPLv3）的 Windows x64 執行檔與語言資料封裝成 NuGet 套件，供 IPEVO 內部 .NET 專案以「獨立行程呼叫」方式使用，並隨套件附上滿足 GPLv3 的完整對應原始碼。

- 套件 ID：`Ipevo.Windows.EspeakNg`
- 內含 eSpeak NG `1.52.0.1`（上游 commit `fbe4b376`，未修改）
- 平台：Windows x64

---

## 這個套件如何避開 GPL 感染

eSpeak NG 採 GPLv3。GPL 的「衍生著作」判定關鍵在**連結方式**，本套件以下列設計確保呼叫端程式不被 GPL 感染：

- **不連結 `libespeak-ng`**：套件內只有編譯好的 `espeak-ng.exe`（命令列工具），沒有任何 espeak 函式庫供連結。呼叫端用 `System.Diagnostics.Process` 啟動 exe，透過命令列參數與標準輸出／檔案交換資料。
- **行程隔離即 mere aggregation**：兩個程式各自獨立執行、僅以命令列與管線通訊，屬 GPL FAQ 明確認可的「單純聚集（mere aggregation）」，呼叫端維持自己的授權。
- **隨附完整對應原始碼**：套件內含 `espeak-ng/source.zip`，包含建置此 exe 所用的完整 eSpeak NG 原始碼與其相依的 libsonic 原始碼（解開即可離線重建），滿足 GPLv3「提供對應原始碼」的義務。
- **未修改原始碼**：上游原碼原樣使用，無修改標示義務。
- **helper 不觸碰 GPL 程式碼**：套件內的 `EspeakNgLocator` 是 IPEVO 自行撰寫、用來定位檔案路徑的小工具，與 eSpeak NG 原始碼無任何衍生關係。

> 紅線：請勿改以靜態或動態方式連結 `libespeak-ng`，亦勿透過共享記憶體傳遞其內部資料結構。一旦如此，行程隔離不再成立，呼叫端將被視為 GPL 衍生著作。

---

## 套件內容

安裝後，建置時整個 `espeak-ng/` 資料夾會以 `PreserveNewest` 複製到呼叫端專案的輸出目錄：

```
<輸出目錄>/
└── espeak-ng/
    ├── espeak-ng.exe          # 命令列執行檔
    ├── espeak-ng-data/        # 語言與音素資料（ESPEAK_DATA_PATH 之下一層）
    ├── COPYING                # GPLv3 授權全文
    └── source.zip             # 完整對應原始碼（含 libsonic）
```

套件同時提供一個 `netstandard2.0` 的 helper 組件（命名空間 `Ipevo.Windows.EspeakNg`），用來定位上述資源並檢查 GPL 必備檔是否齊備。

---

## 安裝

```
dotnet add package Ipevo.Windows.EspeakNg
```

套件來源為 IPEVO 私有 GitHub NuGet（`https://nuget.pkg.github.com/ipevo-rd/index.json`）。

> 本套件使用 git-lfs 儲存大型二進位檔。若是 clone 本 repo 自行建置，需先安裝 git-lfs 並 `git lfs pull`。

---

## 使用方式

最簡單的方式是用 `EspeakNgRunner`，它會以獨立行程啟動 `espeak-ng.exe`、自動設定 `ESPEAK_DATA_PATH`，維持與 GPL 程式碼的行程隔離。若需自行掌控啟動流程，再改用 `EspeakNgLocator` 取得路徑。

### 方式一：用 `EspeakNgRunner`（建議）

未傳入路徑時自動定位；回傳含成敗狀態的 `EspeakNgResult`（執行檔遺失不丟例外，回傳 `Success=false`）。

```csharp
using Ipevo.Windows.EspeakNg;

// 同步
EspeakNgResult result = EspeakNgRunner.Run("-xq -v en \"Hello world\"");
if (result.Success)
{
    Console.WriteLine(result.Phonemes); // h@l'oU w'3:ld
}

// 非同步（可帶 CancellationToken）
EspeakNgResult asyncResult = await EspeakNgRunner.RunAsync("-xq -v en \"Hello world\"");

// 也可明確傳入路徑（第二、三參數）
EspeakNgResult custom = EspeakNgRunner.Run("-xq -v en \"text\"", exePath, dataPath);
```

`EspeakNgResult` 屬性：`Success`、`ExitCode`、`Phonemes`（標準輸出）、`StandardError`、`ErrorMessage`（無法啟動時的訊息，正常時為 null）。

#### `arguments` 怎麼下

`arguments` 就是在終端機 `espeak-ng` 之後要輸入的那一串（不含 `espeak-ng` 本身），整串原樣交給行程，含空白的文字需自行用雙引號包住。常用選項：

| 選項 | 說明 |
| --- | --- |
| `-v <voice>` | 指定語言／語音，如 `-v en`、`-v cmn` |
| `-q` | 不發音（僅處理，常與 `-x` 併用） |
| `-x` | 輸出 espeak 音素記法到標準輸出 |
| `--ipa` | 輸出 IPA 音素 |
| `-w <file>` | 合成為 WAV 檔（此時標準輸出無音素） |
| 結尾文字 | 欲處理的文字，含空白時用雙引號包住 |

範例：

```csharp
EspeakNgRunner.Run("-q -x -v en \"Hello world\"");    // 取英語音素
EspeakNgRunner.Run("-q --ipa -v en \"Hello world\""); // 取 IPA 音素
EspeakNgRunner.Run("-v cmn -w out.wav \"你好\"");      // 合成中文為 WAV
```

完整選項執行 `espeak-ng --help` 或參閱上游文件。

同步與非同步皆提供，但實際啟動行程的邏輯只存在於 `RunAsync`，`Run` 委派至其上，避免重複程式碼與同步內容死結。

### 方式二：用 `EspeakNgLocator` 自行啟動

`EspeakNgLocator.Locate()` 以呼叫端的輸出目錄為基底，回傳一個 `EspeakNgLocation` 結果物件（不丟例外，由你決定如何處理檢查結果）。

```csharp
using System.Diagnostics;
using Ipevo.Windows.EspeakNg;

var loc = EspeakNgLocator.Locate();

if (!loc.IsValid)
{
    // 執行檔遺失或 GPL 必備檔不齊；loc.MissingGplFiles 列出缺少項目
    throw new InvalidOperationException(
        $"eSpeak NG deployment invalid. Missing GPL files: {string.Join(", ", loc.MissingGplFiles)}");
}

var psi = new ProcessStartInfo(loc.ExecutablePath)
{
    Arguments = "-xq -v en \"Hello world\"",
    RedirectStandardOutput = true,
    UseShellExecute = false,
};

// 引擎需以 ESPEAK_DATA_PATH 找到 espeak-ng-data
psi.Environment["ESPEAK_DATA_PATH"] = loc.DataPath;

using var process = Process.Start(psi);
string phonemes = process.StandardOutput.ReadToEnd();
process.WaitForExit();
```

### `EspeakNgLocation` 屬性

| 屬性 | 說明 |
| --- | --- |
| `RootDirectory` | payload 根目錄（即 `espeak-ng/` 資料夾） |
| `DataPath` | 應設給 `ESPEAK_DATA_PATH` 的路徑（等同 `RootDirectory`） |
| `ExecutablePath` | `espeak-ng.exe` 完整路徑 |
| `DataDirectory` | `espeak-ng-data` 資料夾完整路徑 |
| `IsExecutablePresent` | 執行檔是否存在 |
| `MissingGplFiles` | 缺少的 GPL 必備檔清單（齊備時為空） |
| `AreGplFilesPresent` | GPL 必備檔（`COPYING`、`source.zip`）是否齊備 |
| `IsValid` | 執行檔存在且 GPL 必備檔齊備 |

---

## 呼叫端需自行完成的 GPL 義務

本套件已備妥技術隔離與原始碼，但 GPLv3 的「告知與散布」義務只能由**最終散布產品的你**履行。將含本套件的程式交付給客戶／使用者時，請補上：

1. **保留並一同散布 `espeak-ng/` 整包**
   `espeak-ng.exe`、`COPYING`、`source.zip` 必須跟著你的產品一起送到每一個收到執行檔的對象手上，不可拆散或刪減。若客戶會再轉發，這包也要一併隨行。

2. **向使用者提供第三方軟體聲明**
   在產品的「關於」頁面、安裝目錄文件或授權說明中，可被合理發現地載明：
   - 本產品包含 eSpeak NG（版本與 commit），依 GPLv3 授權，著作權歸原作者
   - 授權全文見隨附的 `COPYING`，完整對應原始碼見隨附的 `source.zip`
   - 上游網址 `https://github.com/espeak-ng/espeak-ng`
   - exe 內靜態連結 libsonic（Apache 2.0），其授權與原始碼一併包含於 `source.zip`

3. **EULA 排除條款**
   若你的產品有自己的使用者授權合約，需明文聲明該合約不適用於隨附的 eSpeak NG，該部分依 GPLv3 授權。GPLv3 禁止對 GPL 部分附加額外限制。

4. **維持行程隔離的呼叫方式**
   僅以 `Process` 啟動 `espeak-ng.exe`、用命令列參數與管線／檔案交換資料。不要連結 `libespeak-ng`，不要用共享記憶體或傳遞其內部資料結構。

> 你不需要做的事：因原始碼未修改，無修改標示義務；因已隨附 `source.zip`，無須另出具書面要約（written offer）。

---

## 重新建置 exe 與資料

`source.zip` 內含 `BUILD.md`，說明如何從原始碼離線重建 `espeak-ng.exe` 與語言資料（MSVC + CMake + Ninja，並以隨附的 libsonic 取代 FetchContent 下載）。

---

## 授權

- eSpeak NG 與本套件散布的執行檔／資料：**GPLv3**（見 `espeak-ng/COPYING`）。
- libsonic（靜態連結於 exe 內）：Apache 2.0，授權與原始碼包含於 `source.zip`。
- 套件中的 `EspeakNgLocator` helper 為 IPEVO 撰寫，隨套件一同以 GPLv3 散布。

---

## 免責

以上 GPL 合規說明為工程實務上的通行理解，非法律意見。正式商用發布前，建議由法務依你所在司法管轄區對 GPLv3 的解釋再行確認。
