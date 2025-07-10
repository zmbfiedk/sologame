using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifetime = 5f;

    private float lifeTimer;

    private void Start()
    {
        lifeTimer = lifetime;
    }

    private void Update()
    {
        // Move forward constantly
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // Destroy after lifetime expires
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if it hits an enemy
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        else if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
        {
            // Destroy on hitting any other collider except player (optional)
            Destroy(gameObject);
        }
    }
}
