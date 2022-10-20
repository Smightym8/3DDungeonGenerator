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

        // Patroling
        private Vector3 _walkPoint;
        private bool _isWalkPointSet;
        private readonly float walkPointRange = 20f;
        private float _timer = 10f;
        private float _walkingSpeed = 3f;
        
        // Chasing Player
        private float _runningSpeed = 10f;
        
        // Attacking
        public float timeBetweenAttack = 3f;
        public bool hasAlreadyAttacked;
        
        // States
        public float sightRange = 15f;
        public float attackRange = 1.5f;
        public bool isPlayerInSightRange, isPlayerInAttackRange;
        
        // Animations
        private static readonly int IsWalking = Animator.StringToHash("isWalking");
        private static readonly int IsRunning = Animator.StringToHash("isRunning");
        private static readonly int IsAttacking = Animator.StringToHash("isAttacking");
        private static readonly int IsAttacked = Animator.StringToHash("isAttacked");
        private static readonly int IsDead = Animator.StringToHash("isDead");
        private bool _isDying;
        private bool _isAttacked;

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
        }

        private void Patroling()
        {
            if (!_isWalkPointSet)
            {
                SearchWalkPoint();
            }

            if (_isWalkPointSet)
            {
                agent.SetDestination(_walkPoint);
            }

            Vector3 distanceToWalkPoint = transform.position - _walkPoint;
            
            // Walk point reached
            if (distanceToWalkPoint.magnitude < 1f)
            {
                _timer -= Time.deltaTime; // TODO: Randomize
                animator.SetBool(IsWalking, false);
                if(_timer <= 0) { _isWalkPointSet = false;}
            }
        }
        
        private void SearchWalkPoint()
        {
            _timer = 10f;
            
            var randomZ = Random.Range(-walkPointRange, walkPointRange);
            var randomX = Random.Range(-walkPointRange, walkPointRange);

            var position = transform.position;
            _walkPoint = new Vector3(position.x + randomX, position.y, position.z + randomZ);

            if (Physics.Raycast(_walkPoint, -transform.up, Layer.GroundLayer))
            {
                _isWalkPointSet = true;
            }
        }
        
        private void ChasePlayer()
        {
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

        public void GotHit()
        {
            animator.SetBool(IsAttacking, false);
            animator.SetBool(IsWalking, false);
            animator.SetBool(IsRunning, false);
            
            animator.SetBool(IsAttacked, true);
            _isAttacked = true;
            
            //StartAttacking();
        }

        private void StartAttacking()
        {
            // Ensure that enemy is not moving and looking to player
            agent.SetDestination(transform.position);
            transform.LookAt(player);
            
            animator.SetBool(IsAttacked, false);
            _isAttacked = false;
            
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
                // TODO: Enable damage again
                //health.maxHealth -= 10;
            }
        }

        public void Die()
        {
            _isDying = true;
            animator.SetBool(IsDead, true);
        }
    }    
}