# Event-Based Animation & Sound API Documentation

This document describes the event-based APIs for controlling animations and sound effects in the game. All state changes fire events automatically - no polling required.

---

## 🎯 Core Philosophy

**Use Events, Not Polling**
- ✅ Subscribe to events for state changes
- ❌ Don't poll with `IsMoving()`, `IsChasingPlayer()`, etc. (deprecated)

**Benefits:**
- Better performance (no Update() checks)
- Exact timing (events fire immediately when state changes)
- Cleaner code (declarative vs imperative)
- No missed states

---

## 📋 Event Categories

### 1. Enemy Events (`Enemy.cs`)

#### State Transition Events
```csharp
public event Action<EnemyState, EnemyState> OnStateChanged; // (oldState, newState)
public event Action OnStartedChasing;
public event Action OnStoppedChasing;
public event Action OnEnteredPatrol;
public event Action OnEnteredAlert;
public event Action OnEnteredSearch;
public event Action OnEnteredReturn;
```

#### Movement Events
```csharp
public event Action OnStartedMoving;
public event Action OnStoppedMoving;
public event Action<Vector2> OnMovementDirectionChanged; // (direction)
public event Action<float> OnSpeedChanged; // (speed)
```

#### Detection Events
```csharp
public event Action OnPlayerSpotted;
public event Action OnPlayerLost;
```

#### Health Events (inherited from EntityHealth)
```csharp
public event Action<int, int> OnHealthChanged; // (currentHealth, maxHealth)
public event Action<Enemy> OnEnemyDied;
```

---

### 2. Target Events (`Target.cs`)

#### State Transition Events
```csharp
public event Action<TargetState, TargetState> OnStateChanged; // (oldState, newState)
public event Action OnStartedEscaping;
public event Action OnStoppedEscaping;
```

#### Movement Events
```csharp
public event Action OnStartedMoving;
public event Action OnStoppedMoving;
public event Action<Vector2> OnMovementDirectionChanged; // (direction)
```

#### Escape Events
```csharp
public event Action OnPlayerSpotted;
public event Action<float> OnEscapeProgressChanged; // (progress 0-1)
public event Action<Target> OnTargetReachedEscapePoint;
```

#### Health Events (inherited from EntityHealth)
```csharp
public event Action<int, int> OnHealthChanged; // (currentHealth, maxHealth)
public event Action<Target> OnTargetDied;
```

---

### 3. Player Events (`Player.cs`)

#### Movement Events
```csharp
public event Action OnStartedMoving;
public event Action OnStoppedMoving;
public event Action OnStartedRunning;
public event Action OnStoppedRunning;
public event Action<Vector2> OnMovementDirectionChanged; // (direction)
public event Action<float> OnSpeedChanged; // (speed)
```

#### Equipment Events
```csharp
public event Action OnHandsEmpty;
public event Action OnWeaponEquipped;
public event Action OnItemEquipped;
```

#### Health Events (inherited from EntityHealth)
```csharp
public event Action<int, int> OnHealthChanged; // (currentHealth, maxHealth)
public event Action OnPlayerDied;
```

---

### 4. Weapon Events (`Weapon.cs`)

#### Attack Events
```csharp
public event Action<GameObject> OnAttackPerformed; // (attacker)
```

#### Equipment Events
```csharp
public event Action OnEquipped;
public event Action OnUnequipped;
public event Action OnBecameReady; // When equip delay finishes
```

#### Durability Events
```csharp
public event Action<int, int> OnDurabilityChanged; // (current, max)
public event Action OnWeaponBroken;
```

---

### 5. Ranged Weapon Events (`RangedWeapon.cs`)

#### Ammo Events
```csharp
public event Action<int, int> OnAmmoChanged; // (current, max)
public event Action OnAmmoEmpty;
public event Action OnReloadStarted;
public event Action OnReloadCompleted;
```

---

## 💡 Usage Examples

### Example 1: Enemy Chase Animation

