using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public GameObject player;
    public Transform thirdPersonCamera;
    public Animator animator;
    public int walkingSpeed = 3;
    public int runningSpeed = 6;
    public float smoothTime = 0.1f;

    private int _currentSpeed;
    private Vector3 _moveDirection;
    private float _turnSmoothVelocity;
    private Rigidbody _rigidbody;
    
    private bool _isMoving;
    private bool _isRunning;
    private bool _isAttacking;
    
    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private static readonly int IsRunning = Animator.StringToHash("isRunning");
    private static readonly int IsAttacking = Animator.StringToHash("isAttacking");

    private void Awake()
    {
        _rigidbody = player.GetComponent<Rigidbody>();
        _currentSpeed = walkingSpeed;
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;

        // Move
        if ((direction.magnitude >= 0.1) && !_isAttacking)
        {
            _isMoving = true;
            _moveDirection = direction;
            animator.SetBool(IsWalking, true);
        }
        else
        {
            _isMoving = false;
            animator.SetBool(IsWalking, false);
        }
        
        
        // Run
        if (Input.GetKeyDown(KeyCode.LeftShift) && !_isAttacking)
        {
            _isRunning = true;
            animator.SetBool(IsRunning, true);
        }
        
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            _isRunning = false;
            animator.SetBool(IsRunning, false);
        }

        // Attack
        if (Input.GetKeyDown(KeyCode.Mouse0) && !_isMoving)
        {
            _isAttacking = true;
            animator.SetBool(IsAttacking, true);
        }
    }

    private void FixedUpdate()
    {
        if (_isMoving)
        {
            var targetAngle = Mathf.Atan2(_moveDirection.x, _moveDirection.z) * Mathf.Rad2Deg + thirdPersonCamera.eulerAngles.y;
            var angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, 
                targetAngle, 
                ref _turnSmoothVelocity, 
                smoothTime
            );
            
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            var moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            Vector3 currentPos = _rigidbody.position;
            _rigidbody.MovePosition(currentPos + moveDirection.normalized * (_currentSpeed * Time.deltaTime)); 
            //player.Move(moveDirection.normalized * (_currentSpeed * Time.deltaTime));
        }

        if (_isMoving && _isRunning)
        {
            _currentSpeed = runningSpeed;
        }

        if (!_isRunning)
        {
            _currentSpeed = walkingSpeed;
        }
    }

    private void Attack()
    {
        animator.SetBool(IsAttacking, false);
        _isAttacking = false;

        var healthsFound = FindObjectsOfType<Health>();

        foreach (var health in healthsFound)
        {
            if (!health.gameObject.tag.Equals(Tag.Enemy)) continue;
            
            float distance = Vector3.Distance(transform.position, health.gameObject.transform.position);
            
            if (distance < 2)
            {
                health.maxHealth -= 10;
                health.ActivateAttacking();
            }
        }
    }
}
