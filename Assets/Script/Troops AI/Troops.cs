using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Troops : Unit
{
    private Unit currentTarget;
    private List<Unit> targetsInRange = new List<Unit>();

    protected override void Start()
    {
        base.Start();
        UnitTeam = Team.Player;
    }

    protected override void Move()
    {
        // Remove dead targets
        targetsInRange.RemoveAll(t => t == null || t.isDead);

        if (targetsInRange.Count > 0)
        {
            // Pick the closest target
            currentTarget = targetsInRange
                .OrderBy(t => Vector2.Distance(transform.position, t.transform.position))
                .FirstOrDefault();

            if (currentTarget != null)
            {
                float distance = Vector2.Distance(transform.position, currentTarget.transform.position);

                if (distance <= 0.5f) // attack range
                {
                    isAttacking = true;
                    rb.velocity = Vector2.zero;
                    SetAnimationState(false, true);
                }
                else
                {
                    // Move toward target
                    Vector2 dir = (currentTarget.transform.position - transform.position).normalized;
                    transform.Translate(dir * moveSpeed * Time.deltaTime);
                    SetAnimationState(true, false);
                    isAttacking = false;
                }
            }

            return; // stop further movement
        }

        // No targets, move forward
        transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);
        SetAnimationState(true, false);
        isAttacking = false;
    }

    protected override void FindAndPerformAttack()
    {
        if (currentTarget == null || currentTarget.isDead)
        {
            // Pick closest target again
            currentTarget = targetsInRange
                .OrderBy(t => Vector2.Distance(transform.position, t.transform.position))
                .FirstOrDefault();

            if (currentTarget == null)
            {
                isAttacking = false;
                SetAnimationState(true, false);
                return;
            }
        }

        PerformAttack(currentTarget.GetComponent<Collider2D>());
    }

    protected override void PerformAttack(Collider2D targetCollider)
    {
        if (targetCollider == null) return;

        Unit targetUnit = targetCollider.GetComponent<Unit>();
        if (targetUnit != null && !targetUnit.isDead)
        {
            targetUnit.TakeDamage(attackPoints);
            Debug.Log($"[ATTACK] {gameObject.name} dealt {attackPoints} damage to {targetUnit.gameObject.name} " +
                      $"(HP: {targetUnit.CurrentHealth}/{targetUnit.MaxHealth})");

            if (targetUnit.CurrentHealth <= 0)
            {
                currentTarget = null;
                isAttacking = false;
                SetAnimationState(true, false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Unit target = other.GetComponent<Unit>();
        if (target == null || target.UnitTeam == UnitTeam || target.isDead) return;

        if (!targetsInRange.Contains(target))
            targetsInRange.Add(target);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Unit target = other.GetComponent<Unit>();
        if (target == null) return;

        targetsInRange.Remove(target);

        if (currentTarget == target)
        {
            currentTarget = null;
            isAttacking = false;
        }
    }
}
