using UnityEngine;
using System.Collections.Generic;

namespace Game.EntityManager
{
    /// <summary>
    /// 統一攻擊系統
    /// 職責：處理所有實體的攻擊，包括傷害計算、範圍檢測、攻擊規則
    /// </summary>
    public class AttackSystem
    {
        // 使用 EntityDataLoader.EntityType 統一類型定義（移除重複的枚舉）
        // 注意：IEntity.GetEntityType() 返回 EntityManager.EntityType，需要轉換

        private HashSet<IEntity> activeEntities = new HashSet<IEntity>();
        private bool showDebugInfo = false;

        public HashSet<IEntity> ActiveEntities => activeEntities;

        public AttackSystem(bool showDebugInfo = false)
        {
            this.showDebugInfo = showDebugInfo;
        }

        /// <summary>
        /// 統一處理所有實體的攻擊（Player, Enemy, Target 等）
        /// </summary>
        /// <param name="attackCenter">攻擊中心位置</param>
        /// <param name="attackRange">攻擊範圍</param>
        /// <param name="attacker">攻擊者 GameObject</param>
        public void HandleAttack(Vector2 attackCenter, float attackRange, GameObject attacker)
        {
            if (attacker == null) return;

            float rangeSqr = attackRange * attackRange;

            // 從攻擊者獲取武器傷害值
            int attackDamage = GetAttackDamage(attacker);

            // 獲取攻擊者類型（用於判斷可以攻擊哪些目標）
            var attackerType = GetEntityType(attacker);
            string damageSource = GetDamageSourceName(attacker, attackerType);

            if (showDebugInfo)
            {
                Debug.Log($"[AttackSystem] HandleAttack: {attackerType} at {attackCenter}, range={attackRange}, damage={attackDamage}");
            }

            // 檢查範圍內的所有實體並造成傷害
            CheckEntitiesInAttackRange(attackCenter, rangeSqr, attacker, attackDamage, attackerType, damageSource);
        }

        /// <summary>
        /// 從攻擊者獲取攻擊傷害值
        /// </summary>
        private int GetAttackDamage(GameObject attacker)
        {
            if (attacker == null) return 1; // 預設傷害值

            var itemHolder = attacker.GetComponent<ItemHolder>();
            if (itemHolder != null && itemHolder.IsCurrentItemWeapon)
            {
                var weapon = itemHolder.CurrentWeapon;
                if (weapon != null)
                {
                    return weapon.AttackDamage;
                }
            }

            return 1; // 預設傷害值
        }

        /// <summary>
        /// 獲取實體類型
        /// </summary>
        private EntityDataLoader.EntityType GetEntityType(GameObject entity)
        {
            if (entity == null) return EntityDataLoader.EntityType.None;

            if (entity.GetComponent<Player>() != null)
                return EntityDataLoader.EntityType.Player;
            if (entity.GetComponent<Enemy>() != null)
                return EntityDataLoader.EntityType.Enemy;
            if (entity.GetComponent<Target>() != null)
                return EntityDataLoader.EntityType.Target;

            return EntityDataLoader.EntityType.None;
        }

        /// <summary>
        /// 獲取傷害來源名稱（用於日誌）
        /// </summary>
        private string GetDamageSourceName(GameObject attacker, EntityDataLoader.EntityType attackerType)
        {
            string entityName = attacker != null ? attacker.name : "Unknown";

            switch (attackerType)
            {
                case EntityDataLoader.EntityType.Player:
                    return $"Player Attack ({entityName})";
                case EntityDataLoader.EntityType.Enemy:
                    return $"Enemy Attack ({entityName})";
                case EntityDataLoader.EntityType.Target:
                    return $"Target Attack ({entityName})";
                default:
                    return $"Attack ({entityName})";
            }
        }

