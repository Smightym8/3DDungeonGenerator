using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Enemy
{
    public class EnemyMovement : MonoBehaviour
    {
        public NavMeshAgent agent;
        public Animator animator;

        public Transform player;
        public LayerMask groundLayer = Layer.GroundLayer;
        public LayerMask playerLayer = Layer.PlayerLayer;
        
        // Patroling
        public Vector3 walkPoint;
        public bool isWalkPointSet;
        public float walkPointRange = 20f;
        private float _timer = 10f;
        
        // Attacking
        public float timeBetweenAttack = 3f;
        public bool hasAlreadyAttacked;
        
        // States
        public float sightRange = 15f;
        public float attackRange = 1.5f;
        public bool isPlayerInSightRange, isPlayerInAttackRange;
        
        // Animations
        private static readonly int IsWalking = Animator.StringToHash("isWalking");
        private static readonly int IsAttacking = Animator.StringToHash("isAttacking");
        private static readonly int IsDead = Animator.StringToHash("isDead");
        private bool _isWalking;
        private bool _isDying;
        private bool _isAttacking;

        private void Awake()
        {
            player = GameObject.FindWithTag(Tag.Player).transform;
            agent = GetComponent<NavMeshAgent>();
            _timer = 10f;
        }

        private void Update()
        {
            // Check for sight and attack range
            var position = transform.position;
            var playerPosition = player.position;
            isPlayerInSightRange = Vector3.Distance(position, playerPosition) < sightRange;
            isPlayerInAttackRange = Vector3.Distance(position, playerPosition) < attackRange;
            
            // Switch to appropriate state
            if (!isPlayerInSightRange && !isPlayerInAttackRange && !_isDying)
            {
                Patroling();
            }
            else if (isPlayerInSightRange && !isPlayerInAttackRange && !_isDying)
            {
                ChasePlayer();
            } 
            else if (isPlayerInSightRange && isPlayerInAttackRange && !_isDying)
            {
                animator.SetBool(IsWalking, false);
                AttackPlayer();
            }
        }

        private void Patroling()
        {
            if (!isWalkPointSet)
            {
                SearchWalkPoint();
            }

            if (isWalkPointSet)
            {
                animator.SetBool(IsWalking, true);
                _isWalking = true;
                agent.SetDestination(walkPoint);
            }

            Vector3 distanceToWalkPoint = transform.position - walkPoint;
            
            // Walk point reached
            if (distanceToWalkPoint.magnitude < 1f)
            {
                _timer -= Time.deltaTime; // TODO: Randomize
                animator.SetBool(IsWalking, false);
                _isWalking = false;
                if(_timer <= 0) { isWalkPointSet = false;}
            }
        }
        
        private void SearchWalkPoint()
        {
            _timer = 10f;
            
            var randomZ = Random.Range(-walkPointRange, walkPointRange);
            var randomX = Random.Range(-walkPointRange, walkPointRange);

            var position = transform.position;
            walkPoint = new Vector3(position.x + randomX, position.y, position.z + randomZ);

            if (Physics.Raycast(walkPoint, -transform.up, groundLayer))
            {
                isWalkPointSet = true;
            }
        }
        
        private void ChasePlayer()
        { 
            animator.SetBool(IsWalking, true);
            agent.SetDestination(player.position);
        }
      
        private void AttackPlayer()
        {
            if (hasAlreadyAttacked) return;
            
            StartAttacking();
            hasAlreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttack);
        }

        private void ResetAttack()
        {
          hasAlreadyAttacked = false;
        }

        public void StartAttacking()
        {
            // Ensure that enemy is not moving and looking to player
            agent.SetDestination(transform.position);
            transform.LookAt(player);

            _isAttacking = true;
            animator.SetBool(IsAttacking, true);
        }

        public void Attack()
        {
            animator.SetBool(IsAttacking, false);

            // Check distance again, player could run away before attack starts
            float distance = Vector3.Distance(transform.position, player.position);
            
            if (distance < 2)
            {
                var health = FindObjectOfType<PlayerMovement>().GetComponentInChildren<Health>(); 
                health.maxHealth -= 10;
            }
        }

        public void Die()
        {
            _isDying = true;
            animator.SetBool(IsDead, true);
        }
    }    
}