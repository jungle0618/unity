# 共用組件架構建議

## 📊 當前狀況分析

### Enemy（敵人）
✅ **已模組化**
- `EnemyDetection` - 偵測系統
- `EnemyMovement` - 移動系統  
- `EnemyStateMachine` + `EnemyState` - 狀態系統
- `EnemyVisualizer` - 視覺化系統

### Target（目標）
✅ **使用 Enemy 組件，無需獨立組件**
- Target 直接使用 `Enemy`、`EnemyMovement`、`EnemyDetection`、`EnemyVisualizer`
- Target 與 Enemy 邏輯完全相同，無需重複實作
- 透過參數配置區分 Target 和普通 Enemy

### Player（玩家）
❌ **功能集中在單一類別**
- `PlayerController` 包含：移動、血量、武器、輸入處理等
- 沒有分離 Detection、Movement、State、Visualizer

---

## ✅ 建議：採用共用架構

### 優點

1. **代碼重用性高**
   - 減少重複代碼
   - 統一介面，更容易擴展新功能

2. **維護性更好**
   - 修改一個地方可以影響所有實體
   - 更容易找到和修復 Bug

3. **架構一致性**
   - Target 已經在使用 Enemy 組件
   - Player 採用相同架構可保持一致性

4. **更好的測試性**
   - 組件可以獨立測試
   - 更容易進行單元測試

### 缺點與注意事項

1. **初始開發成本**
   - 需要重構 Player 現有代碼
   - 需要設計良好的抽象層

2. **過度抽象的風險**
   - 不同實體可能有特殊需求
   - 需要在抽象和靈活性之間平衡

3. **學習曲線**
   - 新架構需要團隊理解
   - 需要良好的文檔說明

---

## 🏗️ 建議的架構設計

### 方案：繼承 + 組合模式

```
BaseEntity (抽象基類)
├── BaseDetection (抽象基類)
│   ├── EnemyDetection
│   └── PlayerDetection
│   (Target 直接使用 EnemyDetection)
│
├── BaseMovement (抽象基類)
│   ├── EnemyMovement
│   └── PlayerMovement
│   (Target 直接使用 EnemyMovement)
│
├── BaseStateMachine<TState> (泛型狀態機)
│   ├── EnemyStateMachine : BaseStateMachine<EnemyState>
│   └── PlayerStateMachine : BaseStateMachine<PlayerState>
│   (Target 直接使用 EnemyStateMachine)
│
└── BaseVisualizer (抽象基類)
    ├── EnemyVisualizer
    └── PlayerVisualizer
    (Target 直接使用 EnemyVisualizer)
```

### 核心抽象類別設計

#### 1. BaseDetection
```csharp
public abstract class BaseDetection : MonoBehaviour
{
    protected Transform target;
    
    public abstract bool CanSeeTarget(Vector2 targetPos);
    public abstract float GetDistanceToTarget();
    public abstract Vector2 GetDirectionToTarget();
    
    public virtual void SetTarget(Transform newTarget) => target = newTarget;
    public virtual Transform GetTarget() => target;
}
```

#### 2. BaseMovement
```csharp
public abstract class BaseMovement : MonoBehaviour
{
    protected Rigidbody2D rb;
    
    public abstract void MoveTowards(Vector2 target, float speedMultiplier);
    public abstract void StopMovement();
    public abstract Vector2 GetMovementDirection();
    
    public Vector2 Position => transform.position;
}
```

#### 3. BaseStateMachine<TState>
```csharp
public abstract class BaseStateMachine<TState> where TState : System.Enum
{
    public TState CurrentState { get; protected set; }
    public System.Action<TState, TState> OnStateChanged;
    
    public abstract void ChangeState(TState newState);
    public abstract void UpdateState(float deltaTime);
}
```

#### 4. BaseVisualizer
```csharp
public abstract class BaseVisualizer : MonoBehaviour
{
    public abstract void SetShowDebugGizmos(bool show);
    protected abstract void OnDrawGizmos();
    protected abstract void OnDrawGizmosSelected();
}
```

---

## 🎯 實作優先順序

### 階段 1：建立基礎架構（低風險）
1. ✅ 創建基礎抽象類別
2. ✅ 讓 Enemy 組件繼承基礎類別（保持向後兼容）
3. ✅ 測試確保 Enemy 功能正常

### 階段 2：重構 Player（中風險）
1. ⚠️ 創建 `PlayerDetection`、`PlayerMovement`、`PlayerStateMachine`、`PlayerVisualizer`
2. ⚠️ 重構 `PlayerController` 使用新組件
3. ⚠️ 測試確保 Player 功能正常

### 階段 3：優化與擴展（低風險）
1. ✅ 統一介面，添加共用功能
2. ✅ 優化性能
3. ✅ 添加新功能時使用統一架構

---

## 💡 具體建議

### ✅ 建議實作

1. **Detection 系統**
   - Player 和 Enemy 都使用 `BaseDetection`
   - Player 可能需要不同的視野邏輯（例如：360度視野）

2. **Movement 系統**
   - Player 和 Enemy 都使用 `BaseMovement`
   - Player 使用輸入控制，Enemy 使用 AI 控制

3. **State 系統**
   - Player 可以使用簡化的狀態機（Idle, Moving, Attacking, Dead）
   - Enemy 保持現有的複雜狀態機

4. **Visualizer 系統**
   - 統一調試 Gizmos 繪製
   - 每個實體可以自訂顯示內容

### ⚠️ 需要注意的差異

| 功能 | Enemy | Player | Target |
|------|-------|--------|--------|
| **移動控制** | AI 自動 | 輸入控制 | AI 自動（逃亡） |
| **狀態機** | 複雜（6種狀態） | 簡單（4種狀態） | 可能不需要 |
| **偵測** | 主動偵測玩家 | 被動（玩家看到敵人） | 不需要 |
| **視覺化** | 視野範圍、狀態顏色 | 血量顏色、移動方向 | 可能不需要 |

---

## 🚀 實施建議

### 建議採用此架構，但分階段實施：

1. **先建立基礎抽象類別**（不影響現有代碼）
2. **讓 Enemy 組件繼承**（保持向後兼容）
3. **逐步重構 Player**（測試確保功能正常）
4. **統一 Target**（Target 已經在使用 Enemy 組件，可能需要微調）

### 如果時間有限：

可以先只統一 **Detection** 和 **Visualizer**，因為：
- Detection 邏輯相似度高
- Visualizer 主要用於調試
- Movement 和 State 差異較大，可以稍後統一

---

## ❓ 需要決定的問題

1. **Player 是否需要狀態機？**
   - 目前 Player 沒有明確的狀態管理
   - 如果添加狀態機，可以統一介面

2. **Target 的處理方式**
   - ✅ Target 直接使用 Enemy 組件，無需獨立實作
   - ✅ Target 與 Enemy 邏輯相同，透過配置參數區分
   - ✅ 減少重複代碼，保持架構簡潔

3. **是否有其他實體類型？**
   - 未來可能會有更多實體類型
   - 統一架構可以輕鬆擴展

---

## 📝 結論

**建議採用共用架構**，理由：
- ✅ 代碼重用性高
- ✅ 維護性更好
- ✅ Target 已經在使用 Enemy 組件
- ✅ 未來擴展更容易

**實施策略**：
- 分階段實施，降低風險
- 保持向後兼容
- 充分測試每個階段