        /// <summary>
        /// 檢查攻擊範圍內的所有實體並造成傷害（通用方法）
        /// 使用統一的 entity registry 來訪問所有實體類型
        /// </summary>
        private void CheckEntitiesInAttackRange(Vector2 attackCenter, float rangeSqr, GameObject attacker, int damage, EntityDataLoader.EntityType attackerType, string damageSource)
        {
            // 使用統一的 entity registry 遍歷所有實體
            var entitiesList = new List<IEntity>(activeEntities);
            int hitCount = 0;

            foreach (var entity in entitiesList)
            {
                if (entity == null || entity.IsDead) continue;
                if (entity.gameObject == attacker) continue;

                float distSqr = ((Vector2)entity.Position - attackCenter).sqrMagnitude;
                if (distSqr <= rangeSqr)
                {
                    EntityDataLoader.EntityType targetType = GetEntityTypeFromIEntity(entity);
                    if (ShouldAttackTarget(attackerType, targetType))
                    {
                        entity.TakeDamage(damage, damageSource);
                        hitCount++;

                        if (showDebugInfo)
                        {
                            float dist = Mathf.Sqrt(distSqr);
                            Debug.Log($"[AttackSystem] Attack hit: {targetType} {entity.gameObject.name} at distance {dist:F2}, damage={damage}");
                        }
                    }
                    else if (showDebugInfo)
                    {
                        Debug.Log($"[AttackSystem] Attack skipped: {attackerType} cannot attack {targetType} (same type or invalid target)");
                    }
                }
            }

            if (showDebugInfo && hitCount == 0)
            {
                Debug.Log($"[AttackSystem] Attack hit nothing (attacker: {attackerType}, activeEntities: {entitiesList.Count})");
            }
        }

        /// <summary>
        /// 從 IEntity 獲取 EntityType（轉換 EntityManager.EntityType 為 EntityDataLoader.EntityType）
        /// </summary>
        private EntityDataLoader.EntityType GetEntityTypeFromIEntity(IEntity entity)
        {
            if (entity == null) return EntityDataLoader.EntityType.None;

            // IEntity.GetEntityType() 返回 EntityManager.EntityType
            // 轉換為 EntityDataLoader.EntityType（統一使用）
            var entityType = entity.GetEntityType();
            
            // 使用字符串比較來轉換（因為是不同的枚舉類型）
            string typeName = entityType.ToString();
            switch (typeName)
            {
                case "Player":
                    return EntityDataLoader.EntityType.Player;
                case "Enemy":
                    return EntityDataLoader.EntityType.Enemy;
                case "Target":
                    return EntityDataLoader.EntityType.Target;
                default:
                    return EntityDataLoader.EntityType.None;
            }
        }

        /// <summary>
        /// 判斷攻擊者是否應該攻擊目標（可以根據遊戲規則自定義）
        /// </summary>
        private bool ShouldAttackTarget(EntityDataLoader.EntityType attackerType, EntityDataLoader.EntityType targetType)
        {
            // 攻擊規則：
            // - Player 可以攻擊 Target 和 Enemy
            // - Enemy 可以攻擊 Player（不能攻擊 Target）
            // - Target 可以攻擊 Player（不能攻擊 Enemy）
            // - 不能攻擊同類型

            if (attackerType == targetType) return false;

            switch (attackerType)
            {
                case EntityDataLoader.EntityType.Player:
                    // Player 可以攻擊 Target 和 Enemy
                    return targetType == EntityDataLoader.EntityType.Target || targetType == EntityDataLoader.EntityType.Enemy;

                case EntityDataLoader.EntityType.Enemy:
                    // Enemy 可以攻擊 Player（不能攻擊 Target）
                    return targetType == EntityDataLoader.EntityType.Player;

                case EntityDataLoader.EntityType.Target:
                    // Target 可以攻擊 Player（不能攻擊 Enemy）
                    return targetType == EntityDataLoader.EntityType.Player;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 添加實體到註冊表
        /// </summary>
        public void AddEntity(IEntity entity)
        {
            if (entity != null)
            {
                activeEntities.Add(entity);
            }
        }

        /// <summary>
        /// 從註冊表移除實體
        /// </summary>
        public void RemoveEntity(IEntity entity)
        {
            if (entity != null)
            {
                activeEntities.Remove(entity);
            }
        }
    }
}

