using UnityEngine;

public class SpriteFlipper : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    public bool isFacingRight = false; 
    // Set this to false in Inspector if you want the sprite to start facing LEFT

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError("❌ SpriteRenderer not found on " + gameObject.name);
            enabled = false;
            return;
        }

        // Apply initial facing direction
        spriteRenderer.flipX = !isFacingRight;
    }
    public void FlipSprite()
    {
        isFacingRight = !isFacingRight;
        spriteRenderer.flipX = !spriteRenderer.flipX;
    }
}
