using UnityEngine;

/// <summary>
/// Handles projectile movement, damage, and collision detection.
/// Attach this to your projectile prefab.
/// </summary>
public class Projectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float damage;
    private Team ownerTeam;
    private float spawnTime;
    private float lifetime;

    [SerializeField] private Rigidbody2D rb;

    /// <summary>
    /// Initialize the projectile with all necessary data
    /// </summary>
    public void Initialize(Vector2 dir, float dmg, Team team, float spd, float life)
    {
        direction = dir.normalized;
        damage = dmg;
        ownerTeam = team;
        speed = spd;
        lifetime = life;
        spawnTime = Time.time;

        // Get Rigidbody2D if not assigned
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        // Set velocity immediately
        if (rb != null)
            rb.velocity = direction * speed;

        // Optional: Rotate projectile to face direction
        if (direction.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    private void Update()
    {
        // Auto-destroy after lifetime expires
        if (Time.time - spawnTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if it hit a unit
        Unit unit = other.GetComponent<Unit>();
        if (unit != null)
        {
            // Don't hit friendly units
            if (unit.UnitTeam == ownerTeam || unit.isDead)
                return;

            // Deal damage
            unit.TakeDamage(damage);
            Debug.Log($"[Projectile] Hit {unit.name} for {damage} damage");

            // Destroy projectile on hit
            Destroy(gameObject);
            return;
        }

        // Optional: Hit towers
        Tower tower = other.GetComponent<Tower>();
        if (tower != null)
        {
            // Check if it's an enemy tower
            bool isEnemyTower = (ownerTeam == Team.Player && tower.owner == Tower.TowerOwner.Enemy) ||
                                (ownerTeam == Team.Enemy && tower.owner == Tower.TowerOwner.Player);

            if (isEnemyTower)
            {
                tower.TakeDamage(Mathf.RoundToInt(damage));
                Debug.Log($"[Projectile] Hit tower {tower.name} for {damage} damage");
                Destroy(gameObject);
            }
        }
    }
}