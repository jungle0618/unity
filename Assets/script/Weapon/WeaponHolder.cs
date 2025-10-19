using System;
using UnityEngine;

/// <summary>
/// WeaponHolder（加強版）
/// 確保不會重複複製武器，並支援 prefab / 已存在 child 的情況
/// </summary>
public class WeaponHolder : MonoBehaviour
{
    [Header("Weapon (Prefab)")]
    [SerializeField] private GameObject weaponPrefab;

    [Header("Behavior")]
    [SerializeField] private bool equipOnStart = true; // 一開始是否自動裝備 prefab

    [Header("Attack Settings")]
    [SerializeField] private float attackAngle = 30f;
    [SerializeField] private float attackDuration = 0.15f;

    // runtime reference (不要序列化，避免多個 holder 指向同一 instance)
    private Weapon currentWeapon;

    // 記錄是哪個 prefab 用來裝備（方便避免重複 Instantiate 同一 prefab）
    private GameObject equippedPrefab;

    // 防止重入（同一時間多次呼叫 EquipFromPrefab）
    private bool isEquipping = false;

    // attack state
    private bool isAttacking = false;
    private float attackEndTime = 0f;
    private float originalRotation = 0f;

    public Weapon CurrentWeapon => currentWeapon;
    public event Action<Vector2, float, GameObject> OnAttackPerformed;
    public event Action<int, int> OnWeaponDurabilityChanged; // 當前耐久度, 最大耐久度
    public event Action OnWeaponBroken; // 武器損壞事件

    private void Start()
    {
        // 若場景編輯時已把 Weapon 放在本物件底下（child），就先採用它，避免再 Instantiate
        if (currentWeapon == null)
        {
            Weapon childWeapon = GetComponentInChildren<Weapon>();
            if (childWeapon != null && childWeapon.transform.parent == transform)
            {
                // 使用現有的 child instance 作為 currentWeapon
                SetWeapon(childWeapon);
                // 無法知道它是由哪個 prefab 產生，所以把 equippedPrefab 設為 null（表示 runtime instance）
                equippedPrefab = null;
                return;
            }
        }

        // 若設定為啟動時裝備 prefab，且尚未裝備任何武器，才 Instantiate
        if (equipOnStart && weaponPrefab != null && currentWeapon == null)
        {
            EquipFromPrefab(weaponPrefab);
        }
    }

    // 新增 Update 方法來檢查攻擊動畫是否結束
    private void Update()
    {
        // 檢查攻擊動畫是否結束
        if (isAttacking && Time.time >= attackEndTime)
        {
            ResetWeaponRotation();
        }
    }

    private void OnDisable()
    {
        // 可選：當 holder 被停用時不自動 destroy 武器，視遊戲需求決定
        // 如果你希望停用時把武器一起 disable，可以 uncomment：
        // if (currentWeapon != null) currentWeapon.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // 若你希望當 holder 被銷毀時也銷毀其武器（避免 orphan），可以這樣：
        if (currentWeapon != null)
        {
            // 取消訂閱耐久度事件
            currentWeapon.OnDurabilityChanged -= OnWeaponDurabilityChangedHandler;
            currentWeapon.OnWeaponBroken -= OnWeaponBrokenHandler;
            
            Destroy(currentWeapon.gameObject);
            currentWeapon = null;
            equippedPrefab = null;
        }
    }

