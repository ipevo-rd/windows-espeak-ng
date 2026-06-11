# Releasing & 維護者須知

本 repo 為公開的 GPLv3 套件,但發布到公司私倉(GitHub Packages, org feed)。
發布鑰匙必須只在維護者手上。以下為發布流程與 repo 安全設定建議。

## 發布流程

發布由 **打 release tag** 觸發(見 `.github/workflows/publish.yml`):

1. 確認要發的版本(套件版號慣例:`1.52.0.x`,其中 `1.52.0` 對齊內含的 eSpeak NG 版本,第 4 碼為打包修訂)。
2. 打並推 tag,格式 `release-vX.Y.Z[.W]` 或 `release-X.Y.Z[.W]`,例如:
   ```
   git tag release-v1.52.0.3
   git push origin release-v1.52.0.3
   ```
3. CI(`windows-latest`)會:
   - 以 Native AOT 重建 `IpevoEspeakNgProvider` → 更新 `espeak-ng/IpevoEspeakNgProvider.exe`
   - 以 tag 版號 `dotnet pack`
   - 用 `secrets.NUGET_PAT` 推到 `nuget.pkg.github.com/<owner>`(`--skip-duplicate`)

> `workflow_dispatch`(手動觸發)不帶版號 → 只做建置檢查,不發布。

## 發布鑰匙(secret)

- 使用公司 org secret **`NUGET_PAT`**(機器人帳號,如 `rd-cicd`),所有 repo 推倉共用。
- PAT 範圍最小化:`write:packages` + `read:packages`;設到期日、定期輪替。
- 存放:**Organization → Settings → Secrets and variables → Actions → organization secret**,
  Repository access 選「Selected repositories」並加入本 repo。**不要**每個 repo 各存一份。

## Repo 安全設定建議(公開 repo)

公開後外人只能 fork / 開 PR,**動不了 repo、也碰不到 secret**(fork PR 無 secret、token 唯讀)。
真正能接觸 key 的是「有 write 權限、可改 workflow 的人」。請收緊到核心維護者:

1. **確認 org Base permissions**(Org → Member privileges)。若為 `Write`,則**所有 org 成員**預設都能 push 本 repo;握有發布 key 的 repo 建議收緊(改成 Read,再用 team 授予特定人 write)。
2. **write / 維護權限只給核心維護者**(個人或 team),不要全組織。
3. **Tag protection / Ruleset**:限制只有維護者能建立 `release*` tag(發布的實際觸發點)。
4. **Branch protection(main)**:要求 PR review、要求 CI 通過、限制直接 push。
5. **Actions → Fork PR workflows**:對 outside collaborators 開啟「Require approval to run workflows」。

## 不要做(會漏 key 或破壞合規)

- **不要在任何 workflow 加 `pull_request` 觸發去使用 secret**;尤其**絕不用 `pull_request_target` 去 checkout/執行不信任的 PR 程式碼**(唯一會把 secret 漏給 fork 的寫法)。
- **不要改動發布觸發條件**讓非 tag 事件能跑到推倉步驟。

## GPL 合規(發布前自檢)

- `espeak-ng/source.zip` 必須是**套件內所有 GPL 二進位**的完整對應源碼:
  espeak-ng(1.52.0)+ libsonic + `IpevoEspeakNgProvider/` 源碼 + `BUILD.md`。
  → 若改了 provider 源碼或升級 espeak,**記得同步重建 source.zip**。
- `espeak-ng/COPYING`(GPLv3)與根目錄 `COPYING` 保留。
- 套件 `PackageLicenseExpression` 維持 `GPL-3.0-or-later`。
- provider 在 CI 由源碼重建,確保 payload 的 exe 與隨附源碼一致。
