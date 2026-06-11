# Maintaining — 重建與升級指引

當需要**升級 espeak-ng** 或**改寫 provider** 時,照本文重建套件成品。
另見 `RELEASING.md`(發布/secret/權限)。

## Repo 結構

| 路徑 | 性質 |
| --- | --- |
| `IpevoEspeakNgProvider/` | provider 源碼(GPL,P/Invoke libespeak-ng) |
| `espeak-ng/espeak-ng.exe` `.dll` `espeak-ng-data/` | **預建二進位成品**(LFS),CI/本機重建後提交 |
| `espeak-ng/IpevoEspeakNgProvider.exe` | provider 的 AOT 成品(LFS;CI 發布時會重建覆蓋) |
| `espeak-ng/COPYING` | GPLv3 全文 |
| `espeak-ng/source.zip` | **所有 GPL 二進位的完整對應源碼**(espeak + sonic + provider 源碼 + BUILD.md) |
| `Ipevo.Windows.EspeakNg.csproj` | 純內容打包專案(不輸出 managed dll) |
| `build/` `buildTransitive/` | 部署 targets(把 `espeak-ng/` 複製到消費端輸出) |

## 前置工具

- Visual Studio,含 **C/C++ (MSVC)**;CMake 與 Ninja(VS 內建)
- .NET SDK 8+
- git、**git-lfs**(`git lfs install` 後 clone 才會取到實體二進位)

## ⚠️ 最重要的約束:espeak 版本必須對齊 Piper 模型

消費端的 Piper 模型是用**特定版本** espeak 的 `espeak_TextToPhonemes` 訓練的(目前 = **release 1.52.0**)。

- **不要**為了「用新版」就升級到 master。實測過:1.52.0 之後的版本在 API 路徑會改音素(例如英語多出顎化滑音 `ʲ`),導致與模型不對齊、合成走音。
- provider 走的是 `espeak_TextToPhonemes`(翻譯階段 API),**不是** CLI 的 `--ipa`(合成階段,會多套同位音/聲調)。這是刻意的,勿改。
- **升級 espeak = 改變音素 = 等同要重新驗證或重訓模型**。沒有重訓打算,就維持與模型相符的 espeak 版本。

---

## A. 升級 espeak-ng 到新版本

> 前提:你已確認新版音素與目標模型相容(或同時要重訓模型)。

1. 取得 espeak-ng 源碼並切到目標 tag:
   ```
   git clone https://github.com/espeak-ng/espeak-ng.git
   cd espeak-ng
   git checkout <tag>          # 例如 1.52.0
   ```
2. 在 x64 開發者環境(VsDevCmd -arch=x64)建 **shared** 版 + 資料:
   ```
   cmake -B build -G Ninja -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON
   cmake --build build --target espeak-ng       REM espeak-ng.dll
   cmake --build build --target espeak-ng-bin   REM espeak-ng.exe (動態連結 dll)
   cmake --build build --target data            REM espeak-ng-data
   ```
   - 離線建置:加 `-DFETCHCONTENT_SOURCE_DIR_SONIC-GIT=<本地 sonic> -DFETCHCONTENT_FULLY_DISCONNECTED=ON`;否則 sonic 由 FetchContent 自動下載。
   - 產物:`build/src/espeak-ng.dll`、`build/src/espeak-ng.exe`、`build/espeak-ng-data/`。
3. 更新 payload(本 repo 的 `espeak-ng/`):
   - 覆蓋 `espeak-ng.exe`、`espeak-ng.dll`
   - 刪掉舊 `espeak-ng-data/` 後整包複製新的
4. 同步更新 provider 連結用的 dll:把新的 `espeak-ng.dll` 複製到 `IpevoEspeakNgProvider/espeak-ng.dll`
5. 重建 provider(見 B 的步驟 2-3),因為它依賴的 dll 換版了
6. **重建 `source.zip`**(見 D),內含新 espeak tag 的源碼
7. 在 `Ipevo.Windows.EspeakNg.csproj` 調 `<Version>`(對齊新 espeak 版本)
8. 驗證(見 E)

---

## B. 改寫 / 修改 provider

1. 編輯 `IpevoEspeakNgProvider/`(`Program.cs`、`NativeMethods.cs`)。
   - 協定(stdin `voice\ttext` → stdout 音素)若改動,**消費端的 client 也要同步**(localTTS 自有 `EspeakPhonemeProvider`)。
