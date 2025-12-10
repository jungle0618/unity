# Attack & Damage Events - Animation Guide

## Quick Reference for Sound & Animation Events

This guide focuses on the events available for triggering sounds and animations when entities attack or take damage.

---

## 🎯 Primary Events for Animation/Sound

### 1. **OnHealthChanged** - Hit Reactions
**When to use**: Play hit animations, damage sounds, blood effects, damage numbers

```csharp
// Available on: Player, Enemy, Target
enemy.OnHealthChanged += (int currentHealth, int maxHealth) =>
{
    // Detect damage taken
    if (currentHealth < previousHealth)
    {
        int damageTaken = previousHealth - currentHealth;
        
        // Play hit animation
        animator.SetTrigger("TakeHit");
        
        // Play hurt sound
        audioSource.PlayOneShot(hurtSound);
        
        // Show damage number
        ShowDamageNumber(damageTaken);
    }
    previousHealth = currentHealth;
};
```

---

### 2. **OnAttackPerformed** - Attack Effects
**When to use**: Play attack animations, attack sounds, muzzle flash, weapon effects

```csharp
// Available on: All Weapon instances
Weapon weapon = enemy.ItemHolder.CurrentWeapon;
weapon.OnAttackPerformed += (GameObject attacker) =>
{
    // Play attack animation
    animator.SetTrigger("Attack");
    
    // Play attack sound
    audioSource.PlayOneShot(attackSound);
    
    // Show muzzle flash (for guns)
    if (weapon is RangedWeapon)
    {
        muzzleFlash.Play();
    }
    
    // Camera shake
    CameraShake(0.1f, 0.2f);
};
```

---

### 3. **OnEntityDied** - Death Effects
**When to use**: Play death animations, death sounds, particle effects, drop items

```csharp
// Available on: Player, Enemy, Target
enemy.OnEnemyDied += (Enemy deadEnemy) =>
{
    // Play death animation
    animator.SetTrigger("Die");
    
    // Play death sound
    audioSource.PlayOneShot(deathSound);
    
    // Spawn death particles
    Instantiate(deathParticles, transform.position, Quaternion.identity);
    
    // Drop loot
    DropLoot();
};
```

---

### 4. **OnWeaponBroken** - Weapon Break Effects
**When to use**: Play weapon break sounds, break particles, switch to empty hands

```csharp
// Available on: All Weapon instances
weapon.OnWeaponBroken += () =>
{
    // Play break sound
    audioSource.PlayOneShot(breakSound);
    
    // Spawn break particles
    Instantiate(breakParticles, weaponTransform.position, Quaternion.identity);
    
    // Notification
    ShowNotification("Weapon Broken!");
};
```

---

## 📋 Complete Event List

### Health Events

| Event | Parameters | Available On | Use For |
|-------|-----------|--------------|---------|
| `OnHealthChanged` | (int current, int max) | Player, Enemy, Target | Hit animations, hurt sounds |
| `OnEntityDied` | () | All entities (via EntityHealth) | Death animations, death sounds |
| `OnPlayerDied` | () | Player | Game over, respawn |
| `OnEnemyDied` | (Enemy enemy) | Enemy | Enemy death effects |
| `OnTargetDied` | (Target target) | Target | Mission completion |

### Weapon Events

| Event | Parameters | Available On | Use For |
|-------|-----------|--------------|---------|
| `OnAttackPerformed` | (GameObject attacker) | All Weapons | Attack animations, sounds |
| `OnDurabilityChanged` | (int current, int max) | All Weapons | Weapon condition visuals |
| `OnWeaponBroken` | () | All Weapons | Break effects, sounds |
| `OnAmmoChanged` | (int current, int max) | RangedWeapon | Reload sounds, UI updates |

---

## 🎬 Complete Animation Controller Example

This example shows a complete setup for handling all combat animations and sounds for an enemy.

