# Patrol Data 格式說明

## 📋 概述

這個文件說明新版 `patroldata.txt` 的格式和使用方式。新格式支援敵人類型、裝備物品和巡邏路徑的完整定義。

## 📝 檔案格式

### 基本結構

每個敵人使用 3 行定義：

```
{編號} {類別}
{物品列表}
{巡邏點列表}
```

### 格式說明

#### 第 1 行：敵人編號和類別
- **格式**: `{EnemyIndex} {EnemyType}`
- **EnemyIndex**: 敵人編號（整數）
- **EnemyType**: 敵人類別（字串）
  - `Enemy`: 普通敵人
  - `Guard1`, `Guard2`, `Guard3`: 不同等級的守衛
  - `Target`: 任務目標
  - 可自訂其他類別

#### 第 2 行：物品列表
- **格式**: `{Item1};{Item2};{Item3}` 或 `None`
- 多個物品用分號 `;` 分隔
- 使用 `None` 表示無物品
- 物品名稱需在 EnemyManager 的 Inspector 中設定映射

#### 第 3 行：巡邏點列表
- **格式**: `x1,y1|x2,y2|x3,y3`
- 每個巡邏點使用 `x,y` 格式（只需 2D 座標）
- 多個巡邏點用 `|` 分隔
- 至少需要 1 個巡邏點

### 完整範例

```txt
# Patrol Data File
# Format: 
# Line 1: {EnemyIndex} {EnemyType}
# Line 2: {Items} (separated by semicolon if multiple, use "None" for no items)
# Line 3: {Patrol points} (x,y format, separated by | if multiple)

# 低級守衛（單點巡邏，帶小刀）
0 Guard1
Knife
20,50

# 中級守衛（多點巡邏，帶槍和鑰匙）
1 Guard2
Gun;BlueKey
22,41|35,41|35,14|22,14

# 任務目標（無武器）
2 Target
None
23,42|36,42

# 高級守衛（多個物品）
3 Guard3
Gun;RedKey;HealthPack
40,47|46,53
```

## 🔧 Unity Inspector 設定

### EnemyManager 組件設定

在 Unity 的 EnemyManager 組件中，您需要設定：

1. **Patrol Data File**
   - 將 `patroldata.txt` 拖曳到此欄位

2. **Item Mappings** (物品名稱映射)
   - 點擊 "+" 添加新的映射
   - 每個映射包含：
     - **Item Name**: 物品名稱（必須與 patroldata.txt 中的名稱完全一致）
     - **Item Prefab**: 物品的 Prefab 參考

### 設定範例

```
Item Mappings:
├─ [0] Knife       → Knife_Prefab
├─ [1] Gun         → Gun_Prefab
├─ [2] RedKey      → RedKey_Prefab
├─ [3] BlueKey     → BlueKey_Prefab
└─ [4] HealthPack  → HealthPack_Prefab
```

## 📦 物品系統整合

### 支援的物品類型

- **武器**: Knife, Gun, Rifle 等
- **鑰匙**: RedKey, BlueKey, GreenKey 等
- **其他物品**: 任何繼承自 `Item` 的物品

### 物品自動裝備

系統會在生成敵人時：
1. 清空敵人的 ItemHolder
2. 根據 patroldata.txt 中定義的物品名稱
3. 查找 Inspector 中設定的映射
4. 自動將對應的 Prefab 裝備到敵人身上

## 🎯 敵人類型應用

### 不同類型的用途

- **Enemy**: 基礎敵人，用於一般場景
- **Guard1/2/3**: 不同等級的守衛
  - Guard1: 低級守衛（可能較弱、速度慢）
  - Guard2: 中級守衛
  - Guard3: 高級守衛（可能較強、速度快）
- **Target**: 玩家的任務目標
  - 通常不配備武器
  - 可能有特殊的 AI 行為

### 未來擴展

您可以在代碼中根據 `enemyType` 實現不同的行為：
- 不同的移動速度
- 不同的血量
- 不同的視野範圍
- 特殊的 AI 邏輯

## ⚠️ 注意事項

1. **物品名稱大小寫**
   - 物品名稱區分大小寫
   - `Knife` 和 `knife` 是不同的物品
   - 建議統一使用首字母大寫的駝峰命名法

2. **空行和註釋**
   - 使用 `#` 開頭的行為註釋
   - 空行會被自動跳過
   - 建議在每個敵人定義前加上註釋

3. **巡邏點順序**
   - 敵人會按照定義的順序巡邏
   - 最後一個點會自動連回第一個點形成循環

4. **錯誤處理**
   - 如果找不到物品映射，會在 Console 顯示警告
   - 敵人仍會生成，但不會裝備該物品

## 🐛 除錯技巧

### 查看載入資訊

在 Unity Console 中可以看到：
- 每個敵人的載入資訊
- 物品映射的設定
- 巡邏點的數量

### 常見問題

1. **敵人沒有裝備物品**
   - 檢查 Item Mappings 是否正確設定
   - 檢查物品名稱是否完全一致（包括大小寫）

2. **敵人沒有生成**
   - 檢查 patroldata.txt 格式是否正確
   - 檢查 Console 是否有錯誤訊息

3. **巡邏點不正確**
   - 檢查座標格式是否為 `x,y`
   - 檢查是否使用 `|` 分隔多個點

## 📚 相關檔案

- `patroldata.txt`: 敵人資料檔案
- `EnemyManager.cs`: 管理敵人生成的腳本
- `ItemHolder.cs`: 管理物品裝備的腳本
- `Enemy.cs`: 敵人主控制腳本

## 🔄 從舊格式遷移

如果您有舊格式的 patroldata.txt，新系統仍保持向後相容：
- 舊的 `enemyPatrolData` 列表仍然存在
- 可以繼續使用 `GetEnemyPatrolPoints()` 方法
- 建議逐步遷移到新格式以使用完整功能




