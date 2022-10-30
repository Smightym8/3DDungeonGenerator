using System;
using General;
using UnityEngine;

namespace Player
{
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
        private UIManager _uiManager;
        private GameObject _key;
    
        private bool _isMoving;
        private bool _isRunning;
        private bool _isInKeyRange;
        private bool _isPickupKeyPressed;
        private bool _isKeyInInventory;

        private static readonly int IsWalking = Animator.StringToHash("isWalking");
        private static readonly int IsRunning = Animator.StringToHash("isRunning");

        private void Awake()
        {
            _rigidbody = player.GetComponent<Rigidbody>();
            _currentSpeed = walkingSpeed;
            _uiManager = FindObjectOfType<UIManager>();
        }

        private void Start()
        {
            // Get key in start method because Awake method is called before the key exists
            _key = GameObject.FindGameObjectWithTag(Tag.Key);
        }

        private void Update()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;

            // Move
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
        
            // Run
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
            
            // Pick up key
            if (Input.GetKeyDown(KeyCode.E) && _isInKeyRange && !_isKeyInInventory)
            {
                _isPickupKeyPressed = true;
            }
            
            if (Input.GetKeyUp(KeyCode.E))
            {
                _isPickupKeyPressed = false;
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
            }

            if (_isMoving && _isRunning)
            {
                _currentSpeed = runningSpeed;
            }

            if (!_isRunning)
            {
                _currentSpeed = walkingSpeed;
            }

            if (_isPickupKeyPressed && _isInKeyRange && !_isKeyInInventory)
            {
                Destroy(_key, 0);
                _isKeyInInventory = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag.Equals(Tag.KeyTable) && !_isKeyInInventory)
            {
                _isInKeyRange = true;
                _uiManager.SetText("Press E to pick up the key");
                _uiManager.ToggleTextWindow(true); 
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.tag.Equals(Tag.KeyTable))
            {
                _isInKeyRange = false;
                FindObjectOfType<UIManager>().ToggleTextWindow(false);    
                _uiManager.SetText("");
            }
        }
    }
}
