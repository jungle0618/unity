using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Game.EntityManager
{
    /// <summary>
    /// 實體生成器
    /// 職責：生成和初始化所有實體（Player, Enemy, Target）
    /// </summary>
    public class EntitySpawner
    {
        private GameObject playerPrefab;
        private GameObject enemyPrefab;
        private GameObject targetPrefab;

        private Player player;
        // 注意：activeTargets 已移至 EntityEventManager，統一管理

        private List<Enemy> spawnedEnemies = new List<Enemy>();
        private EntityDataLoader dataLoader;
        private EntityItemManager itemManager;
        private EntityEventManager eventManager;
        private AttackSystem attackSystem;

        private DangerousManager dangerousManager;
        private PlayerDetection playerDetection;
        private bool enablePlayerDetection;
        private bool autoRegisterWithPlayerDetection;

        private float aiUpdateInterval;
        private bool showDebugInfo = false;

        // 危險等級乘數
        private System.Func<DangerousManager.DangerLevel, DangerLevelMultipliers> getMultipliersForLevel;

        // 公共屬性
        public Player Player => player;
        // 注意：ActiveTargets 已移至 EntityEventManager
        public Vector3 PlayerInitialPosition { get; private set; }
        public List<string> PlayerInitialItems { get; private set; }
        public List<Enemy> SpawnedEnemies => spawnedEnemies;

        public EntitySpawner(
            GameObject playerPrefab,
            GameObject enemyPrefab,
            GameObject targetPrefab,
            EntityDataLoader dataLoader,
            EntityItemManager itemManager,
            EntityEventManager eventManager,
            AttackSystem attackSystem,
            DangerousManager dangerousManager,
            PlayerDetection playerDetection,
            bool enablePlayerDetection,
            bool autoRegisterWithPlayerDetection,
            float aiUpdateInterval,
            System.Func<DangerousManager.DangerLevel, DangerLevelMultipliers> getMultipliersForLevel,
            bool showDebugInfo = false)
        {
            this.playerPrefab = playerPrefab;
            this.enemyPrefab = enemyPrefab;
            this.targetPrefab = targetPrefab;
            this.dataLoader = dataLoader;
            this.itemManager = itemManager;
            this.eventManager = eventManager;
            this.attackSystem = attackSystem;
            this.dangerousManager = dangerousManager;
            this.playerDetection = playerDetection;
            this.enablePlayerDetection = enablePlayerDetection;
            this.autoRegisterWithPlayerDetection = autoRegisterWithPlayerDetection;
            this.aiUpdateInterval = aiUpdateInterval;
            this.getMultipliersForLevel = getMultipliersForLevel;
            this.showDebugInfo = showDebugInfo;

            PlayerInitialItems = new List<string>();
        }

        /// <summary>
        /// 初始化玩家引用（從 Prefab 生成）
        /// </summary>
        public bool InitializePlayer()
        {
            // 如果已經有 Player 實例，不重複生成
            if (player != null)
            {
                if (showDebugInfo)
                {
                    Debug.Log("[EntitySpawner] Player already exists, skipping spawn");
                }
                return true;
            }

            // 從 Prefab 生成 Player
            GameObject playerGO = Object.Instantiate(playerPrefab);
            player = playerGO.GetComponent<Player>();

            if (player == null)
            {
                Debug.LogError($"[EntitySpawner] Player prefab missing Player component! Prefab: {playerPrefab.name}");
                Object.Destroy(playerGO);
                return false;
            }

            // 設置 Player 名稱
            player.gameObject.name = "Player";

            if (showDebugInfo)
            {
                Debug.Log($"[EntitySpawner] Player spawned from prefab: {playerPrefab.name}");
            }

            return true;
        }

        /// <summary>
        /// 生成 Enemy
        /// </summary>
        public void SpawnEnemy(Vector3 position, int enemyIndex = -1)
        {
            // 驗證 itemManager 是否準備好
            if (itemManager == null || itemManager.ItemMappingDict == null || itemManager.ItemMappingDict.Count == 0)
            {
                Debug.LogError("EntitySpawner: itemManager 未初始化或物品映射為空！無法生成帶有物品的 Enemy。");
                return;
            }
            
            // 獲取實體資料（只查找 Enemy 類型的資料）
            var enemyDataList = dataLoader.GetEntitiesByType(EntityDataLoader.EntityType.Enemy);
            EntityDataLoader.EntityData enemyData = null;

            if (enemyIndex >= 0 && enemyIndex < enemyDataList.Count)
            {
                enemyData = enemyDataList[enemyIndex];
            }
            else if (enemyDataList.Count > 0)
            {
                // 使用隨機敵人資料
                int randomIndex = Random.Range(0, enemyDataList.Count);
                enemyData = enemyDataList[randomIndex];
            }

            if (enemyData == null || enemyData.patrolPoints == null || enemyData.patrolPoints.Length == 0)
            {
                Debug.LogError($"EntitySpawner: No valid enemy data for enemy index {enemyIndex}");
                return;
            }

            // 直接實例化 Enemy
            if (enemyPrefab == null)
            {
                Debug.LogError("EntitySpawner: Enemy prefab is not assigned!");
                return;
            }

            GameObject enemyGO = Object.Instantiate(enemyPrefab);
            Enemy enemy = enemyGO.GetComponent<Enemy>();

            if (enemy == null)
            {
                Debug.LogError($"EntitySpawner: Enemy prefab missing Enemy component! Prefab: {enemyPrefab.name}");
                Object.Destroy(enemyGO);
                return;
            }

            // 清空現有物品並裝備新物品
            if (enemy.ItemHolder != null)
            {
                enemy.ItemHolder.ClearAllItems();
                itemManager.EquipItemsToEntity(enemy, enemyData.itemNames);
            }

            // 設定名稱
            enemy.gameObject.name = $"Enemy_{enemyData.entityIndex}_{enemyData.type}";

            // 先設定 patrol locations
            enemy.SetPatrolLocations(enemyData.patrolPoints);

            // 啟用敵人
            enemy.gameObject.SetActive(true);

            // 將敵人移動到第一個巡邏點（出生地）
            if (enemyData.patrolPoints != null && enemyData.patrolPoints.Length > 0)
            {
                enemy.transform.position = enemyData.patrolPoints[0];
                // 設定初始朝向
                enemy.transform.rotation = Quaternion.Euler(0f, 0f, enemyData.initialRotation);
                if (showDebugInfo)
                {
                    Debug.Log($"EntitySpawner: Moved enemy to spawn point {enemyData.patrolPoints[0]} with rotation {enemyData.initialRotation}°");
                }
            }

            // 初始化敵人
            enemy.Initialize(player != null ? player.transform : null);

            if (showDebugInfo)
            {
                Debug.Log($"EntitySpawner: Initialized enemy at {enemy.transform.position}, Active: {enemy.gameObject.activeSelf}");
            }

            // 加入已生成列表
            spawnedEnemies.Add(enemy);

            // 註冊到統一實體註冊表（通過 AttackSystem）
            if (enemy is IEntity entity)
            {
                attackSystem.ActiveEntities.Add(entity);
            }

            // 訂閱攻擊事件
            eventManager.SubscribeToEnemyEvents(enemy);
            
            // 注意：敵人死亡事件訂閱在 EntityManager 中處理，這裡不需要訂閱

            // 設定 AI 更新間隔
            enemy.SetAIUpdateInterval(aiUpdateInterval + Random.Range(0f, aiUpdateInterval * 0.5f));

            // 根據當前危險等級初始化敵人屬性
            if (dangerousManager != null && getMultipliersForLevel != null)
            {
                DangerousManager.DangerLevel currentLevel = dangerousManager.CurrentDangerLevelType;
                DangerLevelMultipliers multipliers = getMultipliersForLevel(currentLevel);

                enemy.UpdateDangerLevelStats(multipliers.viewRangeMultiplier, multipliers.viewAngleMultiplier,
                                            multipliers.speedMultiplier, multipliers.damageReduction);
            }

            // 註冊到玩家偵測系統
            if (enablePlayerDetection && autoRegisterWithPlayerDetection && playerDetection != null && enemy is IEntity enemyEntity)
            {
                playerDetection.AddEntity(enemyEntity);
            }

            if (showDebugInfo)
            {
                string itemsStr = enemyData.itemNames.Count > 0 ? string.Join(", ", enemyData.itemNames) : "None";
                Debug.Log($"EntitySpawner: Spawned enemy {enemyData.entityIndex} ({enemyData.type}) at {enemy.transform.position} with items [{itemsStr}] and {enemyData.patrolPoints.Length} patrol points");
            }
        }

        /// <summary>
        /// 生成 Target
        /// </summary>
        public void SpawnTarget(Vector3 position, Vector3 escapePoint, int targetIndex)
        {
            if (targetPrefab == null)
            {
                Debug.LogError("EntitySpawner: Target prefab is not assigned!");
                return;
            }

            // 驗證 itemManager 是否準備好
            if (itemManager == null || itemManager.ItemMappingDict == null || itemManager.ItemMappingDict.Count == 0)
            {
                Debug.LogError("EntitySpawner: itemManager 未初始化或物品映射為空！無法生成帶有物品的 Target。");
                return;
            }

            GameObject targetGO = Object.Instantiate(targetPrefab);
            Target target = targetGO.GetComponent<Target>();

            if (target == null)
            {
                Debug.LogError($"EntitySpawner: Target prefab missing Target component! Prefab: {targetPrefab.name}");
                Object.Destroy(targetGO);
                return;
            }

            // 設置名稱
            target.gameObject.name = $"Target_{targetIndex}";

            // 獲取 Target 數據（在設置位置和朝向之前）
            var targetData = dataLoader.GetEntityData(targetIndex, EntityDataLoader.EntityType.Target);

            // 設置位置和朝向
            target.transform.position = position;
            // 設定初始朝向（從數據中獲取，如果沒有則使用預設值 0）
            float targetRotation = 0f;
            if (targetData != null)
            {
                targetRotation = targetData.initialRotation;
            }
            target.transform.rotation = Quaternion.Euler(0f, 0f, targetRotation);

            // 初始化 Target（設置逃亡點）
            if (target is IEntity targetEntity)
            {
                // 註冊到統一實體註冊表
                attackSystem.ActiveEntities.Add(targetEntity);
            }

            // 裝備物品（itemManager 已驗證）
            if (targetData != null)
            {
                // 裝備物品
                if (target.ItemHolder != null)
                {
                    itemManager.EquipItemsToEntity(target, targetData.itemNames);
                }
                
                // 設置 patrol locations（如果有的話）
                if (targetData.patrolPoints != null && targetData.patrolPoints.Length > 0)
                {
                    target.SetPatrolLocations(targetData.patrolPoints);
                }
            }

            // 初始化 Target（設置 Player target 和逃亡點，啟動 AI）
            // 這會設置 isInitialized = true，啟動狀態機和 AI 邏輯
            Transform playerTransform = player != null ? player.transform : null;
            target.Initialize(playerTransform, escapePoint);

            // 訂閱事件（EntityEventManager 會管理 activeTargets）
            eventManager.AddTarget(target);

            if (showDebugInfo)
            {
                Debug.Log($"EntitySpawner: Spawned Target {targetIndex} at {position} with escape point {escapePoint}");
            }
        }

        /// <summary>
        /// 生成所有初始實體（從數據讀取）
        /// </summary>
        public void SpawnInitialEntities()
        {
            if (dataLoader.EntityDataList.Count == 0)
            {
                Debug.LogError("EntitySpawner: No patrol data loaded!");
                return;
            }

            // 確保 Player 已生成
            if (player == null)
            {
                Debug.LogError("[EntitySpawner] Player is null! Cannot set initial position.");
                return;
            }

            // 設置玩家初始位置（如果數據中有 Player 類型）
            var playerData = dataLoader.GetPlayerData();
            if (playerData != null && playerData.patrolPoints != null && playerData.patrolPoints.Length > 0)
            {
                PlayerInitialPosition = playerData.patrolPoints[0];
                player.transform.position = PlayerInitialPosition;
                // 設定初始朝向
                player.transform.rotation = Quaternion.Euler(0f, 0f, playerData.initialRotation);

                // 保存玩家初始物品
                PlayerInitialItems = new List<string>(playerData.itemNames);

                if (showDebugInfo)
                {
                    Debug.Log($"EntitySpawner: Set player initial position to {PlayerInitialPosition} with rotation {playerData.initialRotation}°");
                }
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.Log($"EntitySpawner: Player spawned at default position: {player.transform.position}");
                }
            }

            // 裝備玩家初始物品（驗證 itemManager 已準備好）
            if (PlayerInitialItems != null && PlayerInitialItems.Count > 0)
            {
                if (itemManager == null || itemManager.ItemMappingDict == null || itemManager.ItemMappingDict.Count == 0)
                {
                    Debug.LogError("EntitySpawner: itemManager 未初始化或物品映射為空！無法為 Player 裝備初始物品。");
                }
                else
                {
                    itemManager.EquipItemsToEntity(player, PlayerInitialItems);
                }
            }
            else if (showDebugInfo)
            {
                Debug.Log("EntitySpawner: No initial items for Player from patroldata.txt, using prefab default items");
            }

            // 訂閱 Player 攻擊事件
            eventManager.SubscribeToPlayerEvents(player);

            // 生成所有 Target
            SpawnAllTargets();

            // 生成所有 Enemy
            var enemyDataList = dataLoader.GetEntitiesByType(EntityDataLoader.EntityType.Enemy);
            int enemyCount = enemyDataList.Count;

            Debug.Log($"EntitySpawner: Attempting to spawn {enemyCount} enemies");

            // 生成所有 Enemy 類型的實體
            int enemyIndex = 0;
            foreach (var data in enemyDataList)
            {
                if (data.type == EntityDataLoader.EntityType.Enemy)
                {
                    SpawnEnemy(Vector3.zero, enemyIndex);
                    enemyIndex++;
                }
            }

            Debug.Log($"EntitySpawner: Successfully spawned {spawnedEnemies.Count} enemies and {eventManager.ActiveTargets.Count} targets");
        }

        /// <summary>
        /// 生成所有 Target
        /// </summary>
        private void SpawnAllTargets()
        {
            var targetDataList = dataLoader.GetEntitiesByType(EntityDataLoader.EntityType.Target);

            foreach (var targetData in targetDataList)
            {
                if (targetData.patrolPoints != null && targetData.patrolPoints.Length > 0)
                {
                    Vector3 spawnPosition = targetData.patrolPoints[0];
                    Vector3 escapePoint = targetData.escapePoint != Vector3.zero ? targetData.escapePoint : spawnPosition;

                    SpawnTarget(spawnPosition, escapePoint, targetData.entityIndex);
                }
            }
        }

        // 注意：DangerLevelMultipliers 需要從 EntityManager 傳入，這裡使用簡單的類定義
        [System.Serializable]
        public class DangerLevelMultipliers
        {
            public float viewRangeMultiplier = 1.0f;
            public float viewAngleMultiplier = 1.0f;
            public float speedMultiplier = 1.0f;
            public float damageReduction = 0f;
        }
    }
}

