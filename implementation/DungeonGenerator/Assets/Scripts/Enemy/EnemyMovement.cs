using System;
using UnityEngine;

namespace Enemy
{
    public class EnemyMovement : MonoBehaviour
    {
        public Animator animator;

        private static readonly int IsAttacking = Animator.StringToHash("isAttacking");
        private static readonly int IsDead = Animator.StringToHash("isDead");
        
        public void StartAttacking()
        {
            animator.SetBool(IsAttacking, true);
        }

        public void Attack()
        {
            animator.SetBool(IsAttacking, false);

            var health = FindObjectOfType<PlayerMovement>().GetComponentInChildren<Health>(); 
            health.maxHealth -= 10;
        }

        public void Die()
        {
            animator.SetBool(IsDead, true);
        }

        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log($"Collision with {collision.gameObject.name}");
        }
    }    
}