```csharp
using UnityEngine;

public class EnemyAnimationController : MonoBehaviour
{
    [SerializeField] private Enemy enemy;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform weaponTransform;
    
    [Header("Weapon Positions")]
    [SerializeField] private Vector3 loweredPosition = new Vector3(0.5f, -0.2f, 0);
    [SerializeField] private Vector3 raisedPosition = new Vector3(0.5f, 0.1f, 0);
    
    private void Start()
    {
        // Subscribe to events
        enemy.OnStartedChasing += OnStartedChasing;
        enemy.OnStoppedChasing += OnStoppedChasing;
        enemy.OnStartedMoving += () => animator.SetBool("IsWalking", true);
        enemy.OnStoppedMoving += () => animator.SetBool("IsWalking", false);
    }
    
    private void OnStartedChasing()
    {
        // Raise weapon
        weaponTransform.localPosition = raisedPosition;
        animator.SetBool("IsChasing", true);
    }
    
    private void OnStoppedChasing()
    {
        // Lower weapon
        weaponTransform.localPosition = loweredPosition;
        animator.SetBool("IsChasing", false);
    }
    
    private void OnDestroy()
    {
        // Always unsubscribe
        if (enemy != null)
        {
            enemy.OnStartedChasing -= OnStartedChasing;
            enemy.OnStoppedChasing -= OnStoppedChasing;
        }
    }
}
```

---

### Example 2: Hit Reactions & Death

```csharp
using UnityEngine;

public class EntityCombatEffects : MonoBehaviour
{
    [SerializeField] private Enemy enemy;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Sounds")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;
    
    [Header("Particles")]
    [SerializeField] private ParticleSystem hitParticles;
    [SerializeField] private ParticleSystem deathParticles;
    
    private int previousHealth;
    
    private void Start()
    {
        previousHealth = enemy.CurrentHealth;
        
        // Subscribe to events
        enemy.OnHealthChanged += OnHealthChanged;
        enemy.OnEnemyDied += OnEnemyDied;
        enemy.OnPlayerSpotted += () => PlayExclamationMark();
    }
    
    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        // Detect damage
        if (currentHealth < previousHealth)
        {
            // Play hit animation
            animator.SetTrigger("TakeHit");
            
            // Play hit sound
            audioSource.PlayOneShot(hitSound);
            
            // Show hit particles
            if (hitParticles != null)
                hitParticles.Play();
        }
        
        previousHealth = currentHealth;
    }
    
    private void OnEnemyDied(Enemy deadEnemy)
    {
        // Play death animation
        animator.SetTrigger("Die");
        
        // Play death sound
        audioSource.PlayOneShot(deathSound);
        
        // Spawn death particles
        if (deathParticles != null)
            Instantiate(deathParticles, transform.position, Quaternion.identity);
    }
    
    private void PlayExclamationMark()
    {
        // Show alert icon
        Debug.Log("Enemy spotted player!");
    }
    
    private void OnDestroy()
    {
        if (enemy != null)
        {
            enemy.OnHealthChanged -= OnHealthChanged;
            enemy.OnEnemyDied -= OnEnemyDied;
        }
    }
}
```

---

### Example 3: Weapon Attack Effects

```csharp
using UnityEngine;

public class WeaponEffects : MonoBehaviour
{
    [SerializeField] private Enemy enemy;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Sounds")]
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip emptyClickSound;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem muzzleFlash;
    
    private Weapon currentWeapon;
    
    private void Update()
    {
        // Track weapon changes
        if (enemy.ItemHolder != null)
        {
            Weapon newWeapon = enemy.ItemHolder.CurrentWeapon;
            if (newWeapon != currentWeapon)
            {
                UnsubscribeFromWeapon(currentWeapon);
                SubscribeToWeapon(newWeapon);
                currentWeapon = newWeapon;
            }
        }
    }
    
    private void SubscribeToWeapon(Weapon weapon)
    {
        if (weapon == null) return;
        
        weapon.OnAttackPerformed += OnAttack;
        weapon.OnEquipped += () => PlayEquipSound();
        
        if (weapon is RangedWeapon ranged)
        {
            ranged.OnAmmoEmpty += () => audioSource.PlayOneShot(emptyClickSound);
            ranged.OnReloadStarted += () => audioSource.PlayOneShot(reloadSound);
        }
    }
    
    private void UnsubscribeFromWeapon(Weapon weapon)
    {
        if (weapon == null) return;
        
        weapon.OnAttackPerformed -= OnAttack;
        
        if (weapon is RangedWeapon ranged)
        {
            ranged.OnAmmoEmpty -= null;
            ranged.OnReloadStarted -= null;
        }
    }
    
    private void OnAttack(GameObject attacker)
    {
        // Play attack sound
        audioSource.PlayOneShot(attackSound);
        
        // Show muzzle flash for guns
        if (currentWeapon is RangedWeapon && muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
    }
    
    private void PlayEquipSound()
    {
        // Play weapon equip sound
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromWeapon(currentWeapon);
    }
}
```

