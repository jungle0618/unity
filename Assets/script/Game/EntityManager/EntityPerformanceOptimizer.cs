using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Game.EntityManager
{
    /// <summary>
    /// 實體性能優化器
    /// 職責：性能優化（視錐剔除、批次處理、位置快取）
    /// </summary>
    public class EntityPerformanceOptimizer
    {
        private EntityPool entityPool;
        private Player player;
        private MonoBehaviour coroutineRunner;

        // 性能參數
        private float cullingDistance;
        private float updateInterval;
        private int enemiesPerFrameUpdate;
        private float aiUpdateInterval;

        // 快取和狀態
        private Vector3 cachedPlayerPosition;
        private float playerPositionUpdateTime = 0f;
        private const float PLAYER_POSITION_UPDATE_INTERVAL = 0.1f;

        // 批次處理
        private int currentUpdateIndex = 0;
        private List<Enemy> activeEnemiesList = new List<Enemy>();

        // 協程控制
        private Coroutine managementCoroutine;
        private WaitForSeconds updateWait;
        private WaitForSeconds aiUpdateWait;

        private bool showDebugInfo = false;

        public EntityPerformanceOptimizer(
            EntityPool entityPool,
            Player player,
            MonoBehaviour coroutineRunner,
            float cullingDistance,
            float updateInterval,
            int enemiesPerFrameUpdate,
            float aiUpdateInterval,
            bool showDebugInfo = false)
        {
            this.entityPool = entityPool;
            this.player = player;
            this.coroutineRunner = coroutineRunner;
            this.cullingDistance = cullingDistance;
            this.updateInterval = updateInterval;
            this.enemiesPerFrameUpdate = enemiesPerFrameUpdate;
            this.aiUpdateInterval = aiUpdateInterval;
            this.showDebugInfo = showDebugInfo;

            updateWait = new WaitForSeconds(updateInterval);
            aiUpdateWait = new WaitForSeconds(aiUpdateInterval);
            if (player != null)
            {
                cachedPlayerPosition = player.transform.position;
            }
        }

        /// <summary>
        /// 更新快取的玩家位置
        /// </summary>
        public void UpdateCachedPlayerPosition()
        {
            if (player == null) return;

            if (Time.time - playerPositionUpdateTime >= PLAYER_POSITION_UPDATE_INTERVAL)
            {
                cachedPlayerPosition = player.transform.position;
                playerPositionUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// 開始管理循環
        /// </summary>
        public void StartManagement()
        {
            if (managementCoroutine != null)
            {
                coroutineRunner.StopCoroutine(managementCoroutine);
            }

            managementCoroutine = coroutineRunner.StartCoroutine(ManagementLoop());
        }

        /// <summary>
        /// 停止管理循環
        /// </summary>
        public void StopManagement()
        {
            if (managementCoroutine != null)
            {
                coroutineRunner.StopCoroutine(managementCoroutine);
                managementCoroutine = null;
            }
        }

        /// <summary>
        /// 管理循環協程
        /// </summary>
        private IEnumerator ManagementLoop()
        {
            while (coroutineRunner != null && coroutineRunner.enabled)
            {
                yield return updateWait;

                UpdateEnemyCullingOptimized();

                if (showDebugInfo)
                {
                    UpdateDebugInfo();
                }
            }
        }

        /// <summary>
        /// 優化的敵人剔除更新
        /// </summary>
        private void UpdateEnemyCullingOptimized()
        {
            // 批次處理：每次只處理部分敵人以分散 CPU 負載
            activeEnemiesList.Clear();
            activeEnemiesList.AddRange(entityPool.ActiveEnemies);

            int enemiesToProcess = Mathf.Min(enemiesPerFrameUpdate, activeEnemiesList.Count);
            float cullingDistanceSqr = cullingDistance * cullingDistance; // 使用平方距離避免開根號

            // 處理活躍敵人
            for (int i = 0; i < enemiesToProcess; i++)
            {
                if (currentUpdateIndex >= activeEnemiesList.Count)
                    currentUpdateIndex = 0;

                if (currentUpdateIndex < activeEnemiesList.Count)
                {
                    Enemy enemy = activeEnemiesList[currentUpdateIndex];
                    if (enemy == null)
                    {
                        entityPool.ActiveEnemies.Remove(enemy);
                    }
                    else if (enemy.IsDead)
                    {
                        // 敵人已死亡，移除並清理
                        entityPool.ActiveEnemies.Remove(enemy);
                        entityPool.CulledEnemies.Remove(enemy);
                        entityPool.MarkEnemyDead(enemy);
                    }
                    else
                    {
                        // 檢查敵人是否在剔除距離內
                        float distanceSqr = (enemy.Position - (Vector2)cachedPlayerPosition).sqrMagnitude;

                        if (distanceSqr > cullingDistanceSqr)
                        {
                            // 將敵人移除到剔除列表
                            entityPool.MarkEnemyCulled(enemy);
                            enemy.gameObject.SetActive(false);
                        }
                    }
                    currentUpdateIndex++;
                }
            }

            // 檢查剔除的敵人是否需要重新激活（提高頻率以確保響應及時）
            if (Time.frameCount % 10 == 0) // 每 10 幀檢查一次
            {
                CheckCulledEnemiesForReactivation(cullingDistanceSqr);
            }
        }

        /// <summary>
        /// 檢查剔除的敵人是否需要重新激活
        /// </summary>
        private void CheckCulledEnemiesForReactivation(float cullingDistanceSqr)
        {
            var culledList = new List<Enemy>(entityPool.CulledEnemies);

            foreach (var enemy in culledList)
            {
                if (enemy == null || enemy.IsDead)
                {
                    entityPool.CulledEnemies.Remove(enemy);
                    continue;
                }

                // 獲取敵人位置：如果敵人被禁用，使用第一個巡邏點作為參考位置
                Vector2 enemyPosition;
                if (!enemy.gameObject.activeSelf)
                {
                    // 敵人被禁用時，使用第一個巡邏點作為位置參考
                    Vector3 firstPatrolLocation = enemy.GetFirstPatrolLocation();
                    enemyPosition = firstPatrolLocation;
                }
                else
                {
                    enemyPosition = enemy.Position;
                }

                float distanceSqr = (enemyPosition - (Vector2)cachedPlayerPosition).sqrMagnitude;

                // 如果敵人非常接近玩家（在視野範圍內），應該立即激活
                // 或者如果距離在剔除距離內且未達到最大數量限制
                bool shouldReactivate = false;

                if (distanceSqr <= cullingDistanceSqr)
                {
                    // 如果敵人非常接近（在視野範圍內，假設 15 單位），優先激活
                    float viewRangeSqr = 15f * 15f; // 視野範圍的平方
                    if (distanceSqr <= viewRangeSqr)
                    {
                        // 非常接近玩家，優先激活
                        shouldReactivate = true;
                    }
                    else if (entityPool.ActiveEnemyCount < GetMaxActiveEnemies())
                    {
                        // 在剔除距離內但未達到最大數量，可以激活
                        shouldReactivate = true;
                    }
                }

                if (shouldReactivate)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"EntityPerformanceOptimizer: Reactivating enemy {enemy.gameObject.name} at distance {Mathf.Sqrt(distanceSqr):F2}");
                    }

                    // 重新激活敵人
                    entityPool.MarkEnemyActive(enemy);

                    // 激活敵人 GameObject
                    if (!enemy.gameObject.activeSelf)
                    {
                        enemy.gameObject.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// 獲取最大活躍敵人數量（需要從外部注入或查詢）
        /// </summary>
        private int GetMaxActiveEnemies()
        {
            // 這個值應該從 EntityManager 或配置中獲取
            // 暫時返回一個合理的默認值
            return 36;
        }

        /// <summary>
        /// 設置最大活躍敵人數量（用於動態調整）
        /// </summary>
        public void SetMaxActiveEnemies(int maxEnemies)
        {
            // 這個方法可以擴展以支持動態調整
        }

        /// <summary>
        /// 更新除錯資訊
        /// </summary>
        private void UpdateDebugInfo()
        {
            if (showDebugInfo && Time.frameCount % 60 == 0) // 降低日誌頻率
            {
                Debug.Log($"EntityPerformanceOptimizer - Active: {entityPool.ActiveEnemyCount}, " +
                         $"Culled: {entityPool.CulledEnemies.Count}, " +
                         $"Dead: {entityPool.DeadEnemyCount}, " +
                         $"Pooled: {entityPool.PooledEnemyCount}");
            }
        }

        /// <summary>
        /// 設置性能參數
        /// </summary>
        public void SetPerformanceParameters(float cullingDistance, float updateInterval, int enemiesPerFrameUpdate, float aiUpdateInterval)
        {
            this.cullingDistance = cullingDistance;
            this.updateInterval = updateInterval;
            this.enemiesPerFrameUpdate = enemiesPerFrameUpdate;
            this.aiUpdateInterval = aiUpdateInterval;

            updateWait = new WaitForSeconds(updateInterval);
            aiUpdateWait = new WaitForSeconds(aiUpdateInterval);
        }
    }
}


