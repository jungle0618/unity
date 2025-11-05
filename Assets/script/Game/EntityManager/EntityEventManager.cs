using UnityEngine;
using System.Collections.Generic;

namespace Game.EntityManager
{
    /// <summary>
    /// 實體事件管理器
    /// 職責：管理所有實體的事件訂閱和處理
    /// </summary>
    public class EntityEventManager
    {
        private List<Target> activeTargets = new List<Target>();
        private AttackSystem attackSystem;
        private bool showDebugInfo = false;

        // 事件回調（由 EntityManager 提供）
        public System.Action<Target> OnTargetDied;
        public System.Action<Target> OnTargetReachedEscapePoint;

        public List<Target> ActiveTargets => activeTargets;

        public EntityEventManager(AttackSystem attackSystem, bool showDebugInfo = false)
        {
            this.attackSystem = attackSystem;
            this.showDebugInfo = showDebugInfo;
        }

        /// <summary>
        /// 訂閱 Player 的攻擊事件
        /// </summary>
        public void SubscribeToPlayerEvents(Player player)
        {
            if (player == null) return;

            var itemHolder = player.GetComponent<ItemHolder>();
            if (itemHolder != null)
            {
                // 先取消訂閱（避免重複訂閱）
                itemHolder.OnAttackPerformed -= attackSystem.HandleAttack;
                // 再訂閱
                itemHolder.OnAttackPerformed += attackSystem.HandleAttack;

                if (showDebugInfo)
                {
                    Debug.Log("[EntityEventManager] Subscribed to Player attack events");
                }
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning("[EntityEventManager] Player ItemHolder not found, cannot subscribe to attack events");
            }
        }

        /// <summary>
        /// 取消訂閱 Player 事件
        /// </summary>
        public void UnsubscribeFromPlayerEvents(Player player)
        {
            if (player == null) return;

            var itemHolder = player.GetComponent<ItemHolder>();
            if (itemHolder != null)
            {
                itemHolder.OnAttackPerformed -= attackSystem.HandleAttack;
            }
        }

        /// <summary>
        /// 訂閱 Enemy 的攻擊事件
        /// </summary>
        public void SubscribeToEnemyEvents(Enemy enemy)
        {
            if (enemy == null) return;

            var itemHolder = enemy.GetComponent<ItemHolder>();
            if (itemHolder != null)
            {
                itemHolder.OnAttackPerformed += attackSystem.HandleAttack;

                if (showDebugInfo)
                {
                    Debug.Log($"[EntityEventManager] Subscribed to Enemy attack events: {enemy.name}");
                }
            }
        }

        /// <summary>
        /// 取消訂閱 Enemy 事件
        /// </summary>
        public void UnsubscribeFromEnemyEvents(Enemy enemy)
        {
            if (enemy == null) return;

            var itemHolder = enemy.GetComponent<ItemHolder>();
            if (itemHolder != null)
            {
                itemHolder.OnAttackPerformed -= attackSystem.HandleAttack;
            }
        }

        /// <summary>
        /// 訂閱 Target 事件（在初始化時調用）
        /// </summary>
        public void SubscribeToTargetEvents()
        {
            // 查找場景中所有的 Target
            Target[] allTargets = Object.FindObjectsByType<Target>(FindObjectsSortMode.None);

            foreach (var target in allTargets)
            {
                if (target != null && !activeTargets.Contains(target))
                {
                    activeTargets.Add(target);
                    target.OnTargetDied += HandleTargetDied;
                    target.OnTargetReachedEscapePoint += HandleTargetReachedEscapePoint;

                    if (showDebugInfo)
                    {
                        Debug.Log($"[EntityEventManager] 訂閱 Target 事件: {target.gameObject.name}");
                    }
                }
            }
        }

        /// <summary>
        /// 手動添加 Target 到事件訂閱
        /// </summary>
        public void AddTarget(Target target)
        {
            if (target == null || activeTargets.Contains(target)) return;

            activeTargets.Add(target);
            target.OnTargetDied += HandleTargetDied;
            target.OnTargetReachedEscapePoint += HandleTargetReachedEscapePoint;
            
            // 註冊到 GameManager（如果存在）
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterTarget(target);
            }

            if (showDebugInfo)
            {
                Debug.Log($"[EntityEventManager] Added Target to event subscription: {target.gameObject.name}");
            }
        }

        /// <summary>
        /// 取消訂閱 Target 事件
        /// </summary>
        public void UnsubscribeFromTargetEvents()
        {
            foreach (var target in activeTargets)
            {
                if (target != null)
                {
                    target.OnTargetDied -= HandleTargetDied;
                    target.OnTargetReachedEscapePoint -= HandleTargetReachedEscapePoint;
                }
            }
            activeTargets.Clear();
        }

        /// <summary>
        /// 處理 Target 死亡事件
        /// </summary>
        private void HandleTargetDied(Target target)
        {
            if (target == null) return;

            if (showDebugInfo)
            {
                Debug.Log($"[EntityEventManager] Target died: {target.gameObject.name}");
            }

            // 通知外部（通過事件）
            OnTargetDied?.Invoke(target);
        }

        /// <summary>
        /// 處理 Target 到達逃亡點事件
        /// </summary>
        private void HandleTargetReachedEscapePoint(Target target)
        {
            if (target == null) return;

            if (showDebugInfo)
            {
                Debug.Log($"[EntityEventManager] Target reached escape point: {target.gameObject.name}");
            }

            // 通知外部（通過事件）
            OnTargetReachedEscapePoint?.Invoke(target);
        }

        /// <summary>
        /// 清理所有事件訂閱
        /// </summary>
        public void Cleanup()
        {
            UnsubscribeFromTargetEvents();
            activeTargets.Clear();
        }
    }
}