---

### Example 4: Target Escape Progress

```csharp
using UnityEngine;
using UnityEngine.UI;

public class TargetEscapeUI : MonoBehaviour
{
    [SerializeField] private Target target;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Animator animator;
    
    private void Start()
    {
        // Subscribe to events
        target.OnStartedEscaping += OnStartedEscaping;
        target.OnEscapeProgressChanged += OnProgressChanged;
        target.OnTargetReachedEscapePoint += OnReachedEscapePoint;
        
        progressBar.gameObject.SetActive(false);
    }
    
    private void OnStartedEscaping()
    {
        // Show progress bar
        progressBar.gameObject.SetActive(true);
        
        // Play panic animation
        animator.SetBool("IsPanicking", true);
    }
    
    private void OnProgressChanged(float progress)
    {
        // Update progress bar
        progressBar.value = progress;
        
        // Increase panic intensity
        animator.SetFloat("PanicLevel", progress);
    }
    
    private void OnReachedEscapePoint(Target escapedTarget)
    {
        // Hide progress bar
        progressBar.gameObject.SetActive(false);
        
        Debug.Log("Target escaped!");
    }
    
    private void OnDestroy()
    {
        if (target != null)
        {
            target.OnStartedEscaping -= OnStartedEscaping;
            target.OnEscapeProgressChanged -= OnProgressChanged;
            target.OnTargetReachedEscapePoint -= OnReachedEscapePoint;
        }
    }
}
```

---

### Example 5: Player Movement & Running

```csharp
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource footstepSource;
    
    [Header("Footstep Settings")]
    [SerializeField] private AudioClip[] walkSounds;
    [SerializeField] private AudioClip[] runSounds;
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float runStepInterval = 0.3f;
    
    private float lastStepTime;
    private bool isRunning = false;
    
    private void Start()
    {
        // Subscribe to movement events
        player.OnStartedMoving += () => animator.SetBool("IsWalking", true);
        player.OnStoppedMoving += () => animator.SetBool("IsWalking", false);
        player.OnStartedRunning += OnStartedRunning;
        player.OnStoppedRunning += OnStoppedRunning;
        player.OnMovementDirectionChanged += OnDirectionChanged;
        player.OnSpeedChanged += speed => animator.SetFloat("Speed", speed);
    }
    
    private void Update()
    {
        // Play footstep sounds
        if (animator.GetBool("IsWalking"))
        {
            float interval = isRunning ? runStepInterval : walkStepInterval;
            if (Time.time - lastStepTime >= interval)
            {
                PlayFootstep();
                lastStepTime = Time.time;
            }
        }
    }
    
    private void OnStartedRunning()
    {
        isRunning = true;
        animator.SetBool("IsRunning", true);
    }
    
    private void OnStoppedRunning()
    {
        isRunning = false;
        animator.SetBool("IsRunning", false);
    }
    
    private void OnDirectionChanged(Vector2 direction)
    {
        // Flip sprite based on direction
        if (direction.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (direction.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }
    
    private void PlayFootstep()
    {
        AudioClip[] sounds = isRunning ? runSounds : walkSounds;
        if (sounds.Length > 0)
        {
            AudioClip clip = sounds[Random.Range(0, sounds.Length)];
            footstepSource.PlayOneShot(clip);
        }
    }
}
```

---

## ⚠️ Best Practices

### 1. Always Unsubscribe in OnDestroy()
```csharp
private void OnDestroy()
{
    if (enemy != null)
    {
        enemy.OnStartedChasing -= OnStartedChasing;
        enemy.OnHealthChanged -= OnHealthChanged;
    }
}
```

