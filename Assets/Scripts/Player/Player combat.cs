using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private int attackDamage = 40;
    [SerializeField] private float attackRate = 1f;
    private float nextAttackTime = 0f;

    [Header("Parry Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 15f;
    [SerializeField] private float parryStaminaCost = 20f;
    [SerializeField] private float parryDuration = 0.5f;
    [SerializeField] private float parryCooldown = 2f;

    [Header("Parry Momentum")]
    [SerializeField] private float parryMomentumSpeed = 5f;
    [SerializeField] private float parryMomentumDuration = 0.5f;

    [Header("Parry Boost Zone")]
    [SerializeField] private bool inParryBoostZone = false;

    private float currentStamina;
    private bool isParrying = false;
    private float parryTimer = 0f;
    private float parryCooldownTimer = 0f;

    private bool parryBoostActive = false;

    private EnemyController enemy;

    private bool weakpointActive = false;
    private bool weakpointHittable = false;

    private Vector3 parryMomentumDirection = Vector3.zero;
    private float parryMomentumTimer = 0f;

    // Parry window vars
    private bool parryWindowActive = false;
    private float parryWindowDuration = 1f;
    private float parryWindowTimer = 0f;

    private void Start()
    {
        currentStamina = maxStamina;
        enemy = FindObjectOfType<EnemyController>();
    }

    private void Update()
    {
        HandleAttackInput();
        HandleParryInput();
        HandleParryTimers();
        RegenerateStamina();
        HandleParryMomentum();
        HandleParryWindowTimer();
    }

    private void HandleAttackInput()
    {
        if (Time.time >= nextAttackTime && Input.GetButtonDown("Fire1"))
        {
            Attack();
            nextAttackTime = Time.time + 1f / attackRate;
        }
    }

    private void Attack()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider enemyCollider in hitEnemies)
        {
            enemyCollider.GetComponent<EnemyController>()?.TakeDamage(attackDamage);
        }
    }

    // Called by enemy when it attacks the player - opens parry window
    public void OnEnemyAttack()
    {
        parryWindowActive = true;
        parryWindowTimer = parryWindowDuration;
        Debug.Log("Parry window opened");
    }

    private void HandleParryWindowTimer()
    {
        if (parryWindowActive)
        {
            parryWindowTimer -= Time.deltaTime;
            if (parryWindowTimer <= 0f)
            {
                parryWindowActive = false;
                Debug.Log("Parry window closed");
            }
        }
    }

    private void HandleParryInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && parryCooldownTimer <= 0f && currentStamina >= parryStaminaCost)
        {
            if (parryWindowActive)
            {
                StartParry();
                OnParryAttempt();
                parryWindowActive = false;
            }
            else
            {
                Debug.Log("No parry opportunity!");
            }
        }
    }

    private void StartParry()
    {
        isParrying = true;
        parryTimer = parryDuration;
        parryCooldownTimer = parryCooldown;

        currentStamina -= parryStaminaCost;

        if (inParryBoostZone)
        {
            parryTimer *= 1.5f;
            parryBoostActive = true;
            enemy?.SetParryBoost(true);
        }
        else
        {
            parryBoostActive = false;
            enemy?.SetParryBoost(false);
        }

        Debug.Log("Parry started");
    }

    private void HandleParryTimers()
    {
        if (isParrying)
        {
            parryTimer -= Time.deltaTime;

            if (parryTimer <= 0f)
            {
                isParrying = false;
                parryBoostActive = false;
                enemy?.SetParryBoost(false);
                Debug.Log("Parry ended");
            }
        }

        if (parryCooldownTimer > 0f)
        {
            parryCooldownTimer -= Time.deltaTime;
        }
    }

    private void RegenerateStamina()
    {
        if (!isParrying && currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }
    }

    private void HandleParryMomentum()
    {
        if (parryMomentumTimer > 0)
        {
            parryMomentumTimer -= Time.deltaTime;
            transform.position += parryMomentumDirection * parryMomentumSpeed * Time.deltaTime;
        }
        else
        {
            parryMomentumTimer = 0f;
            parryMomentumDirection = Vector3.zero;
        }
    }

    public void OnParryAttempt()
    {
        if (isParrying && enemy != null)
        {
            Debug.Log("Parry Success!");

            enemy.TriggerParrySuccess();

            StartParryMomentum(transform.forward);

            if (parryBoostActive)
            {
                weakpointActive = true;
                weakpointHittable = true;
            }
            else
            {
                weakpointActive = false;
                weakpointHittable = false;
            }
        }
        else
        {
            Debug.Log("Parry Failed: Not parrying or enemy null.");
            enemy?.ResetParryChain();
        }
    }

    public void OnWeakpointHit()
    {
        if (weakpointActive && weakpointHittable)
        {
            weakpointHittable = false;
            weakpointActive = false;

            if (enemy != null && enemy.WeakpointScript != null)
            {
                enemy.WeakpointScript.Hide();
            }

            Debug.Log("Weakpoint Hit! Bonus damage applied.");

            // Bonus damage when hitting weakpoint
            enemy.TakeDamage(attackDamage * 2);
        }
    }

    private void StartParryMomentum(Vector3 direction)
    {
        parryMomentumDirection = direction.normalized;
        parryMomentumTimer = parryMomentumDuration;
    }

    public void EnterParryBoostZone()
    {
        inParryBoostZone = true;
        Debug.Log("Entered Parry Boost Zone");
    }

    public void ExitParryBoostZone()
    {
        inParryBoostZone = false;
        parryBoostActive = false;
        enemy?.SetParryBoost(false);
        Debug.Log("Exited Parry Boost Zone");
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}