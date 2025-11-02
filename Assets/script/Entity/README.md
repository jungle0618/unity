# Entity 基礎架構說明

## 📋 概述

此目錄包含所有實體（Entity）的基礎抽象類別，用於統一 Enemy、Player、Target 等人物物件的架構。

**注意**：Target 直接使用 Enemy 組件，無需獨立實作。

## 🏗️ 架構設計

### 核心組件

```
BaseEntity<TState>
├── BaseStateMachine<TState> (狀態機)
├── BaseMovement (移動)
├── BaseDetection (偵測)
├── BaseVisualizer (視覺化)
└── WeaponHolder (武器管理，已實作)
```

### 類別說明

#### 1. BaseEntity<TState>
- **位置**: `BaseEntity.cs`
- **說明**: 核心抽象實體類別，整合所有組件
- **泛型參數**: `TState` - 狀態枚舉類型
- **功能**:
  - 統一管理所有組件引用
  - 提供統一的實體生命週期
  - 提供共用的公共介面

#### 2. BaseStateMachine<TState>
- **位置**: `BaseStateMachine.cs`
- **說明**: 泛型狀態機基類，支援任意枚舉類型的狀態
- **功能**:
  - 狀態轉換管理
  - 狀態變更事件
  - 狀態更新邏輯

#### 3. BaseMovement
- **位置**: `BaseMovement.cs`
- **說明**: 移動組件基類
- **功能**:
  - 統一的移動介面
  - 基礎移動方法
  - 速度管理

#### 4. BaseDetection
- **位置**: `BaseDetection.cs`
- **說明**: 偵測組件基類
- **功能**:
  - 目標管理
  - 統一的偵測介面
  - 距離和方向計算

#### 5. BaseVisualizer
- **位置**: `BaseVisualizer.cs`
- **說明**: 視覺化組件基類
- **功能**:
  - 統一的 Gizmos 繪製介面
  - 調試視覺化管理

## ✅ 已完成的重構

### Enemy 組件更新

1. **EnemyStateMachine**
   - ✅ 繼承 `BaseStateMachine<EnemyState>`
   - ✅ 保留所有原有功能
   - ✅ 完全向後兼容

2. **EnemyMovement**
   - ✅ 繼承 `BaseMovement`
   - ✅ 覆寫抽象方法
   - ✅ 保留所有原有功能

3. **EnemyDetection**
   - ✅ 繼承 `BaseDetection`
   - ✅ 覆寫抽象方法
   - ✅ 保留所有原有功能

4. **EnemyVisualizer**
   - ✅ 繼承 `BaseVisualizer`
   - ✅ 覆寫抽象方法
   - ✅ 保留所有原有功能

## 🎯 使用方式

### 創建新的實體類型

例如：創建 Player 實體

```csharp
// 1. 定義 Player 狀態枚舉
public enum PlayerState
{
    Idle,
    Moving,
    Attacking,
    Dead
}

// 2. 創建 Player 組件（繼承基類）
public class PlayerMovement : BaseMovement { }
public class PlayerDetection : BaseDetection { }
public class PlayerStateMachine : BaseStateMachine<PlayerState> { }
public class PlayerVisualizer : BaseVisualizer { }

// 3. 創建 Player 主類別（可選，繼承 BaseEntity）
public class Player : BaseEntity<PlayerState>
{
    protected override void InitializeEntity()
    {
        // 初始化邏輯
    }
}
```

## 📝 注意事項

1. **向後兼容性**: 所有現有的 Enemy 組件都保持完全向後兼容
2. **組件引用**: 基類使用 `protected` 欄位，子類別可以直接訪問
3. **抽象方法**: 必須在子類別中實現所有抽象方法
4. **虛擬方法**: 可以選擇性覆寫虛擬方法來添加特定功能

## 🎯 Target 處理方式

**Target 直接使用 Enemy 組件**
- Target 與 Enemy 邏輯完全相同
- Target.prefab 使用 `Enemy`、`EnemyMovement`、`EnemyDetection`、`EnemyVisualizer`
- 無需建立獨立的 Target 組件
- 透過配置參數（如名稱、外觀）區分 Target 和普通 Enemy

## 🔄 未來擴展

- [ ] Player 組件重構
- [x] Target 組件統一（使用 Enemy 組件，已完成）
- [ ] 添加更多共用功能到基類
- [ ] 性能優化

## 📚 相關文件

- `SharedComponentsProposal.md` - 架構提案文檔
- `Enemy/` - Enemy 組件實作
- `Player/` - Player 組件（待重構）