    /// <summary>
    /// 設定武器（傳入已存在的 Weapon 實例）
    /// 預設會銷毀舊的實例；若使用物件池請改成回收舊物件。
    /// </summary>
    public void SetWeapon(Weapon weapon)
    {
        if (weapon == null)
        {
            // 取消訂閱舊武器的耐久度事件
            if (currentWeapon != null)
            {
                currentWeapon.OnDurabilityChanged -= OnWeaponDurabilityChangedHandler;
                currentWeapon.OnWeaponBroken -= OnWeaponBrokenHandler;
            }
            
            currentWeapon = null;
            equippedPrefab = null;
            return;
        }

        // 如果傳入的 weapon 已經是本 holder 的 child，且就是 currentWeapon，就直接返回
        if (currentWeapon == weapon && weapon.transform.parent == transform)
        {
            return;
        }

        // 如果已有其他武器，移除或銷毀（根據需求）
        if (currentWeapon != null && currentWeapon != weapon)
        {
            // 取消訂閱舊武器的耐久度事件
            currentWeapon.OnDurabilityChanged -= OnWeaponDurabilityChangedHandler;
            currentWeapon.OnWeaponBroken -= OnWeaponBrokenHandler;
            
            // 預設銷毀舊實例；若你用 pooling，改為回收
            Destroy(currentWeapon.gameObject);
        }

        currentWeapon = weapon;

        // 訂閱新武器的耐久度事件
        currentWeapon.OnDurabilityChanged += OnWeaponDurabilityChangedHandler;
        currentWeapon.OnWeaponBroken += OnWeaponBrokenHandler;

        // 把武器掛到本 holder 下（local transform reset）
        currentWeapon.transform.SetParent(this.transform, worldPositionStays: false);
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;
        currentWeapon.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// 從 prefab 裝備武器（安全且會避免重複複製）
    /// 若已裝備相同 prefab，會直接回傳現有武器。
    /// </summary>
    public Weapon EquipFromPrefab(GameObject prefab)
    {
        if (prefab == null) return null;

        // 已有正在進行的裝備流程 → 直接回傳現有武器（或 null）
        if (isEquipping)
        {
            return currentWeapon;
        }

        // 如果已經裝備且已知是由同一 prefab 生成，則不再 Instantiate
        if (currentWeapon != null && equippedPrefab == prefab)
        {
            return currentWeapon;
        }

        // 如果 currentWeapon 存在但 equippedPrefab 不同，表示要換武器：先清除舊的
        if (currentWeapon != null && equippedPrefab != prefab)
        {
            Destroy(currentWeapon.gameObject);
            currentWeapon = null;
            equippedPrefab = null;
        }

        isEquipping = true;
        try
        {
            // Instantiate 並把它直接放在本 holder 下
            GameObject weaponGO = Instantiate(prefab, this.transform);
            weaponGO.transform.localPosition = Vector3.zero;
            weaponGO.transform.localRotation = Quaternion.identity;
            weaponGO.transform.localScale = Vector3.one;

            var weapon = weaponGO.GetComponent<Weapon>();
            if (weapon == null)
            {
                Debug.LogWarning($"EquipFromPrefab: prefab {prefab.name} does not contain a Weapon component.");
                Destroy(weaponGO);
                return null;
            }

            // 記錄是哪個 prefab 生成的，避免重複生成
            equippedPrefab = prefab;
            SetWeapon(weapon);
            return currentWeapon;
        }
        finally
        {
            isEquipping = false;
        }
    }

    /// <summary>
    /// 更新武器朝向
    /// </summary>
    public void UpdateWeaponDirection(Vector2 direction)
    {
        if (currentWeapon == null || direction.sqrMagnitude < 0.01f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        currentWeapon.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (!isAttacking)
        {
            originalRotation = angle;
        }
    }

    /// <summary>
    /// 嘗試攻擊
    /// </summary>
    public bool TryAttack(GameObject attacker)
    {
        if (currentWeapon == null || isAttacking) return false;

        Vector2 origin = transform.position;

        bool success = currentWeapon.TryPerformAttack(origin, attacker);
        Debug.Log("TryAttack: " + success);
        if (success)
        {
            TriggerAttackAnimation();
            OnAttackPerformed?.Invoke(origin, currentWeapon.AttackRange, attacker);
        }

        return success;
    }

    public bool CanAttack()
    {
        return currentWeapon != null && !isAttacking && currentWeapon.CanAttack();
    }

    private void TriggerAttackAnimation()
    {
        if (currentWeapon == null || isAttacking) return;

        isAttacking = true;
        attackEndTime = Time.time + attackDuration;

        float swingAngle = originalRotation + attackAngle;
        currentWeapon.transform.rotation = Quaternion.Euler(0f, 0f, swingAngle);
    }

    private void ResetWeaponRotation()
    {
        if (currentWeapon == null) return;

        isAttacking = false;
        currentWeapon.transform.rotation = Quaternion.Euler(0f, 0f, originalRotation);
    }

    public void StopAttackAnimation()
    {
        if (isAttacking)
        {
            ResetWeaponRotation();
        }
    }

    /// <summary>
    /// 處理武器耐久度變化事件
    /// </summary>
    private void OnWeaponDurabilityChangedHandler(int currentDurability, int maxDurability)
    {
        OnWeaponDurabilityChanged?.Invoke(currentDurability, maxDurability);
    }

    /// <summary>
    /// 處理武器損壞事件
    /// </summary>
    private void OnWeaponBrokenHandler()
    {
        OnWeaponBroken?.Invoke();
        Debug.Log($"武器 {currentWeapon.name} 已損壞！");
    }

    /// <summary>
    /// 修復當前武器的耐久度
    /// </summary>
    /// <param name="amount">修復的數量</param>
    public void RepairCurrentWeapon(int amount)
    {
        if (currentWeapon != null)
        {
            currentWeapon.RepairDurability(amount);
        }
    }

    /// <summary>
    /// 完全修復當前武器
    /// </summary>
    public void FullRepairCurrentWeapon()
    {
        if (currentWeapon != null)
        {
            currentWeapon.FullRepair();
        }
    }

    /// <summary>
    /// 獲取當前武器的耐久度信息
    /// </summary>
    /// <returns>耐久度信息 (當前耐久度, 最大耐久度, 耐久度百分比)</returns>
    public (int current, int max, float percentage) GetWeaponDurabilityInfo()
    {
        if (currentWeapon == null)
        {
            return (0, 0, 0f);
        }
        
        return (currentWeapon.CurrentDurability, currentWeapon.MaxDurability, currentWeapon.DurabilityPercentage);
    }
}