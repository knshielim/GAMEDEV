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
        if (currentTarget == null)
        {
            Unit targetCandidate = other.GetComponent<Unit>();

            if (targetCandidate != null && !targetCandidate.isDead)
            {
                if ((other.CompareTag("Enemy") || other.CompareTag("Tower")) && other.gameObject != gameObject)
                {
                    currentTarget = targetCandidate;
                    isAttacking = true;
                }
            }
        }
    }
}