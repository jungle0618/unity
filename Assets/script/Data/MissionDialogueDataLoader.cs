using UnityEngine;
using System;

/// <summary>
/// 任務對話數據載入器
/// 從 missiondialogues.json 載入對話文字
/// </summary>
public class MissionDialogueDataLoader
{
    // JSON 序列化用的結構
    [System.Serializable]
    private class DialogueSetJson
    {
        public string[] dialogues;
    }

    [System.Serializable]
    private class MissionDialoguesJson
    {
        public DialogueSetJson missionStart;
        public DialogueSetJson missionWin;
        public DialogueSetJson missionFail;
    }

    /// <summary>
    /// 對話數據結構
    /// </summary>
    [System.Serializable]
    public class MissionDialogueData
    {
        public string[] missionStartDialogues;
        public string[] missionWinDialogues;
        public string[] missionFailDialogues;
    }

    private MissionDialogueData dialogueData;
    private bool showDebugInfo = false;

    public MissionDialogueData DialogueData => dialogueData;

    public MissionDialogueDataLoader(bool showDebugInfo = false)
    {
        this.showDebugInfo = showDebugInfo;
        dialogueData = new MissionDialogueData();
    }

    /// <summary>
    /// 從 TextAsset 載入對話數據
    /// </summary>
    public bool LoadDialogueData(TextAsset dialogueDataFile)
    {
        if (dialogueDataFile == null)
        {
            Debug.LogError("MissionDialogueDataLoader: Dialogue data file (TextAsset) is not assigned!");
            CreateDefaultDialogueData();
            return false;
        }

        return LoadJsonFormat(dialogueDataFile);
    }

    /// <summary>
    /// 從 JSON 格式載入數據
    /// </summary>
    private bool LoadJsonFormat(TextAsset dialogueDataFile)
    {
        try
        {
            MissionDialoguesJson jsonData = JsonUtility.FromJson<MissionDialoguesJson>(dialogueDataFile.text);

            if (jsonData == null)
            {
                Debug.LogError("MissionDialogueDataLoader: Failed to parse JSON data!");
                CreateDefaultDialogueData();
                return false;
            }

            // 載入任務開始對話
            if (jsonData.missionStart != null && jsonData.missionStart.dialogues != null)
            {
                dialogueData.missionStartDialogues = jsonData.missionStart.dialogues;
            }
            else
            {
                dialogueData.missionStartDialogues = new string[0];
            }

            // 載入任務勝利對話
            if (jsonData.missionWin != null && jsonData.missionWin.dialogues != null)
            {
                dialogueData.missionWinDialogues = jsonData.missionWin.dialogues;
            }
            else
            {
                dialogueData.missionWinDialogues = new string[0];
            }

            // 載入任務失敗對話
            if (jsonData.missionFail != null && jsonData.missionFail.dialogues != null)
            {
                dialogueData.missionFailDialogues = jsonData.missionFail.dialogues;
            }
            else
            {
                dialogueData.missionFailDialogues = new string[0];
            }

            if (showDebugInfo)
            {
                Debug.Log($"MissionDialogueDataLoader: Successfully loaded dialogue data - " +
                    $"Start: {dialogueData.missionStartDialogues.Length}, " +
                    $"Win: {dialogueData.missionWinDialogues.Length}, " +
                    $"Fail: {dialogueData.missionFailDialogues.Length}");
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"MissionDialogueDataLoader: Error loading JSON format dialogue data: {e.Message}");
            CreateDefaultDialogueData();
            return false;
        }
    }

    /// <summary>
    /// 創建默認對話數據（用於錯誤處理）
    /// </summary>
    private void CreateDefaultDialogueData()
    {
        dialogueData.missionStartDialogues = new string[]
        {
            "Good evening, agent 67.",
            "Your mission begins now."
        };

        dialogueData.missionWinDialogues = new string[]
        {
            "Mission accomplished",
            "Well done, agent 67."
        };

        dialogueData.missionFailDialogues = new string[]
        {
            "Mission failed",
            "Try again, agent 67."
        };

        if (showDebugInfo)
        {
            Debug.LogWarning("MissionDialogueDataLoader: Created default dialogue data");
        }
    }
}

