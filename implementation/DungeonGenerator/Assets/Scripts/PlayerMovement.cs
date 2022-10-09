using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform camera;
    public Animator animator;
    public float speed = 3f;
    public float smoothTime = 0.1f;

    private bool _isMoving;
    private bool _isRunning;
    private Vector3 _moveDirection;
    private float _turnSmoothVelocity;
    
    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private static readonly int IsRunning = Animator.StringToHash("isRunning");

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;

        if (direction.magnitude >= 0.1)
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

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            _isRunning = true;
            animator.SetBool(IsRunning, true);
        }
        
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            _isRunning = false;
            animator.SetBool(IsRunning, false);
        }
    }

    private void FixedUpdate()
    {
        if (_isMoving)
        {
            var targetAngle = Mathf.Atan2(_moveDirection.x, _moveDirection.z) * Mathf.Rad2Deg + camera.eulerAngles.y;
            var angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, 
                targetAngle, 
                ref _turnSmoothVelocity, 
                smoothTime
            );
            
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            var moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDirection.normalized * (speed * Time.deltaTime));
        }

        if (_isMoving && _isRunning)
        {
            speed *= 2;
        }

        if (!_isRunning)
        {
            speed /= 2;
        }
    }
}
