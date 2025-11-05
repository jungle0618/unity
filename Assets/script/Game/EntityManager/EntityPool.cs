using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Game.EntityManager
{
    /// <summary>
    /// Enemy 對象池管理器
    /// 職責：管理 Enemy 對象池，提供對象的獲取和回收
    /// </summary>
    public class EntityPool
    {
        private Queue<Enemy> enemyPool = new Queue<Enemy>();
        private HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
        private HashSet<Enemy> culledEnemies = new HashSet<Enemy>();
        private List<Enemy> deadEnemies = new List<Enemy>();

        private GameObject enemyPrefab;
        private int poolSize;
        private bool showDebugInfo = false;

        // 統計資訊
        public int ActiveEnemyCount => activeEnemies.Count;
        public int PooledEnemyCount => enemyPool.Count;
        public int DeadEnemyCount => deadEnemies.Count;
        public int TotalEnemyCount => ActiveEnemyCount + PooledEnemyCount + culledEnemies.Count + DeadEnemyCount;

        public HashSet<Enemy> ActiveEnemies => activeEnemies;
        public HashSet<Enemy> CulledEnemies => culledEnemies;

        public EntityPool(GameObject enemyPrefab, int poolSize, bool showDebugInfo = false)
        {
            this.enemyPrefab = enemyPrefab;
            this.poolSize = poolSize;
            this.showDebugInfo = showDebugInfo;
        }

        /// <summary>
        /// 初始化對象池（使用協程分批創建，避免卡頓）
        /// </summary>
        public IEnumerator InitializePoolCoroutine(MonoBehaviour coroutineRunner, int enemiesPerBatch = 5)
        {
            if (enemyPrefab == null)
            {
                Debug.LogError("EntityPool: Enemy prefab is null!");
                yield break;
            }

            int totalToCreate = Mathf.Min(poolSize, poolSize);
            int created = 0;

            while (created < totalToCreate)
            {
                int batchSize = Mathf.Min(enemiesPerBatch, totalToCreate - created);

                for (int i = 0; i < batchSize; i++)
                {
                    Enemy enemy = CreatePooledEnemy();
                    if (enemy != null)
                    {
                        enemyPool.Enqueue(enemy);
                        created++;
                    }
                }

                if (showDebugInfo && created % 10 == 0)
                {
                    Debug.Log($"EntityPool: Initialized {created}/{totalToCreate} enemies");
                }

                // 每批次之間等待一幀，避免卡頓
                yield return null;
            }

            if (showDebugInfo)
            {
                Debug.Log($"EntityPool: Pool initialization complete. Total: {TotalEnemyCount}");
            }
        }

        /// <summary>
        /// 從對象池獲取 Enemy
        /// </summary>
        public Enemy GetPooledEnemy()
        {
            if (enemyPool.Count > 0)
            {
                return enemyPool.Dequeue();
            }

            // 池子空了，創建新的（如果允許的話）
            if (TotalEnemyCount < poolSize * 1.5f)
            {
                return CreatePooledEnemy();
            }

            return null;
        }

        /// <summary>
        /// 將 Enemy 返回到對象池
        /// </summary>
        public void ReturnEnemyToPool(Enemy enemy)
        {
            if (enemy == null) return;

            activeEnemies.Remove(enemy);
            culledEnemies.Remove(enemy);

            enemy.gameObject.SetActive(false);
            enemyPool.Enqueue(enemy);
        }

        /// <summary>
        /// 創建新的 Enemy 並加入對象池
        /// </summary>
        private Enemy CreatePooledEnemy()
        {
            if (enemyPrefab == null)
            {
                Debug.LogError("EntityPool: Cannot create enemy - prefab is null!");
                return null;
            }

            GameObject enemyGO = Object.Instantiate(enemyPrefab);
            Enemy enemy = enemyGO.GetComponent<Enemy>();

            if (enemy == null)
            {
                Debug.LogError($"EntityPool: Enemy prefab missing Enemy component! Prefab: {enemyPrefab.name}");
                Object.Destroy(enemyGO);
                return null;
            }

            // 設置名稱
            enemyGO.name = $"Enemy_Pool_{TotalEnemyCount}";

            // 立即禁用，等待使用
            enemyGO.SetActive(false);

            if (showDebugInfo)
            {
                Debug.Log($"EntityPool: Created pooled enemy {enemyGO.name}");
            }

            return enemy;
        }

        /// <summary>
        /// 標記 Enemy 為活躍
        /// </summary>
        public void MarkEnemyActive(Enemy enemy)
        {
            if (enemy == null) return;

            if (!activeEnemies.Contains(enemy))
            {
                activeEnemies.Add(enemy);
            }
            culledEnemies.Remove(enemy);
        }

        /// <summary>
        /// 標記 Enemy 為剔除狀態
        /// </summary>
        public void MarkEnemyCulled(Enemy enemy)
        {
            if (enemy == null) return;

            activeEnemies.Remove(enemy);
            if (!culledEnemies.Contains(enemy))
            {
                culledEnemies.Add(enemy);
            }
        }

        /// <summary>
        /// 標記 Enemy 為死亡
        /// </summary>
        public void MarkEnemyDead(Enemy enemy)
        {
            if (enemy == null) return;

            activeEnemies.Remove(enemy);
            culledEnemies.Remove(enemy);

            if (!deadEnemies.Contains(enemy))
            {
                deadEnemies.Add(enemy);
            }
        }

        /// <summary>
        /// 清空對象池
        /// </summary>
        public void ClearPool()
        {
            // 銷毀池中的所有敵人
            while (enemyPool.Count > 0)
            {
                Enemy enemy = enemyPool.Dequeue();
                if (enemy != null)
                {
                    Object.Destroy(enemy.gameObject);
                }
            }

            activeEnemies.Clear();
            culledEnemies.Clear();
            deadEnemies.Clear();

            if (showDebugInfo)
            {
                Debug.Log("EntityPool: Pool cleared");
            }
        }

        /// <summary>
        /// 獲取活躍的 Enemy 列表（用於批次處理）
        /// </summary>
        public List<Enemy> GetActiveEnemiesList()
        {
            return new List<Enemy>(activeEnemies);
        }
    }
}

