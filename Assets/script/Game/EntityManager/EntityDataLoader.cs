using UnityEngine;
using System.Collections.Generic;

namespace Game.EntityManager
{
    /// <summary>
    /// 實體數據載入器
    /// 職責：從 patroldata.json 載入實體數據（Enemy, Target, Player）
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

        // JSON 序列化用的 Vector3 結構
        [System.Serializable]
        private class Vector3Data
        {
            public float x;
            public float y;
            public float z;
        }

        // JSON 序列化用的實體數據結構
        [System.Serializable]
        private class EntityDataJson
        {
            public int entityIndex;
            public string type;
            public string[] items;
            public Vector3Data[] patrolPoints;
            public float rotation;
            public Vector3Data escapePoint;
        }

        // JSON 根結構
        [System.Serializable]
        private class PatrolDataJson
        {
            public EntityDataJson[] entities;
        }

        // 實體資料結構（擴展以支持不同類型）
        [System.Serializable]
        public class EntityData
        {
            public int entityIndex;
            public EntityType type = EntityType.Enemy; // 實體類型
            public List<string> itemNames = new List<string>();
            public Vector3[] patrolPoints;
            public Vector3 escapePoint = Vector3.zero; // Target 的逃亡點（可選）
            public float initialRotation = 0f; // 初始朝向（度數）
        }

        private List<EntityData> entityDataList = new List<EntityData>();
        private bool showDebugInfo = false;

        public List<EntityData> EntityDataList => entityDataList;

        public EntityDataLoader(bool showDebugInfo = false)
        {
            this.showDebugInfo = showDebugInfo;
        }

        /// <summary>
        /// 從TextAsset載入所有實體的patrol points（JSON 格式）
        /// </summary>
        public bool LoadPatrolData(TextAsset patrolDataFile)
        {
            entityDataList.Clear();

            if (patrolDataFile == null)
            {
                Debug.LogError("EntityDataLoader: Patrol data file (TextAsset) is not assigned! Please assign the patroldata.json file in the inspector.");
                CreateDefaultPatrolData();
                return false;
            }

            return LoadJsonFormat(patrolDataFile);
        }

        /// <summary>
        /// 從 JSON 格式載入數據
        /// </summary>
        private bool LoadJsonFormat(TextAsset patrolDataFile)
        {
            try
            {
                PatrolDataJson jsonData = JsonUtility.FromJson<PatrolDataJson>(patrolDataFile.text);
                
                if (jsonData == null || jsonData.entities == null)
                {
                    return false;
                }

                foreach (var jsonEntity in jsonData.entities)
                {
                    EntityData entityData = new EntityData();
                    entityData.entityIndex = jsonEntity.entityIndex;
                    entityData.initialRotation = jsonEntity.rotation;

                    // 解析類型
                    if (jsonEntity.type.Equals("Target", System.StringComparison.OrdinalIgnoreCase))
                    {
                        entityData.type = EntityType.Target;
                    }
                    else if (jsonEntity.type.Equals("Player", System.StringComparison.OrdinalIgnoreCase))
                    {
                        entityData.type = EntityType.Player;
                    }
                    else if (jsonEntity.type.Equals("Exit", System.StringComparison.OrdinalIgnoreCase))
                    {
                        entityData.type = EntityType.Exit;
                    }
                    else
                    {
                        entityData.type = EntityType.Enemy;
                    }

                    // 解析物品列表
                    if (jsonEntity.items != null)
                    {
                        foreach (string item in jsonEntity.items)
                        {
                            if (!string.IsNullOrEmpty(item))
                            {
                                entityData.itemNames.Add(item);
                            }
                        }
                    }

                    // 解析巡邏點
                    if (jsonEntity.patrolPoints != null && jsonEntity.patrolPoints.Length > 0)
                    {
                        List<Vector3> patrolPoints = new List<Vector3>();
                        foreach (var point in jsonEntity.patrolPoints)
                        {
                            patrolPoints.Add(new Vector3(point.x, point.y, point.z));
                        }
                        entityData.patrolPoints = patrolPoints.ToArray();
                    }

                    // 解析逃亡點（僅用於 Target）
                    if (jsonEntity.escapePoint != null && entityData.type == EntityType.Target)
                    {
                        entityData.escapePoint = new Vector3(
                            jsonEntity.escapePoint.x,
                            jsonEntity.escapePoint.y,
                            jsonEntity.escapePoint.z
                        );
                    }

                    // 驗證數據有效性
                    if (entityData.patrolPoints != null && entityData.patrolPoints.Length > 0)
                    {
                        entityDataList.Add(entityData);
                    }
                }

                if (showDebugInfo)
                {
                    Debug.Log($"EntityDataLoader: Successfully loaded {entityDataList.Count} entities from JSON format");
                }

                return entityDataList.Count > 0;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"EntityDataLoader: Error loading JSON format patrol data: {e.Message}");
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

