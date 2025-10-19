using System;
using UnityEngine;

/// <summary>
/// WeaponHolder�]�[�j���^
/// �T�O���|���ƽƻs�Z���A�ä䴩 prefab / �w�s�b child �����p
/// </summary>
public class WeaponHolder : MonoBehaviour
{
    [Header("Weapon (Prefab)")]
    [SerializeField] private GameObject weaponPrefab;

    [Header("Behavior")]
    [SerializeField] private bool equipOnStart = true; // �@�}�l�O�_�۰ʸ˳� prefab

    [Header("Attack Settings")]
    [SerializeField] private float attackAngle = 30f;
    [SerializeField] private float attackDuration = 0.15f;

    // runtime reference (���n�ǦC�ơA�קK�h�� holder ���V�P�@ instance)
    private Weapon currentWeapon;

    // �O���O���� prefab �ΨӸ˳ơ]��K�קK���� Instantiate �P�@ prefab�^
    private GameObject equippedPrefab;

    // ����J�]�P�@�ɶ��h���I�s EquipFromPrefab�^
    private bool isEquipping = false;

    // attack state
    private bool isAttacking = false;
    private float attackEndTime = 0f;
    private float originalRotation = 0f;

    public Weapon CurrentWeapon => currentWeapon;
    public event Action<Vector2, float, GameObject> OnAttackPerformed;

    private void Start()
    {
        // �Y�����s��ɤw�� Weapon ��b�����󩳤U�]child�^�A�N���ĥΥ��A�קK�A Instantiate
        if (currentWeapon == null)
        {
            Weapon childWeapon = GetComponentInChildren<Weapon>();
            if (childWeapon != null && childWeapon.transform.parent == transform)
            {
                // �ϥβ{���� child instance �@�� currentWeapon
                SetWeapon(childWeapon);
                // �L�k���D���O�ѭ��� prefab ���͡A�ҥH�� equippedPrefab �]�� null�]��� runtime instance�^
                equippedPrefab = null;
                return;
            }
        }

        // �Y�]�w���Ұʮɸ˳� prefab�A�B�|���˳ƥ���Z���A�~ Instantiate
        if (equipOnStart && weaponPrefab != null && currentWeapon == null)
        {
            EquipFromPrefab(weaponPrefab);
        }
    }

    // �s�W Update ��k���ˬd�����ʵe�O�_����
    private void Update()
    {
        // �ˬd�����ʵe�O�_����
        if (isAttacking && Time.time >= attackEndTime)
        {
            ResetWeaponRotation();
        }
    }

    private void OnDisable()
    {
        // �i��G�� holder �Q���ήɤ��۰� destroy �Z���A���C���ݨD�M�w
        // �p�G�A�Ʊ氱�ήɧ�Z���@�_ disable�A�i�H uncomment�G
        // if (currentWeapon != null) currentWeapon.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // �Y�A�Ʊ�� holder �Q�P���ɤ]�P����Z���]�קK orphan�^�A�i�H�o�ˡG
        if (currentWeapon != null)
        {
            Destroy(currentWeapon.gameObject);
            currentWeapon = null;
            equippedPrefab = null;
        }
    }

    /// <summary>
    /// �]�w�Z���]�ǤJ�w�s�b�� Weapon ��ҡ^
    /// �w�]�|�P���ª���ҡF�Y�ϥΪ�����Ч令�^���ª���C
    /// </summary>
    public void SetWeapon(Weapon weapon)
    {
        if (weapon == null)
        {
            currentWeapon = null;
            equippedPrefab = null;
            return;
        }

        // �p�G�ǤJ�� weapon �w�g�O�� holder �� child�A�ӥB�N�O currentWeapon�A�N������^
        if (currentWeapon == weapon && weapon.transform.parent == transform)
        {
            return;
        }

        // �p�G�w����L�Z���A�����ξP���]�ھڻݨD�^
        if (currentWeapon != null && currentWeapon != weapon)
        {
            // �w�]�P���¹�ҡF�Y�A�� pooling�A�אּ�^��
            Destroy(currentWeapon.gameObject);
        }

        currentWeapon = weapon;

        // ��Z�����쥻 holder �U�]local transform reset�^
        currentWeapon.transform.SetParent(this.transform, worldPositionStays: false);
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;
        currentWeapon.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// �q prefab �˳ƪZ���]�w���B�|�קK���ƽƻs�^
    /// �Y�w�˳ƬۦP prefab�A�|�����^�ǲ{���Z���C
    /// </summary>
    public Weapon EquipFromPrefab(GameObject prefab)
    {
        if (prefab == null) return null;

        // �w�����b�i�檺�˳Ƭy�{ �� ������^�{���Z���]�� null�^
        if (isEquipping)
        {
            return currentWeapon;
        }

        // �p�G�w�g�˳ƥB�w���O�ѦP�@ prefab �ͦ��A�h���A Instantiate
        if (currentWeapon != null && equippedPrefab == prefab)
        {
            return currentWeapon;
        }

        // �p�G currentWeapon �s�b�� equippedPrefab ���P�A��ܭn���Z���G���M���ª�
        if (currentWeapon != null && equippedPrefab != prefab)
        {
            Destroy(currentWeapon.gameObject);
            currentWeapon = null;
            equippedPrefab = null;
        }

        isEquipping = true;
        try
        {
            // Instantiate �ç⥦������b�� holder �U
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

            // �O���O���� prefab �ͦ����A�קK���ƥͦ�
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
    /// ��s�Z���¦V
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
    /// ���է���
    /// </summary>
    public bool TryAttack(GameObject attacker)
    {
        if (currentWeapon == null || isAttacking) return false;

        Vector2 origin = transform.position;

        bool success = currentWeapon.TryPerformAttack(origin, attacker);

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
}