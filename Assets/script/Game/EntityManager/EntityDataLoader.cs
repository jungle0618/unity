using UnityEngine;
using System.Collections.Generic;

namespace Game.EntityManager
{
    /// <summary>
    /// 實體數據載入器
    /// 職責：從 patroldata.txt 載入實體數據（Enemy, Target, Player）
    /// </summary>
    public class EntityDataLoader
    {
        // 實體類型枚舉
        public enum EntityType
        {
            None,   // 無效類型
            Enemy,  // 敵人
            Target, // 目標
            Player, // 玩家（僅用於設置初始位置）
            Exit    // 出口點（勝利條件）
        }

        // 實體資料結構（擴展以支持不同類型）
        [System.Serializable]
        public class EntityData
        {
            public int entityIndex;
            public string entityType; // 保留以向後兼容
            public EntityType type = EntityType.Enemy; // 實體類型
            public List<string> itemNames = new List<string>();
            public Vector3[] patrolPoints;
            public Vector3 escapePoint = Vector3.zero; // Target 的逃亡點（可選）
        }

        private List<EntityData> entityDataList = new List<EntityData>();
        private bool showDebugInfo = false;

        public List<EntityData> EntityDataList => entityDataList;

        public EntityDataLoader(bool showDebugInfo = false)
        {
            this.showDebugInfo = showDebugInfo;
        }

        /// <summary>
        /// 從TextAsset載入所有實體的patrol points（擴展格式）
        /// 格式：
        /// Line 1: {EntityIndex} {EntityType} [EntityType 可以是: Enemy, Target, Player]
        /// Line 2: {Items} (separated by semicolon, "None" for no items)
        /// Line 3: {Patrol points} (x,y format, separated by | if multiple)
        /// Line 4 (可選): {Escape point} (x,y format, 僅用於 Target 類型)
        /// </summary>
        public bool LoadPatrolData(TextAsset patrolDataFile)
        {
            entityDataList.Clear();

            if (patrolDataFile == null)
            {
                Debug.LogError("EntityDataLoader: Patrol data file (TextAsset) is not assigned! Please assign the patroldata.txt file in the inspector.");
                CreateDefaultPatrolData();
                return false;
            }

            try
            {
                string[] lines = patrolDataFile.text.Split('\n');
                int lineIndex = 0;

                while (lineIndex < lines.Length)
                {
                    string line = lines[lineIndex].Trim();

                    // 跳過註釋和空行
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    {
                        lineIndex++;
                        continue;
                    }

                    // 第1行：讀取 EntityIndex 和 EntityType
                    string[] indexTypeParts = line.Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (indexTypeParts.Length < 2)
                    {
                        Debug.LogWarning($"EntityDataLoader: Invalid entity definition at line {lineIndex}: {line}");
                        lineIndex++;
                        continue;
                    }

                    EntityData entityData = new EntityData();
                    entityData.entityIndex = int.Parse(indexTypeParts[0]);
                    entityData.entityType = indexTypeParts[1];

                    // 根據 EntityType 字符串設置類型
                    if (indexTypeParts[1].Equals("Target", System.StringComparison.OrdinalIgnoreCase))
                    {
                        entityData.type = EntityType.Target;
                    }
                    else if (indexTypeParts[1].Equals("Player", System.StringComparison.OrdinalIgnoreCase))
                    {
                        entityData.type = EntityType.Player;
                    }
                    else if (indexTypeParts[1].Equals("Exit", System.StringComparison.OrdinalIgnoreCase))
                    {
                        entityData.type = EntityType.Exit;
                    }
                    else
                    {
                        entityData.type = EntityType.Enemy; // 默認為 Enemy
                    }

                    lineIndex++;

                    // 第2行：讀取 Items
                    if (lineIndex < lines.Length)
                    {
                        string itemsLine = lines[lineIndex].Trim();
                        if (!string.IsNullOrWhiteSpace(itemsLine) && !itemsLine.StartsWith("#"))
                        {
                            if (itemsLine != "None")
                            {
                                string[] itemNames = itemsLine.Split(';');
                                foreach (string itemName in itemNames)
                                {
                                    string trimmedName = itemName.Trim();
                                    if (!string.IsNullOrEmpty(trimmedName))
                                    {
                                        entityData.itemNames.Add(trimmedName);
                                    }
                                }
                            }
                            lineIndex++;
                        }
                    }

                    // 第3行：讀取 Patrol Points
                    if (lineIndex < lines.Length)
                    {
                        string patrolLine = lines[lineIndex].Trim();
                        if (!string.IsNullOrWhiteSpace(patrolLine) && !patrolLine.StartsWith("#"))
                        {
                            List<Vector3> patrolPoints = new List<Vector3>();
                            string[] pointsArray = patrolLine.Split('|');

                            foreach (string pointStr in pointsArray)
                            {
                                string[] coords = pointStr.Split(',');
                                if (coords.Length >= 2)
                                {
                                    float x = float.Parse(coords[0].Trim());
                                    float y = float.Parse(coords[1].Trim());
                                    float z = 0f;
                                    patrolPoints.Add(new Vector3(x, y, z));
                                }
                            }

                            entityData.patrolPoints = patrolPoints.ToArray();
                            lineIndex++;
                        }
                    }

                    // 第4行（可選）：讀取逃亡點（僅用於 Target）
                    if (entityData.type == EntityType.Target && lineIndex < lines.Length)
                    {
                        string escapeLine = lines[lineIndex].Trim();
                        if (!string.IsNullOrWhiteSpace(escapeLine) && !escapeLine.StartsWith("#"))
                        {
                            string[] escapeCoords = escapeLine.Split(',');
                            if (escapeCoords.Length >= 2)
                            {
                                float x = float.Parse(escapeCoords[0].Trim());
                                float y = float.Parse(escapeCoords[1].Trim());
                                entityData.escapePoint = new Vector3(x, y, 0f);
                                lineIndex++;
                            }
                        }
                    }

                    // 添加到列表
                    // Enemy 和 Target 需要 patrol points，Player 和 Exit 只需要位置（第一個點）
                    if (entityData.type == EntityType.Player || entityData.type == EntityType.Exit)
                    {
                        // Player 和 Exit 只需要第一個點作為初始位置
                        if (entityData.patrolPoints != null && entityData.patrolPoints.Length > 0)
                        {
                            entityDataList.Add(entityData);
                        }
                    }
                    else if (entityData.patrolPoints != null && entityData.patrolPoints.Length > 0)
                    {
                        entityDataList.Add(entityData);
                    }

                    // 跳過空行（用於分隔實體定義）
                    while (lineIndex < lines.Length && string.IsNullOrWhiteSpace(lines[lineIndex].Trim()))
                    {
                        lineIndex++;
                    }
                }

                if (showDebugInfo)
                {
                    Debug.Log($"EntityDataLoader: Loaded {entityDataList.Count} entities from {patrolDataFile.name}");
                }

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"EntityDataLoader: Error loading patrol data: {e.Message}");
                CreateDefaultPatrolData();
                return false;
            }
        }

