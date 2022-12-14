using Dungeon;
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
        private bool _isInDoorRange;
        private bool _isActionKeyPressed;
        private bool _isKeyInInventory;
        private bool _isQuit;
        private bool _isInstructionsReset;

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
            
            _uiManager.ToggleTextWindow(true);
            _uiManager.SetText("There are a door and a key somewhere in the dungeon.\n" +
                               "Find the key to open the door to enter the next level.");
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
            if (Input.GetKeyDown(KeyCode.E))
            {
                _isActionKeyPressed = true;
            }
            
            if (Input.GetKeyUp(KeyCode.E))
            {
                _isActionKeyPressed = false;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _isQuit = true;
            }
        }

        private void FixedUpdate()
        {
            if (_isMoving)
            {
                if (!_isInstructionsReset)
                {
                    _isInstructionsReset = true;
                    _uiManager.ToggleTextWindow(false);
                    _uiManager.SetText("");
                }
                
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

            if (_isActionKeyPressed && _isInKeyRange && !_isKeyInInventory)
            {
                Destroy(_key, 0);
                _isKeyInInventory = true;
                _uiManager.SetText("You picked up the key");
            }

            if (_isActionKeyPressed && _isInDoorRange && _isKeyInInventory)
            {
                // Reload Dungeon
                DungeonCreator.dungeonCreator.ResetDungeon();
                ResetPlayerState();
            }

            if (_isQuit)
            {
                Application.Quit();
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
            else if (other.tag.Equals(Tag.NextLevelDoor))
            {
                _isInDoorRange = true;
                _uiManager.SetText(!_isKeyInInventory
                    ? "You need the key to open the door"
                    : "Press E to enter the next level");

                _uiManager.ToggleTextWindow(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            _isInKeyRange = false;
            _isInDoorRange = false;
            _uiManager.ToggleTextWindow(false);    
            _uiManager.SetText("");
        }

        private void ResetPlayerState()
        {
            _isKeyInInventory = false;
            _isInKeyRange = false;
            _isInDoorRange = false;
            _uiManager.ToggleTextWindow(false);    
            _uiManager.SetText("");
            _key = GameObject.FindGameObjectWithTag(Tag.Key);
        }
    }
}
