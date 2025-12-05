using UnityEngine;

public class ProjectileEventHandler : MonoBehaviour
{
    public Troops troop; // drag your Troops component here

    public void FireProjectileEvent()
    {
        if (troop != null)
        {
            troop.FireProjectile();
            Debug.Log("Projectile Event Fired!");
        }
    }
}
