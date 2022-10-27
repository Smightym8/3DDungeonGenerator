...
// Switch to appropriate state
if (!isPlayerInSightRange && !isPlayerInAttackRange && !_isAttacked && !_isDying)
{
    // Patroling
    animator.SetBool(IsAttacking, false);
    animator.SetBool(IsRunning, false);
    animator.SetBool(IsWalking, true);
    agent.speed = _walkingSpeed;
    Patroling();
}
else if (isPlayerInSightRange && !isPlayerInAttackRange && !_isAttacked && !_isDying)
{
    // Chase Player
    animator.SetBool(IsAttacking, false);
    animator.SetBool(IsWalking, false);
    animator.SetBool(IsRunning, true);
    agent.speed = _runningSpeed;
    ChasePlayer();
}
else if (isPlayerInSightRange && isPlayerInAttackRange && !_isAttacked && !_isDying)
{
    // Attack Player
    animator.SetBool(IsWalking, false);
    animator.SetBool(IsRunning, false);
    AttackPlayer();
}
...