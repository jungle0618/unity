# Patrol Data 格式說明

## 📋 概述

這個文件說明 `patroldata.json` 的格式和使用方式。系統使用 JSON 格式載入實體數據。

## 📝 JSON 格式

### 基本結構

```json
{
  "entities": [
    {
      "entityIndex": 0,
      "type": "Enemy",
      "items": ["Knife"],
      "patrolPoints": [{"x": 20, "y": 50, "z": 0}],
      "rotation": 180
    }
  ]
}
```

### 字段說明

#### 必需字段

- **`entityIndex`** (int): 實體編號
- **`type`** (string): 實體類型
  - `"Enemy"`: 普通敵人
  - `"Target"`: 任務目標
  - `"Player"`: 玩家（僅用於設置初始位置）
  - `"Exit"`: 出口點（勝利條件）
- **`items`** (string[]): 物品列表，空數組 `[]` 表示無物品
- **`patrolPoints`** (Vector3Data[]): 巡邏點列表，每個點為 `{"x": float, "y": float, "z": float}`
- **`rotation`** (float): 初始朝向（度數，0-360），預設值：`0`

#### 可選字段

- **`escapePoint`** (Vector3Data): 逃亡點，僅用於 `Target` 類型，格式為 `{"x": float, "y": float, "z": float}`

### 完整範例

```json
{
  "entities": [
    {
      "entityIndex": 0,
      "type": "Enemy",
      "items": ["Knife"],
      "patrolPoints": [{"x": 20, "y": 50, "z": 0}],
      "rotation": 180
    },
    {
      "entityIndex": 1,
      "type": "Enemy",
      "items": ["Knife", "RedKey"],
      "patrolPoints": [
        {"x": 22, "y": 41, "z": 0},
        {"x": 35, "y": 41, "z": 0},
        {"x": 35, "y": 14, "z": 0},
        {"x": 22, "y": 14, "z": 0}
      ],
      "rotation": 180
    },
    {
      "entityIndex": 0,
      "type": "Player",
      "items": ["Knife", "Gun"],
      "patrolPoints": [{"x": 4, "y": 26, "z": 0}],
      "rotation": 0
    },
    {
      "entityIndex": 0,
      "type": "Target",
      "items": [],
      "patrolPoints": [{"x": 80, "y": 26, "z": 0}],
      "rotation": 0,
      "escapePoint": {"x": 47, "y": 54, "z": 0}
    }
  ]
}
```

### JSON 格式優點

1. ✅ **結構化**：清晰的層次結構，易於理解
2. ✅ **類型明確**：數字、字串、陣列自動識別
3. ✅ **易於擴展**：新增字段不需要修改解析邏輯
4. ✅ **工具支持**：編輯器自動補全、語法高亮、驗證
5. ✅ **錯誤提示**：JSON 格式錯誤會直接報錯，定位問題更容易

## 🔧 Unity Inspector 設定

### EntityManager 組件設定

在 Unity 的 EntityManager 組件中，您需要設定：

1. **Patrol Data File**
   - 將 `patroldata.json` 拖曳到此欄位

2. **Item Mappings** (物品名稱映射)
   - 點擊 "+" 添加新的映射
   - 每個映射包含：
     - **Item Name**: 物品名稱（必須與數據文件中的名稱完全一致）
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
2. 根據數據文件中定義的物品名稱
3. 查找 Inspector 中設定的映射
4. 自動將對應的 Prefab 裝備到敵人身上

## 🎯 實體類型應用

### 不同類型的用途

- **Enemy**: 基礎敵人，用於一般場景
- **Target**: 玩家的任務目標
  - 通常不配備武器
  - 可能有特殊的 AI 行為
  - 可以設置逃亡點（escapePoint）
- **Player**: 玩家初始位置和裝備
- **Exit**: 出口點（勝利條件）

### 未來擴展

您可以在代碼中根據 `type` 實現不同的行為：
- 不同的移動速度
- 不同的血量
- 不同的視野範圍
- 特殊的 AI 邏輯

## ⚠️ 注意事項

1. **物品名稱大小寫**
   - 物品名稱區分大小寫
   - `Knife` 和 `knife` 是不同的物品
   - 建議統一使用首字母大寫的駝峰命名法

2. **JSON 格式要求**
   - 必須是有效的 JSON 格式
   - 所有字串必須用雙引號 `"` 包裹
   - 數組和對象格式必須正確
   - 可以使用 JSON 驗證工具檢查格式

3. **巡邏點順序**
   - 敵人會按照定義的順序巡邏
   - 最後一個點會自動連回第一個點形成循環

4. **錯誤處理**
   - 如果找不到物品映射，會在 Console 顯示警告
   - 敵人仍會生成，但不會裝備該物品
   - JSON 解析失敗會顯示錯誤訊息並創建默認數據

## 🐛 除錯技巧

### 查看載入資訊

在 Unity Console 中可以看到：
- 每個實體的載入資訊
- 物品映射的設定
- 巡邏點的數量

### 常見問題

1. **實體沒有裝備物品**
   - 檢查 Item Mappings 是否正確設定
   - 檢查物品名稱是否完全一致（包括大小寫）

2. **實體沒有生成**
   - 檢查 JSON 文件格式是否正確
   - 檢查 Console 是否有錯誤訊息
   - 確認文件已正確拖曳到 Inspector

3. **巡邏點不正確**
   - 檢查座標格式是否為 `{"x": float, "y": float, "z": float}`
   - 確認數組格式正確

4. **JSON 解析失敗**
   - 使用 JSON 驗證工具檢查格式
   - 確認所有字串都用雙引號包裹
   - 確認數組和對象格式正確
   - 檢查是否有語法錯誤（多餘的逗號、缺少括號等）

## 📚 相關檔案

- `patroldata.json`: JSON 格式數據文件
- `EntityDataLoader.cs`: 數據載入器
- `EntityManager.cs`: 管理實體生成的腳本
- `ItemHolder.cs`: 管理物品裝備的腳本
- `Enemy.cs`: 敵人主控制腳本