```csharp
using UnityEngine;

public class EnemyCombatAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Enemy enemy;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip weaponBreakSound;
    
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem hitParticles;
    [SerializeField] private ParticleSystem deathParticles;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private ParticleSystem weaponBreakParticles;
    
    private int previousHealth;
    private Weapon currentWeapon;
    
    private void Start()
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        // Subscribe to health events
        SubscribeToHealthEvents();
        
        // Subscribe to current weapon events
        SubscribeToWeaponEvents();
        
        previousHealth = enemy.CurrentHealth;
    }
    
    private void Update()
    {
        // Check if weapon changed
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
    
    #region Health Event Handlers
    
    private void SubscribeToHealthEvents()
    {
        if (enemy == null) return;
        
        enemy.OnHealthChanged += OnHealthChanged;
        enemy.OnEnemyDied += OnEnemyDied;
    }
    
    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        // Check if took damage
        if (currentHealth < previousHealth)
        {
            int damageTaken = previousHealth - currentHealth;
            OnTakeDamage(damageTaken);
        }
        
        previousHealth = currentHealth;
    }
    
    private void OnTakeDamage(int damage)
    {
        // Play hit animation
        if (animator != null)
        {
            animator.SetTrigger("TakeHit");
        }
        
        // Play hit sound
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
        
        // Spawn hit particles
        if (hitParticles != null)
        {
            hitParticles.Play();
        }
        
        Debug.Log($"{enemy.name} took {damage} damage!");
    }
    
    private void OnEnemyDied(Enemy deadEnemy)
    {
        // Play death animation
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        
        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // Spawn death particles
        if (deathParticles != null)
        {
            Instantiate(deathParticles, transform.position, Quaternion.identity);
        }
        
        Debug.Log($"{deadEnemy.name} has died!");
    }
    
    #endregion
    
    #region Weapon Event Handlers
    
    private void SubscribeToWeaponEvents()
    {
        if (enemy.ItemHolder != null)
        {
            currentWeapon = enemy.ItemHolder.CurrentWeapon;
            SubscribeToWeapon(currentWeapon);
        }
    }
    
    private void SubscribeToWeapon(Weapon weapon)
    {
        if (weapon == null) return;
        
        weapon.OnAttackPerformed += OnWeaponAttack;
        weapon.OnWeaponBroken += OnWeaponBroken;
    }
    
    private void UnsubscribeFromWeapon(Weapon weapon)
    {
        if (weapon == null) return;
        
        weapon.OnAttackPerformed -= OnWeaponAttack;
        weapon.OnWeaponBroken -= OnWeaponBroken;
    }
    
    private void OnWeaponAttack(GameObject attacker)
    {
        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // Play attack sound
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
        
        // Show muzzle flash for guns
        if (currentWeapon is RangedWeapon && muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        
        Debug.Log($"{attacker.name} attacked with {currentWeapon.name}");
    }
    
    private void OnWeaponBroken()
    {
        // Play weapon break sound
        if (audioSource != null && weaponBreakSound != null)
        {
            audioSource.PlayOneShot(weaponBreakSound);
        }
        
        // Spawn break particles
        if (weaponBreakParticles != null && currentWeapon != null)
        {
            Instantiate(weaponBreakParticles, currentWeapon.transform.position, Quaternion.identity);
        }
        
        Debug.Log($"Weapon {currentWeapon?.name} has broken!");
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // Unsubscribe from all events
        if (enemy != null)
        {
            enemy.OnHealthChanged -= OnHealthChanged;
            enemy.OnEnemyDied -= OnEnemyDied;
        }
        
        UnsubscribeFromWeapon(currentWeapon);
    }
}
```

---

## 🎵 Audio Manager Integration Example

If you have a centralized audio manager, here's how to integrate with the events:

```csharp
using UnityEngine;

public class CombatAudioManager : MonoBehaviour
{
    public static CombatAudioManager Instance { get; private set; }
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private AudioClip[] deathSounds;
    [SerializeField] private AudioClip[] attackSounds;
    
    private AudioSource audioSource;
    
    private void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }
    
    // Call these from event handlers
    public void PlayHitSound(Vector3 position)
    {
        if (hitSounds.Length > 0)
        {
            AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
            AudioSource.PlayClipAtPoint(clip, position);
        }
    }
    
    public void PlayDeathSound(Vector3 position)
    {
        if (deathSounds.Length > 0)
        {
            AudioClip clip = deathSounds[Random.Range(0, deathSounds.Length)];
            AudioSource.PlayClipAtPoint(clip, position);
        }
    }
    
    public void PlayAttackSound(Vector3 position)
    {
        if (attackSounds.Length > 0)
        {
            AudioClip clip = attackSounds[Random.Range(0, attackSounds.Length)];
            AudioSource.PlayClipAtPoint(clip, position);
        }
    }
}

// Usage in event handler:
private void OnTakeDamage(int damage)
{
    CombatAudioManager.Instance.PlayHitSound(transform.position);
}
```

---

## ⚠️ Important Notes

### Event Subscription Best Practices

1. **Always Unsubscribe** in `OnDestroy()`:
   ```csharp
   private void OnDestroy()
   {
       if (enemy != null)
           enemy.OnHealthChanged -= OnHealthChanged;
   }
   ```

2. **Check for Null** before subscribing:
   ```csharp
   if (enemy != null && enemy.ItemHolder != null)
   {
       enemy.OnHealthChanged += OnHealthChanged;
   }
   ```

3. **Track Previous Values** for detecting changes:
   ```csharp
   private int previousHealth;
   
   private void OnHealthChanged(int current, int max)
   {
       if (current < previousHealth) // Took damage
       {
           // Handle damage
       }
       previousHealth = current;
   }
   ```

4. **Handle Weapon Changes** in Update():
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

---

## 🎨 Animator Parameter Recommendations

Suggested animator parameters for combat animations:

```csharp
// Triggers (one-shot animations)
animator.SetTrigger("Attack");     // Attack animation
animator.SetTrigger("TakeHit");    // Hit reaction
animator.SetTrigger("Die");        // Death animation

// Bools (continuous states)
animator.SetBool("IsDead", true);  // Death state
animator.SetBool("InCombat", enemy.IsInCombatMode());

// Floats (blending)
animator.SetFloat("HealthPercent", enemy.HealthPercentage);
animator.SetFloat("Speed", enemy.GetCurrentMovementSpeed());
```

---

## 📝 Testing Checklist

- [ ] Hit sound plays when enemy takes damage
- [ ] Hit animation triggers when taking damage
- [ ] Death sound plays when entity dies
- [ ] Death animation plays when entity dies
- [ ] Attack sound plays when weapon attacks
- [ ] Muzzle flash shows for gun attacks
- [ ] Weapon break sound plays when weapon breaks
- [ ] All event subscriptions are properly unsubscribed in OnDestroy()
- [ ] No null reference exceptions when events fire

---

## 📚 Additional Resources

For complete documentation, see:
- **ANIMATION_API_README.md** - Full API documentation with all examples
- **ANIMATION_API_QUICK_REFERENCE.md** - Quick reference guide
- **Enemy.cs** / **Player.cs** / **Target.cs** - Entity implementations
- **Weapon.cs** - Weapon event implementations

---

**Version**: 1.0  
**Last Updated**: December 2025  
**For**: Animation & Sound Implementation

