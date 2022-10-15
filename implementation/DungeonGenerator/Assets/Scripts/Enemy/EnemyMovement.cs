using UnityEngine;

namespace Enemy
{
    public class EnemyMovement : MonoBehaviour
    {
        public Animator animator;

        private static readonly int IsAttacking = Animator.StringToHash("isAttacking");
        
        public void StartAttacking()
        {
            animator.SetBool(IsAttacking, true);
        }

        public void Attack()
        {
            animator.SetBool(IsAttacking, false);

            FindObjectOfType<PlayerMovement>().GetComponentInChildren<Health>().maxHealth -= 10;
        }
    }    
}