        /// <summary>
        /// 創建默認的 patrol data（用於錯誤處理）
        /// </summary>
        private void CreateDefaultPatrolData()
        {
            entityDataList.Clear();

            // 創建一個默認的 Enemy
            EntityData defaultEnemy = new EntityData
            {
                entityIndex = 0,
                entityType = "Enemy",
                type = EntityType.Enemy,
                patrolPoints = new Vector3[] { Vector3.zero }
            };

            entityDataList.Add(defaultEnemy);

            if (showDebugInfo)
            {
                Debug.LogWarning("EntityDataLoader: Created default patrol data");
            }
        }

        /// <summary>
        /// 獲取指定類型的實體數據列表
        /// </summary>
        public List<EntityData> GetEntitiesByType(EntityType type)
        {
            List<EntityData> result = new List<EntityData>();
            foreach (var data in entityDataList)
            {
                if (data.type == type)
                {
                    result.Add(data);
                }
            }
            return result;
        }

        /// <summary>
        /// 獲取指定索引的實體數據
        /// </summary>
        public EntityData GetEntityData(int entityIndex, EntityType type)
        {
            foreach (var data in entityDataList)
            {
                if (data.entityIndex == entityIndex && data.type == type)
                {
                    return data;
                }
            }
            return null;
        }

        /// <summary>
        /// 獲取 Player 數據（應該只有一個）
        /// </summary>
        public EntityData GetPlayerData()
        {
            var playerDataList = GetEntitiesByType(EntityType.Player);
            if (playerDataList.Count > 0)
            {
                return playerDataList[0];
            }
            return null;
        }
    }
}

