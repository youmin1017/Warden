# Warden

[English](README.md) | 繁體中文

一個 .NET 10 + Nuxt 4 後台管理系統範本,內建:RBAC(角色 + 細粒度權限,支援 `user.*`、`*`
等萬用字元)、以 OIDC(例如 Google)登入並由系統自行簽發 JWT 工作階段、使用者自有且可限制權限範圍的
API Key,以及類似 Orchard 的後台介面,可在執行期間管理使用者、角色/權限與 API Key。

## 技術堆疊

- **後端**:ASP.NET Core 10 Web API,採 MVCS 架構(Model / Controller / Service — 沒有
  Repository 層,Service 直接使用 EF Core 的 `DbContext`)、EF Core 10,可切換
  SQLite / PostgreSQL / MariaDB,OIDC 登入 + JWT Bearer 驗證(access + refresh token),
  並在同一個 `Bearer` header 上支援 API Key(個人存取權杖)驗證。
- **前端**:Nuxt 4 + `@nuxt/ui` v4 + Pinia。

## 專案結構

```
src/
  Warden.Domain/         實體、PermissionConstants — 不依賴任何框架
  Warden.Application/    Service、DTO、權限/驗證相關邏輯
  Warden.Infrastructure/ EF Core DbContext(每個資料庫各一)、migrations、種子資料
  Warden.WebApi/         Program.cs、Controller、appsettings.json
frontend/                Nuxt 4 後台前端
```

## 以此範本建立新專案

