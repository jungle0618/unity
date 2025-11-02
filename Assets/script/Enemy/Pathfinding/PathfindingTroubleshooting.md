# 路徑規劃系統故障排除指南

## 🚨 常見問題

### 問題1：敵人穿牆或走過物件
**原因**：網格設定不正確或tilemap未正確設定

**解決方案**：
1. 檢查PathfindingGrid組件設定
2. 確保Wall Tilemap和Object Tilemap都已正確設定
3. 檢查Cell Size是否與Tilemap一致
4. 使用"檢查網格設定"功能驗證設定

### 問題2：敵人找不到路徑
**原因**：目標位置被障礙物包圍或網格太小

**解決方案**：
1. 檢查目標位置是否可行走
2. 增加網格大小覆蓋整個遊戲區域
3. 檢查是否有路徑連接起點和終點

### 問題3：路徑規劃性能差
**原因**：網格太大或更新太頻繁

**解決方案**：
1. 適當調整網格大小
2. 增加路徑更新間隔
3. 使用路徑平滑減少節點數量

## 🔧 設定檢查清單

### ✅ PathfindingGrid 設定
- [ ] Cell Size = 1.0（與Tilemap一致）
- [ ] Grid Width/Height 覆蓋整個遊戲區域
- [ ] Grid Offset = (0, 0)
- [ ] Wall Tilemap 已設定
- [ ] Object Tilemap 已設定
- [ ] Obstacle Layer Mask 正確設定

### ✅ Tilemap 設定
- [ ] 牆壁Tilemap有Tilemap Collider 2D
- [ ] 物件Tilemap有Tilemap Collider 2D
- [ ] 兩個Tilemap的Layer設定正確
- [ ] Tilemap的Cell Size與網格一致

### ✅ 敵人設定
- [ ] EnemyMovement的Use Pathfinding = true
- [ ] Pathfinding引用正確設定
- [ ] 敵人不在牆壁或物件內部

## 🛠️ 除錯工具

### PathfindingDebugger
- 在Scene視圖中顯示網格
- 測試特定位置是否可行走
- 檢查障礙物檢測

### PathfindingGrid 除錯功能
- 右鍵點擊PathfindingGrid組件
- 選擇"檢查網格設定"
- 選擇"重新創建網格"

## 📋 測試步驟

1. **基本測試**：
   - 在空曠區域放置敵人
   - 設定目標位置
   - 檢查敵人是否能找到路徑

2. **障礙物測試**：
   - 在敵人和目標之間放置牆壁
   - 檢查敵人是否繞過牆壁
   - 檢查敵人是否穿牆

3. **物件測試**：
   - 在路徑上放置桌子等物件
   - 檢查敵人是否繞過物件

## ⚠️ 注意事項

- **Cell Size必須與Tilemap一致**：通常是1.0
- **網格必須覆蓋整個遊戲區域**：包括所有可能的路徑
- **牆壁和物件都是障礙物**：敵人不能走過任何tilemap
- **定期更新網格**：當地形改變時調用UpdateGrid()
