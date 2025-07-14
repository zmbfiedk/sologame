using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Weakpoint Settings")]
    [SerializeField] private Weakpoint[] weakpoints;
    [SerializeField] private Transform[] weakpointPositions;
    [SerializeField] private float baseWeakpointRevealTime = 1f;
    [SerializeField] private float weakpointMoveSpeed = 2f;

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("Attack Settings")]
    [SerializeField] private Collider attackHitbox;
    [SerializeField] private float attackCooldown = 3f;
    [SerializeField] private int attackDamage = 20;

    private float attackTimer = 0f;
    private bool weakpointActive = false;
    private float weakpointTimer = 0f;

    [Header("Parry Chain")]
    private int parryChainCount = 0;

    [Header("Environmental Boosts")]
    private bool isInParryBoostZone = false;

    private Transform player;

    private Renderer enemyRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    private bool attackParried = false; // <-- Track if current attack was parried

    public Weakpoint[] Weakpoints => weakpoints;

    private void Start()
    {
        currentHealth = maxHealth;

        if (weakpoints != null)
        {
            foreach (var wp in weakpoints)
                wp.gameObject.SetActive(false);
        }

        if (attackHitbox != null)
            attackHitbox.enabled = false;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
            originalColor = enemyRenderer.material.color;

        attackTimer = attackCooldown;
    }

    private void Update()
    {
        if (weakpointActive)
        {
            weakpointTimer -= Time.deltaTime;
            if (weakpointTimer <= 0)
            {
                HideWeakpoints();
            }
            else
            {
                MoveWeakpoints();
            }
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            StartCoroutine(PerformAttack());
            attackTimer = attackCooldown;
        }
    }

    private IEnumerator PerformAttack()
    {
        if (player == null)
            yield break;

        attackParried = false; // Reset parry flag for new attack

        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(directionToPlayer);

        Debug.Log("Enemy attacking!");

        PlayerCombat playerCombat = player.GetComponent<PlayerCombat>();
        if (playerCombat != null)
            playerCombat.OnEnemyAttack();

        StartFlash(Color.yellow);

        if (attackHitbox != null)
            attackHitbox.enabled = true;

        yield return new WaitForSeconds(0.5f);

        if (!attackParried)  // Player failed to parry
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log("Player failed to parry and took damage!");
            }
        }
        else
        {
            Debug.Log("Player parried the attack!");
        }

        if (attackHitbox != null)
            attackHitbox.enabled = false;

        Debug.Log("Enemy attack ended");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (attackHitbox != null && attackHitbox.enabled && other.CompareTag("Player"))
        {
            Debug.Log("Player hit by enemy attack!");
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }

    // Call this method when player successfully parries
    public void NotifyParrySuccess()
    {
        attackParried = true;  // Mark attack as parried
        TriggerParrySuccess();
    }

    public void TriggerParrySuccess()
    {
        parryChainCount++;

        if (weakpoints != null && weakpoints.Length > 0)
        {
            int index = Random.Range(0, weakpoints.Length);

            // Move the weakpoint to a predefined position if set
            if (weakpointPositions != null && weakpointPositions.Length > 0)
            {
                Transform targetPos = weakpointPositions[index % weakpointPositions.Length];
                weakpoints[index].transform.position = targetPos.position;
            }

            weakpoints[index].Show(Color.green);
        }

        StartFlash(Color.green);

        weakpointActive = true;
        weakpointTimer = baseWeakpointRevealTime;
    }

    public void ResetParryChain()
    {
        parryChainCount = 0;
    }

    private void HideWeakpoints()
    {
        weakpointActive = false;

        if (weakpoints != null)
        {
            foreach (var wp in weakpoints)
                wp.Hide();
        }
    }

    private void MoveWeakpoints()
    {
        if (weakpoints == null || weakpointPositions == null || weakpointPositions.Length == 0)
            return;

        foreach (var wp in weakpoints)
        {
            int index = System.Array.IndexOf(weakpoints, wp);
            if (index >= 0 && index < weakpointPositions.Length)
            {
                wp.transform.position = Vector3.Lerp(
                    wp.transform.position,
                    weakpointPositions[index].position,
                    Time.deltaTime * weakpointMoveSpeed
                );
            }
        }
    }

    public void SetParryBoost(bool active)
    {
        isInParryBoostZone = active;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Enemy took {damage} damage, current health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Enemy died.");
        Destroy(gameObject);
    }

    private void StartFlash(Color flashColor)
    {
        if (enemyRenderer == null) return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashCoroutine(flashColor));
    }

    private IEnumerator FlashCoroutine(Color flashColor)
    {
        float flashDuration = 1f;
        float t = 0f;
        bool toggle = false;

        while (t < flashDuration)
        {
            enemyRenderer.material.color = toggle ? flashColor : originalColor;
            toggle = !toggle;
            yield return new WaitForSeconds(0.2f);
            t += 0.2f;
        }

        enemyRenderer.material.color = originalColor;
    }
}