本 repo 本身就是一個 [`dotnet new` 範本](https://learn.microsoft.com/dotnet/core/tools/custom-templates)
— 產生新專案時,會把所有 `Warden`/`warden`(命名空間、專案/方案檔、種子 email 網域、JWT
issuer/audience、cookie 名稱、docker-compose 服務名稱、頁面標題)都換成你指定的名稱,不需任何手動修改即可編譯。

在 repo 根目錄安裝一次:

```
dotnet new install .
```

之後在任何地方產生新專案:

```
dotnet new warden -n AcmeCrm -o ../AcmeCrm
```

會產生 `AcmeCrm.sln`、`src/AcmeCrm.Domain`、`src/AcmeCrm.WebApi`、`AcmeCrm.Client` JWT
audience、`acmecrm.db` 連線字串、`admin@acmecrm.local` 種子帳號、`acmecrm_refresh_token`
cookie 等,全部一致改名。照常在輸出目錄執行 `dotnet build` 與 `pnpm install` 即可確認能編譯。

範本更新後要重新安裝:`dotnet new install . --force`。
要移除:在 repo 根目錄執行 `dotnet new uninstall .`。

## 事前準備

- .NET 10 SDK
- Node.js + [pnpm](https://pnpm.io)
- 一個 OIDC 提供者的應用程式註冊(例如 Google OAuth client)— 見下文
- (選用)Docker,若要用 `docker-compose.yml` 跑 PostgreSQL 或 MariaDB 取代 SQLite

## 啟動後端

```
cd src/Warden.WebApi
dotnet run
```

第一次執行時會套用 EF Core migrations 並寫入種子資料:

- 擁有 `*` 萬用權限的 `SuperAdmin` 角色
- 一個對應 `SeedAdmin:Email` 的管理員佔位帳號(預設 `admin@warden.local`,可透過
  `appsettings.json` 的 `SeedAdmin` 區段或 `SeedAdmin__*` 環境變數覆寫)。此帳號沒有密碼:
  第一個透過已設定的 OIDC 提供者、以該 email 登入的人會繼承 `SuperAdmin`。

API 監聽 `http://localhost:5083`,Development 環境下可於 `/scalar` 使用 Scalar UI。

### 設定 OIDC 提供者

系統沒有本地密碼登入 — 至少要在 `Oidc:Providers` 設定一個 OIDC 提供者(可放在
`appsettings.json`、user-secrets,或環境變數 / 根目錄的 `.env`,參考 `.env.example`)。以 Google 為例:

```
Oidc__FrontendBaseUrl=http://localhost:3000
Oidc__Providers__0__Name=google
Oidc__Providers__0__DisplayName=Google
Oidc__Providers__0__Authority=https://accounts.google.com
Oidc__Providers__0__ClientId=<client-id>
Oidc__Providers__0__ClientSecret=<client-secret>
```

並在該提供者的主控台中,把 `{ApiBase}/api/auth/callback/{Name}`(例如
`http://localhost:5083/api/auth/callback/google`)加入授權的重新導向 URI。每個提供者都會在登入頁產生
一個「使用 … 登入」按鈕。`Scope` 預設為 `openid profile email`。

### 切換資料庫

在 `appsettings.json` 設定 `Database:Provider`(或環境變數 `Database__Provider`)為 `Sqlite`、
`Postgres` 或 `MariaDb`,並同步修改 `ConnectionStrings:Default`。在本機執行 Postgres 或 MariaDB:

```
docker compose up -d postgres
docker compose up -d mariadb
```

接著設定:

```json
"Database": { "Provider": "Postgres" },
"ConnectionStrings": { "Default": "Host=localhost;Port=5432;Database=warden;Username=postgres;Password=postgres" }
```

```json
"Database": { "Provider": "MariaDb" },
"ConnectionStrings": { "Default": "Server=localhost;Port=3306;Database=warden;User=root;Password=root;" }
```

### Migrations

因為 migration 是各資料庫專屬的 SQL,每個資料庫都有自己的 migrations 資料夾
(`Persistence/Migrations/Sqlite`、`Persistence/Migrations/Postgres`、`Persistence/Migrations/MariaDb`)。
修改實體後新增 migration 時,每個資料庫都要執行一次:

```
dotnet ef migrations add YourMigrationName --context SqliteAppDbContext -o Persistence/Migrations/Sqlite --project src/Warden.Infrastructure
dotnet ef migrations add YourMigrationName --context PostgresAppDbContext -o Persistence/Migrations/Postgres --project src/Warden.Infrastructure
dotnet ef migrations add YourMigrationName --context MariaDbAppDbContext -o Persistence/Migrations/MariaDb --project src/Warden.Infrastructure
```

要新增其他資料庫,只需再加一個輕量的 `AppDbContext` 子類別 + design-time factory + migrations
資料夾 — 共用的模型全部定義在抽象基底類別 `AppDbContext` 中。

## 新增權限

1. 在 `Warden.Domain/Permissions/PermissionConstants.cs` 新增常數,例如 `Reports.Export = "report.export"`。
2. 在 Controller action 加上 `[RequirePermission(PermissionConstants.Reports.Export)]`。

就這樣 — 權限目錄(`GET /api/admin/permissions/catalog`)會透過 reflection 自動發現它,因此它會
自動出現在後台角色頁面的權限矩陣中(也會成為可選的 API Key 範圍),不需要其他程式修改。把權限授予
角色後,已登入的使用者會立即生效(不需重新登入),因為權限查詢只快取 60 秒,且編輯角色權限時會清除快取。

## 啟動前端

```
cd frontend
pnpm install
pnpm run dev
```

執行於 `http://localhost:3000`。若後端不在 `http://localhost:5083`,請設定 `NUXT_PUBLIC_API_BASE`
(參考 `.env.example`)。

## 驗證機制

登入流程經由 OIDC 提供者:後端處理提供者的 callback,建立(或依 email 連結)使用者,再以一次性的
handoff code 重新導向到前端的 `/auth/callback`。前端用這個 code 呼叫 `POST /api/auth/oidc/exchange`
換取短效的 JWT access token 與不透明的 refresh token。Access token 只存在記憶體中,refresh token
則存在 `SameSite=Lax` 的 cookie(使用 Lax 是為了讓從提供者跨站導回的 redirect 流程中仍會送出
cookie,SSR 才能看到登入狀態)。任何 API 呼叫回傳 401 時,會先靜默 refresh 並重試一次,失敗才導回登入頁。

## API Key

使用者可以在後台的 **API Keys** 頁面(`/admin/api-keys`)建立個人 API Key(個人存取權杖),供腳本或
系統整合使用。每個使用者只能管理自己的 key — 這是自助式功能,不是讓管理員檢視其他人 key 的介面。

- **格式**:`wdn_<12 字元 key id>_<secret>`。原始 key **只在建立時顯示一次**,資料庫只儲存
  secret 的 SHA-256 雜湊,之後介面上只會顯示最後 4 個字元。
- **使用方式**:與 JWT access token 完全相同:

  ```
  curl -H "Authorization: Bearer wdn_0123456789ab_..." http://localhost:5083/api/admin/users
  ```

  `Bearer` 是一個 policy scheme:token 以 `wdn_` 開頭時轉給 API Key handler,否則轉給 JWT
  handler,因此 `[Authorize]` / `[RequirePermission]` 對兩者都不需修改。
- **權限範圍(Scopes)**:每個 key 有一個以上的權限範圍 — 可以是精確的權限如 `user.read`、模組萬用字元如
  `user.*`,或 `*`。Key 只能**縮小**擁有者的權限,不能擴大:建立時不能授予自己沒有的範圍,而且每次請求都會
  以擁有者*當下*的角色權限與 key 的範圍取交集。因此從擁有者的角色移除某個權限,其所有 key 也會一併失去該權限。
- **生命週期**:可選擇到期時間(介面上提供 永不 / 30 天 / 90 天 / 1 年),並可隨時撤銷。已撤銷或過期的
  key,以及擁有者帳號已停用的 key,都會得到 401。`LastUsedAtUtc` 每個 key 最多每分鐘更新一次。
- **功能權限**:此功能本身由 `apikey.read`、`apikey.create`、`apikey.revoke` 控管(`apikey.*` 涵蓋三者)。

| 方法 | 路徑 | 所需權限 | 說明 |
|---|---|---|---|
| `GET` | `/api/admin/api-keys` | `apikey.read` | 列出自己的 key |
| `POST` | `/api/admin/api-keys` | `apikey.create` | 建立 key — body 為 `{ "name", "expiresAtUtc", "scopes": [...] }`;回應中包含原始 key(僅此一次) |
| `DELETE` | `/api/admin/api-keys/{id}` | `apikey.revoke` | 撤銷自己的某個 key |

`wdn_` 前綴**不會**被 `dotnet new warden` 改名;若要使用專案專屬的前綴,請修改
`Warden.Application/Services/ApiKeys/ApiKeyFormat.cs` 中的 `ApiKeyFormat.Prefix`。
