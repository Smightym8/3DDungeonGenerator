...
// Attack
if (Input.GetKeyDown(KeyCode.Mouse0) && !_isMoving)
{
    _isAttacking = true;
    animator.SetBool(IsAttacking, true);
}
...