2. 確認 `IpevoEspeakNgProvider/espeak-ng.dll` 存在(provider build 時 P/Invoke 需要,且應與 payload 同版)。
3. 以 Native AOT 發布:
   ```
   dotnet publish IpevoEspeakNgProvider/IpevoEspeakNgProvider.csproj -c Release -r win-x64
   ```
   - 產物:`IpevoEspeakNgProvider/bin/Release/net8.0/win-x64/publish/IpevoEspeakNgProvider.exe`
   - **vswhere 注意**:AOT link 會呼叫 `vswhere.exe`。若報「vswhere 找不到」,把
     `C:\Program Files (x86)\Microsoft Visual Studio\Installer` 加進 PATH 再試。
     (GitHub `windows-latest` 預設 PATH 已含,CI 通常不用處理。)
4. 把 exe 複製到 payload:`espeak-ng/IpevoEspeakNgProvider.exe`
   - (CI 發布時會自動做此步;手動改時要自己複製或讓 CI 重建。)
5. **重建 `source.zip`**(見 D),內含更新後的 provider 源碼
6. 驗證(見 E)

---

## C. 從零完整重生(全部成品)

依序:**espeak(A1-A3)→ provider dll 同步(A4)→ provider AOT(B2-B4)→ source.zip(D)→ 版號 → 驗證(E)**。

---

## D. 重建 source.zip(GPL 完整對應源碼)

`source.zip` 必須涵蓋套件內**每一個 GPL 二進位**的對應源碼:

```
source.zip
├── espeak-ng/              # espeak-ng 該 tag 的源碼 (git archive <tag>)
├── sonic/                  # 實際連結的 libsonic 源碼
├── IpevoEspeakNgProvider/  # provider 源碼 (Program.cs, NativeMethods.cs, .csproj)
└── BUILD.md                # 從源碼重建 dll/exe/data/provider 的說明
```

作法(PowerShell 範例):
```
# 1) espeak 源碼(用 git archive 取乾淨樹,不含 .git)
git -C <espeak-clone> archive --format=tar -o espeak.tar <tag>
mkdir stage\espeak-ng ; tar -xf espeak.tar -C stage\espeak-ng
# 2) sonic 源碼(實際連結的那份)
Copy-Item <sonic-src>\* stage\sonic -Recurse -Exclude .git
# 3) provider 源碼(只 3 個源檔，不含 bin/obj/dll)
mkdir stage\IpevoEspeakNgProvider
Copy-Item IpevoEspeakNgProvider\Program.cs,IpevoEspeakNgProvider\NativeMethods.cs,IpevoEspeakNgProvider\IpevoEspeakNgProvider.csproj stage\IpevoEspeakNgProvider\
# 4) 更新 BUILD.md 後壓成 source.zip，放回 espeak-ng\source.zip
Compress-Archive stage\espeak-ng,stage\sonic,stage\IpevoEspeakNgProvider,stage\BUILD.md espeak-ng\source.zip -Force
```

> 改了 provider 源碼或升級 espeak,**務必同步重建 source.zip**,否則違反 GPL(binary 與隨附源碼不一致)。

---

## E. 驗證

1. 打包:`dotnet pack Ipevo.Windows.EspeakNg.csproj -c Release` → 確認 nupkg **無 managed dll**(只有 `espeak-ng/` 內容 + targets)。
2. 音素正確性(關鍵):用 payload 的 provider 跑代表性句子,確認與模型對齊。
   ```
   $env:ESPEAK_DATA_PATH="espeak-ng"
   "en-us`tThis is also one of the most dramatic downturns in the history of presidents." | espeak-ng\IpevoEspeakNgProvider.exe
   ```
   - 對 1.52.0:`history` 應為 `hˈɪstɚɹi`(**無 ʲ**)。若出現 `hˈɪstɚɹiʲ`,代表 espeak 版本不對(用到 1.52.0 之後)。
3. 消費端整合:建置 N100,確認輸出目錄 `espeak-ng/` 有 provider+dll+data、**無** managed helper dll,且發音正常。
4. LFS:`git lfs status` 確認 `*.exe`、`*.dll`、`source.zip`、`espeak-ng-data/**` 走 LFS。

---

## 別忘了

- payload 的 `espeak-ng.dll` 與 `IpevoEspeakNgProvider/espeak-ng.dll` **必須同版**。
- 消費端(localTTS)的 `EspeakPhonemeProvider` 與本 provider 的 stdin/stdout 協定要一致。
- 升級/改動後,套件 `<Version>` 要進版,發布走 `RELEASING.md`。
