using UnityEngine;

public class Troops : Unit
{
    private Unit currentTarget;
    
    protected override void Start()
    {
        base.Start();
        UnitTeam = Team.Player; 
    }

    protected override void Move()
    {
        transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);
        SetAnimationState(true, false);
    }

    protected override void FindAndPerformAttack()
    {
        if (currentTarget == null || currentTarget.isDead) 
        {
            currentTarget = null;
            isAttacking = false;
            SetAnimationState(true, false);
            return;
        }
        
        PerformAttack(currentTarget.GetComponent<Collider2D>());
    }
    
    protected override void PerformAttack(Collider2D targetCollider)
    {
        Unit targetUnit = targetCollider.GetComponent<Unit>();
        
        if (targetUnit != null && !targetUnit.isDead)
        {
            targetUnit.TakeDamage(attackPoints);
            Debug.Log($"[ATTACK] {gameObject.name} dealt {attackPoints} damage to {targetUnit.gameObject.name} (HP: {targetUnit.CurrentHealth}/{targetUnit.MaxHealth})");

            if (targetUnit.CurrentHealth <= 0) 
            {
                currentTarget = null;
                isAttacking = false;
                SetAnimationState(true, false);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (currentTarget != null || isDead) return;
        
        Unit targetCandidate = other.GetComponent<Unit>();
        
        if (targetCandidate == null || targetCandidate.isDead) return;
        
        if (targetCandidate.UnitTeam == UnitTeam) return;
        
        if (other.CompareTag("Enemy") || other.CompareTag("Tower"))
        {
            currentTarget = targetCandidate;
            isAttacking = true;
            Debug.Log($"[COMBAT] {gameObject.name} acquired target: {other.gameObject.name}");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (isDead) return;
        
        if (currentTarget == null) return;
        
        Unit exitedUnit = other.GetComponent<Unit>();
        
        if (exitedUnit == null) return;
        
        if (exitedUnit == currentTarget)
        {
            if (exitedUnit.isDead)
            {
                Debug.Log($"[COMBAT] {gameObject.name} ignoring exit of dying target: {exitedUnit.gameObject.name}");
                return;
            }
            
            Debug.Log($"[COMBAT] {gameObject.name} lost target (moved away): {exitedUnit.gameObject.name}");
            currentTarget = null;
            isAttacking = false;
        }
    }
}