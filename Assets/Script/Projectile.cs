using UnityEngine;

/// <summary>
/// Simple projectile used by ranged troops.
/// Moves in a given direction, damages the first enemy Unit it hits, then destroys itself.
/// </summary>
public class Projectile : MonoBehaviour
{
    private Vector2 _direction;
    private float _speed;
    private float _damage;
    private float _lifetime;
    private float _spawnTime;
    private Team _ownerTeam;

    [Tooltip("Optional Rigidbody2D for physics-based movement. If null, uses Transform.Translate.")]
    [SerializeField] private Rigidbody2D rb;

    public void Initialize(Vector2 direction, float damage, Team ownerTeam, float speed, float lifetime)
    {
        _direction = direction.normalized;
        _damage = damage;
        _ownerTeam = ownerTeam;
        _speed = speed;
        _lifetime = lifetime;
        _spawnTime = Time.time;

        // Orient the projectile visually towards its movement direction (optional)
        if (_direction.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    private void Update()
    {
        // Lifetime check
        if (Time.time - _spawnTime >= _lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Movement
        if (rb != null)
        {
            rb.velocity = _direction * _speed;
        }
        else
        {
            transform.Translate(_direction * (_speed * Time.deltaTime), Space.World);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Unit unit = other.GetComponent<Unit>();
        if (unit == null) return;

        // Ignore friendly units
        if (unit.UnitTeam == _ownerTeam) return;

        // Apply damage
        unit.TakeDamage(_damage);
        Debug.Log($"[PROJECTILE] {name} dealt {_damage} damage to {unit.gameObject.name}");

        Destroy(gameObject);
    }
}


