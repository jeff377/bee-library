# 專案相依性全景圖

本文件以視覺化方式呈現 Bee.NET 框架中 11 個 `src/` 專案之間的相依關係。

**閱讀方式**：箭頭方向 A → B 表示「A 依賴 B」；圖表由下而上排列，最底層為無相依性的基礎套件。

## 相依性圖表

```mermaid
graph BT
  subgraph 基礎設施層
    Base["Bee.Base<br/><small>net10.0</small>"]
    Definition["Bee.Definition<br/><small>net10.0</small>"]
    Caching["Bee.ObjectCaching<br/><small>net10.0</small>"]
  end

  subgraph 資料存取層
    RepoAbs["Bee.Repository.Abstractions<br/><small>net10.0</small>"]
    Db["Bee.Db<br/><small>net10.0</small>"]
    Repo["Bee.Repository<br/><small>net10.0</small>"]
  end

  subgraph 商業邏輯層
    Business["Bee.Business<br/><small>net10.0</small>"]
  end

  subgraph API 層
    Contracts["Bee.Api.Contracts<br/><small>net10.0</small>"]
    Core["Bee.Api.Core<br/><small>net10.0</small>"]
    AspNet["Bee.Api.AspNetCore<br/><small>net10.0</small>"]
  end

  subgraph 用戶端
    Client["Bee.Api.Client<br/><small>net10.0</small>"]
  end

  Definition --> Base
  Contracts --> Definition
  Db --> Definition
  RepoAbs --> Definition
  Caching --> Definition
  Business --> Contracts
  Business --> Definition
  Business --> RepoAbs
  Repo --> Db
  Repo --> RepoAbs
  Core --> Contracts
  Core --> Definition
  AspNet --> Core
  Client --> Core
```

## 外部相依套件

| 專案 | 外部套件 |
|------|----------|
| Bee.Base | Newtonsoft.Json |
| Bee.Definition | MessagePack |
| Bee.Db | *(none)* |
| Bee.ObjectCaching | System.Runtime.Caching |
| Bee.Api.AspNetCore | Microsoft.AspNetCore.Mvc.Core |

## 目標框架摘要

所有專案皆以 `net10.0` 單一目標發布。

## 架構要點

- **Bee.Base** 為最底層基礎套件，無任何內部相依性。
- **Bee.Definition** 為被依賴次數最多的專案，共有 6 個直接相依者（Contracts、Db、RepoAbs、Caching、Business、Core）。
- **Bee.Api.AspNetCore** 為 API 託管套件，適用於伺服器端部署。
- 用戶端（Bee.Api.Client）與伺服器端（Bee.Api.AspNetCore）皆透過 **Bee.Api.Core** 共享協定邏輯，確保序列化與加解密行為一致。
