using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Weakpoint Settings")]
    [SerializeField] private Transform weakpoint;
    [SerializeField] private float baseWeakpointRevealTime = 1f; // parry window fixed to 1 second
    [SerializeField] private float weakpointMoveSpeed = 2f;

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("Attack Settings")]
    [SerializeField] private Collider attackHitbox; // assign this collider in inspector (trigger)
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

    // Flash effect variables
    private Renderer enemyRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    // Public property to access Weakpoint script
    public Weakpoint WeakpointScript { get; private set; }

    private void Start()
    {
        currentHealth = maxHealth;

        if (weakpoint != null)
        {
            WeakpointScript = weakpoint.GetComponent<Weakpoint>();
            weakpoint.gameObject.SetActive(false);
        }

        if (attackHitbox != null)
            attackHitbox.enabled = false;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
            originalColor = enemyRenderer.material.color;

        attackTimer = attackCooldown;
    }

    void Update()
    {
        if (weakpointActive)
        {
            weakpointTimer -= Time.deltaTime;
            if (weakpointTimer <= 0)
            {
                HideWeakpoint();
            }
            else
            {
                MoveWeakpoint();
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

        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(directionToPlayer);

        Debug.Log("Enemy attacking!");

        // Notify player to open parry window
        PlayerCombat playerCombat = player.GetComponent<PlayerCombat>();
        if (playerCombat != null)
        {
            playerCombat.OnEnemyAttack();
        }

        if (WeakpointScript != null)
        {
            WeakpointScript.Show(Color.yellow); // Yellow when attack window opens
        }

        // Start flashing yellow to indicate parry window
        StartFlash(Color.yellow);

        if (attackHitbox != null)
            attackHitbox.enabled = true;

        yield return new WaitForSeconds(0.5f);

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

    public void TriggerParrySuccess()
    {
        parryChainCount++;

        if (WeakpointScript != null)
        {
            WeakpointScript.Show(Color.green); // Green on parry success
        }

        // Start flashing green on parry success
        StartFlash(Color.green);

        weakpointActive = true;
        weakpointTimer = baseWeakpointRevealTime;
    }

    public void ResetParryChain()
    {
        parryChainCount = 0;
    }

    private void HideWeakpoint()
    {
        weakpointActive = false;

        if (WeakpointScript != null)
        {
            WeakpointScript.Hide();
        }
    }

    private void MoveWeakpoint()
    {
        Vector3 randomOffset = new Vector3(Mathf.Sin(Time.time), 0, Mathf.Cos(Time.time)) * 0.5f;
        if (weakpoint != null)
            weakpoint.localPosition = Vector3.Lerp(weakpoint.localPosition, randomOffset, Time.deltaTime * weakpointMoveSpeed);
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
        float flashDuration = 1f; // Duration of parry window
        float t = 0f;
        bool toggle = false;

        while (t < flashDuration)
        {
            enemyRenderer.material.color = toggle ? flashColor : originalColor;
            toggle = !toggle;
            yield return new WaitForSeconds(0.2f);
            t += 0.2f;
        }

        // Reset color to original
        enemyRenderer.material.color = originalColor;
    }
}