### 2. Check for Null Before Subscribing
```csharp
private void Start()
{
    if (enemy != null)
    {
        enemy.OnStartedChasing += OnStartedChasing;
    }
}
```

### 3. Use Lambda for Simple Handlers
```csharp
enemy.OnStartedMoving += () => animator.SetBool("IsWalking", true);
```

### 4. Track Weapon Changes in Update()
```csharp
private void Update()
{
    Weapon newWeapon = entity.ItemHolder?.CurrentWeapon;
    if (newWeapon != currentWeapon)
    {
        UnsubscribeFromWeapon(currentWeapon);
        SubscribeToWeapon(newWeapon);
        currentWeapon = newWeapon;
    }
}
```

### 5. Track Previous Values for Damage Detection
```csharp
private int previousHealth;

private void OnHealthChanged(int current, int max)
{
    if (current < previousHealth)
    {
        // Took damage
        int damageTaken = previousHealth - current;
        PlayHitEffect(damageTaken);
    }
    previousHealth = current;
}
```

---

## 📊 Complete Event Reference

### Enemy Events (13 total)
- `OnStateChanged` - State transitions
- `OnStartedChasing` / `OnStoppedChasing` - Chase state
- `OnEnteredPatrol` / `OnEnteredAlert` / `OnEnteredSearch` / `OnEnteredReturn` - State entries
- `OnStartedMoving` / `OnStoppedMoving` - Movement state
- `OnMovementDirectionChanged` / `OnSpeedChanged` - Movement changes
- `OnPlayerSpotted` / `OnPlayerLost` - Detection
- `OnHealthChanged` / `OnEnemyDied` - Health (inherited)

### Target Events (9 total)
- `OnStateChanged` - State transitions
- `OnStartedEscaping` / `OnStoppedEscaping` - Escape state
- `OnStartedMoving` / `OnStoppedMoving` - Movement state
- `OnMovementDirectionChanged` - Movement changes
- `OnPlayerSpotted` - Detection
- `OnEscapeProgressChanged` - Escape progress
- `OnHealthChanged` / `OnTargetDied` / `OnTargetReachedEscapePoint` - Health & goals

### Player Events (11 total)
- `OnStartedMoving` / `OnStoppedMoving` - Movement state
- `OnStartedRunning` / `OnStoppedRunning` - Sprint state
- `OnMovementDirectionChanged` / `OnSpeedChanged` - Movement changes
- `OnHandsEmpty` / `OnWeaponEquipped` / `OnItemEquipped` - Equipment
- `OnHealthChanged` / `OnPlayerDied` - Health (inherited)

### Weapon Events (7 total)
- `OnAttackPerformed` - Attack
- `OnEquipped` / `OnUnequipped` / `OnBecameReady` - Equipment
- `OnDurabilityChanged` / `OnWeaponBroken` - Durability
- `OnAmmoChanged` (RangedWeapon only)

### Ranged Weapon Events (4 total)
- `OnAmmoChanged` / `OnAmmoEmpty` - Ammo state
- `OnReloadStarted` / `OnReloadCompleted` - Reload

---

## 🎨 Recommended Animator Parameters

```csharp
// Bools
"IsWalking"
"IsRunning"
"IsChasing"
"IsAlert"
"IsDead"

// Floats
"Speed"
"HealthPercent"
"PanicLevel"

// Triggers
"TakeHit"
"Die"
"Attack"
```

---

## ✅ Testing Checklist

- [ ] Enemy raises weapon when chasing
- [ ] Enemy lowers weapon when patrolling
- [ ] Hit sound plays when taking damage
- [ ] Hit animation triggers on damage
- [ ] Death animation plays when dying
- [ ] Attack sound plays when attacking
- [ ] Muzzle flash shows for gun attacks
- [ ] Footsteps play when moving
- [ ] Sprint animation when running
- [ ] All events properly unsubscribed in OnDestroy()

---

**Version**: 2.0 (Event-Based)  
**Last Updated**: December 2025  
**Migration**: Polling methods deprecated, use events instead

