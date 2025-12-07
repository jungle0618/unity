# MissionDialogueManager 使用指南

## 概述

`MissionDialogueManager` 負責在遊戲的三個關鍵時刻顯示任務對話：
- **遊戲開始時**：顯示任務簡報
- **遊戲勝利時**：顯示任務完成對話
- **遊戲失敗時**：顯示任務失敗對話

## 文件結構

### 1. JSON 數據文件
- **位置**：`Data/missiondialogues.json`
- **格式**：包含三組對話序列（missionStart, missionWin, missionFail）

### 2. 核心腳本
- **MissionDialogueManager.cs**：主管理器，處理對話顯示邏輯
- **MissionDialogueDataLoader.cs**：數據載入器，從 JSON 讀取對話內容

## 設置步驟

### 1. 在 Unity 中設置

1. **創建 MissionDialogueManager GameObject**
   - 在遊戲場景中創建一個空的 GameObject
   - 命名為 "MissionDialogueManager"
   - 添加 `MissionDialogueManager` 組件

2. **分配對話數據文件**
   - 在 Inspector 中，將 `missiondialogues.json` 拖入 `Dialogue Data File` 欄位
   - 或者讓系統自動查找 `DialogueUIManager`（會自動從 `GameUIManager` 獲取）

3. **配置設置**
   - `Show Mission Start Dialogue`：是否顯示開始對話（默認：true）
   - `Show Mission Win Dialogue`：是否顯示勝利對話（默認：true）
   - `Show Mission Fail Dialogue`：是否顯示失敗對話（默認：true）
   - `Delay Before Showing UI`：對話完成後顯示 UI 的延遲時間（秒）

### 2. 對話系統設置

確保 `DialogueUIManager` 已正確設置：
- 對話面板已分配
- 對話文字組件已分配
- 繼續按鈕已分配（可選，因為支持空格鍵）

### 3. 空格鍵支持

`DialogueUIManager` 已支持空格鍵繼續：
- `Enable Space Key To Continue`：是否啟用空格鍵（默認：true）
- `Continue Key`：繼續按鍵（默認：Space）

## 對話流程

### 遊戲開始時
1. 遊戲狀態變為 `Playing`
2. `MissionDialogueManager` 檢測到狀態變化
3. 顯示任務開始對話序列
4. 玩家按空格鍵（或點擊按鈕）繼續
5. 所有對話完成後，遊戲正常開始

### 遊戲勝利時
1. 遊戲狀態變為 `GameWin`
2. 先隱藏 `GameWinUI`（如果已顯示）
3. 顯示任務勝利對話序列
4. 玩家按空格鍵繼續
5. 所有對話完成後，顯示 `GameWinUI`

### 遊戲失敗時
1. 遊戲狀態變為 `GameOver`
2. 先隱藏 `GameOverUI`（如果已顯示）
3. 顯示任務失敗對話序列
4. 玩家按空格鍵繼續
5. 所有對話完成後，顯示 `GameOverUI`（包含重試選單）

## 自定義對話內容

編輯 `Data/missiondialogues.json` 來修改對話內容：

```json
{
  "missionStart": {
    "dialogues": [
      "第一條對話",
      "第二條對話",
      "..."
    ]
  },
  "missionWin": {
    "dialogues": [
      "勝利對話1",
      "勝利對話2"
    ]
  },
  "missionFail": {
    "dialogues": [
      "失敗對話1",
      "失敗對話2"
    ]
  }
}
```

## 注意事項

1. **時間控制**：對話顯示時，遊戲會自動暫停（`Time.timeScale = 0`），對話完成後恢復
2. **狀態管理**：`MissionDialogueManager` 會自動監聽 `GameManager` 的狀態變化
3. **錯誤處理**：如果 JSON 文件載入失敗，會使用默認對話內容
4. **重置功能**：重新開始遊戲時，`hasShownStartDialogue` 會自動重置

## 調試

啟用 `Show Debug Info` 來查看詳細的日誌信息：
- 對話數據載入狀態
- 對話顯示/完成事件
- 錯誤和警告信息

## API 參考

### MissionDialogueManager

```csharp
// 重置狀態（用於重新開始遊戲）
public void Reset()

// 檢查是否正在顯示對話
public bool IsDialogueActive()
```

### 事件流程

- `GameManager.OnGameStateChanged` → `MissionDialogueManager.OnGameStateChanged`
- 對話完成回調 → 顯示相應的 UI（GameWinUI/GameOverUI）

