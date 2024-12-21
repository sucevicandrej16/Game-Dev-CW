using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TwoDimensionalAnimationController : MonoBehaviour
{
   #region Animator Variables
    private Animator animator;
    private Vector2 moveInput;
    private int VelocityZHash;
    private int VelocityXHash;
    private int isJumpingHash;
    private int isRunningHash;
    private int isWalkingHash;
    private int isIdleHash;
    #endregion

    #region Player State Variables
    private bool isSprinting;
    private bool isWalking;
    private bool isIdle;
    private bool isJumping;
    private bool isRunning;

    #endregion

    #region Physics and Ground Detection
    public Transform groundCheck;
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool wasGrounded;
    private Vector2 jumpMovement;
    #endregion

    #region Input Actions
    private GameDevCW inputActions;
    bool forwardPressed = false;
    bool backwardPressed = false;
    bool leftPressed = false;
    bool rightPressed = false;
    #endregion

    #region Velocity Variables
    float velocityZ = 0.0f;
    float velocityX = 0.0f;
    #endregion

    #region Look Input and Rotation Variables
    private Vector2 lookInput;
    private float verticalRotation = 10f;
    private Vector2 smoothedLookInput;
    private Vector2 lookInputVelocity;
    private float currentRotationVelocity;
    private float targetRotation;
    private float lastTargetRotation;
    #endregion

    #region Movement Settings
    public float acceleration = 2f;
    public float deceleration = 2f;

    public float maxVelocityZ = 1.0f;
    public float maxVelocityX = 1.0f;
    public float sprintMaxVelocityZ = 2.0f;
    public float sprintMaxVelocityX = 2.0f;
    #endregion

    #region Camera Settings
    [Header("References")]
    [SerializeField] private Transform idleTransform;
    [SerializeField] private Transform cameraTransform;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float verticalRotationSpeed = 180f;
    [SerializeField] private float smoothRotationTime = 0.05f;
    [SerializeField] private float inputSmoothTime = 0.02f;
    [SerializeField] private float mouseSensitivity = 2.0f;
    #endregion


    private void Awake()
    {
        inputActions = new GameDevCW();

        // Subscribe to the Move action's performed and canceled events
        inputActions.Player.Move.performed += HandleMove;
        inputActions.Player.Move.canceled += HandleMove;

        inputActions.Player.Look.performed += HandleLook;
        inputActions.Player.Look.canceled += HandleLook;

        inputActions.Player.Jump.performed += HandleJump;
        inputActions.Player.Jump.canceled += HandleJump;

        // Subscribe to the Sprint action's performed and canceled events
        inputActions.Player.Sprint.performed += ctx => isSprinting = true;
        inputActions.Player.Sprint.canceled += ctx => isSprinting = false;
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        VelocityZHash = Animator.StringToHash("VelocityZ");
        VelocityXHash = Animator.StringToHash("VelocityX");
        isWalkingHash = Animator.StringToHash("isWalking");
        isIdleHash = Animator.StringToHash("isIdle");
        isRunningHash = Animator.StringToHash("isRunning");
        isJumpingHash = Animator.StringToHash("isJumping");

        if (idleTransform == null)
            idleTransform = transform;
                    
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleRotation();
        Movement(); // Update movement based on input and sprint state
        // UpdateAnimatorParameters();
        float speed = Mathf.Abs(rb.velocity.x);
        forwardPressed = moveInput.y > 0;
        leftPressed = moveInput.x < 0;
        rightPressed = moveInput.x > 0;
        backwardPressed = moveInput.y < 0;

        isWalking = forwardPressed || leftPressed || rightPressed || backwardPressed;
        isRunning = isGrounded && isSprinting;
        isIdle = !forwardPressed && !leftPressed && !rightPressed && !backwardPressed;

        Debug.Log($"walking {isWalking}");
        animator.SetBool(isIdleHash, isIdle);
        animator.SetBool(isWalkingHash, isWalking);
        animator.SetBool(isRunningHash, isRunning);
        animator.SetBool(isIdleHash, isIdle);

        if (isGrounded && !wasGrounded)
        {
            OnLand();
            Debug.Log("Player landed. Setting isJumping to false.");
        }
        else if (!isGrounded && wasGrounded)
        {
            Debug.Log("Player is now airborne.");
        }
        wasGrounded = isGrounded;
    }

    private void HandleJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            Debug.Log("Jump input detected");
            isJumping = true;
            Jump();
            animator.SetBool(isJumpingHash, isJumping);
        }
        else if (context.canceled)
        {
            // Don't set isJumping to false here - let OnLand handle that
            animator.SetBool(isJumpingHash, false);
            Debug.Log("Jump button released");
        }
    }

    private void Jump()
    {
        // Apply upward force to Rigidbody
        rb.AddForce(new Vector3(0, jumpMovement.y), ForceMode.Impulse);

        // Set isJumping to true
        Debug.Log($"Player jumped. Setting isJumping to {isJumping}");
    }

    private void OnLand()
    {
        if (!isGrounded)
            return;

        // Reset jumping state
        isJumping = false;
        Debug.Log($"Player landed. Setting isJumping to {isJumping}");
        animator.SetBool(isJumpingHash, false);
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    /// <summary>
    /// Updates the animation state based on player input and sprint state.
    /// </summary>

    private void UpdateAnimatorParameters(){
        
    }

    private int groundContacts = 0;

    private void OnCollisionStay(Collision collision)
    {
        bool foundValidGround = false;
        // Check if the collided object is on the ground layer
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.Log($"Contact Point: {contact.point}, Normal: {contact.normal}, Player Y: {transform.position.y}");
            if (contact.normal.y > 0.1f)  // Reduced threshold to be more forgiving
            {
                foundValidGround = true;
                groundContacts++;
                Debug.Log($"Ground detected! Contact count: {groundContacts}");
                break;  // Exit loop once we find valid ground
            }
        }
        
        if (foundValidGround)
        {
            isGrounded = true;
        }
        else
        {
            Debug.Log("No valid ground contact found.");
            groundContacts = 0;
            isGrounded = false;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        groundContacts = Mathf.Max(groundContacts - 1, 0);
        Debug.Log($"Collision exit - Remaining contacts: {groundContacts}");
        if (groundContacts == 0)
        {
            isGrounded = false;
            Debug.Log("No more ground contacts - Player is airborne");
        }
    }


    private void HandleRotation()
    {
        if (cameraTransform == null)
        {
            Debug.LogError("Camera transform is not assigned!");
            return;
        }

        // Apply sensitivity to input
        Vector2 sensitiveInput = lookInput * mouseSensitivity;

        // Smooth input
        smoothedLookInput = Vector2.Lerp(
            smoothedLookInput,
            sensitiveInput,
            1f - Mathf.Exp(-inputSmoothTime * 60f)
        );

        // Vertical rotation (POV camera)
        float mouseY = smoothedLookInput.y * verticalRotationSpeed * Time.deltaTime;
        verticalRotation -= mouseY;

        // Clamp vertical rotation to prevent looking too far up or down
        verticalRotation = Mathf.Clamp(verticalRotation, -30f, 60f); // Adjust values for POV

        // Apply vertical rotation to the camera
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // Horizontal rotation (player rotation)
        float mouseX = smoothedLookInput.x * rotationSpeed * Time.deltaTime;
        targetRotation += mouseX * 60f; // Normalize for framerate
        float currentRotationY = Mathf.SmoothDampAngle(
            idleTransform.eulerAngles.y,
            targetRotation,
            ref currentRotationVelocity,
            smoothRotationTime
        );

        // Apply horizontal rotation to the player
        idleTransform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);
    }

    private void Movement(){
        // Determine current maximum velocities based on sprinting
        float currentMaxVelocityZ = isRunning ? sprintMaxVelocityZ : maxVelocityZ;
        float currentMaxVelocityX = isRunning ? sprintMaxVelocityX : maxVelocityX;

        // Handle forward movement
        if (forwardPressed && velocityZ < currentMaxVelocityZ)
        {
            velocityZ += Time.deltaTime * acceleration;
            velocityZ = Mathf.Min(velocityZ, currentMaxVelocityZ);
        }
        else if (!forwardPressed && velocityZ > 0.0f)
        {
            velocityZ -= Time.deltaTime * deceleration;
            velocityZ = Mathf.Max(velocityZ, 0.0f);
        }

        // Left Movement
        if (leftPressed && velocityX > -currentMaxVelocityX)
        {
            velocityX -= Time.deltaTime * acceleration;
            velocityX = Mathf.Max(velocityX, -currentMaxVelocityX);
        }
        else if (!leftPressed && velocityX < 0.0f)
        {
            velocityX += Time.deltaTime * deceleration;
            velocityX = Mathf.Min(velocityX, 0.0f);
        }

        // Right Movement
        if (rightPressed && velocityX < currentMaxVelocityX)
        {
            velocityX += Time.deltaTime * acceleration;
            velocityX = Mathf.Min(velocityX, currentMaxVelocityX);
        }
        else if (!rightPressed && velocityX > 0.0f)
        {
            velocityX -= Time.deltaTime * deceleration;
            velocityX = Mathf.Max(velocityX, 0.0f);
        }

        // Backward Movement
        if (backwardPressed)
        {
            velocityZ -= Time.deltaTime * acceleration;
            velocityZ = Mathf.Max(velocityZ, -currentMaxVelocityZ);
        }
        else if (!backwardPressed && velocityZ < 0.0f)
        {
            velocityZ += Time.deltaTime * deceleration;
            velocityZ = Mathf.Min(velocityZ, 0.0f);
        }

        // Clamp small residual velocities to zero
        if (!leftPressed && !rightPressed && Mathf.Abs(velocityX) < 0.05f)
        {
            velocityX = 0.0f;
        }

        if (!forwardPressed && !backwardPressed && Mathf.Abs(velocityZ) < 0.05f)
        {
            velocityZ = 0.0f;
        }

        // Handle overshooting velocity limits
        if (forwardPressed && isRunning && velocityZ > currentMaxVelocityZ)
        {
            velocityZ = currentMaxVelocityZ;
        }
        else if (forwardPressed && velocityZ > currentMaxVelocityZ)
        {
            velocityZ -= Time.deltaTime * deceleration;
            if (velocityZ > currentMaxVelocityZ && velocityZ < (currentMaxVelocityZ + 0.05f))
            {
                velocityZ = currentMaxVelocityZ;
            }
        }
        else if (forwardPressed && velocityZ < currentMaxVelocityZ && velocityZ > (currentMaxVelocityZ - 0.05f))
        {
            velocityZ = currentMaxVelocityZ;
        }
        // Update the animator with the current velocities
        animator.SetFloat(VelocityZHash, velocityZ);
        animator.SetFloat(VelocityXHash, velocityX);
    }

    // public void OnLook(Vector2 input)
    // {
    //     lookInput = value.Get<Vector2>();
    // }   



    [SerializeField] private Rigidbody rb; // Rigidbody reference

    private void LateUpdate()
    {
        HandleRotation(); // Update camera and player rotation
    }

    private void FixedUpdate()
    {
        HandleMovement(); // Apply movement in FixedUpdate for physics consistency
    }

    private void HandleMovement()
    {
        // Ensure camera directions are updated
        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        // Calculate movement direction based on updated camera orientation
        Vector3 moveDirection = (cameraForward * velocityZ) + (cameraRight * velocityX);

        // Prevent unintended drift due to floating-point errors
        if (moveDirection.magnitude < 0.01f)
        {
            moveDirection = Vector3.zero;
        }

        // Apply movement to Rigidbody
        rb.MovePosition(rb.position + moveDirection * Time.fixedDeltaTime);

        // Debugging movement direction
        Debug.Log($"Move Direction: {moveDirection}, VelocityX: {velocityX}, VelocityZ: {velocityZ}");

        // Update the animator with the current velocities
        //animator.SetFloat(VelocityZHash, velocityZ);
        //animator.SetFloat(VelocityXHash, velocityX);
    }


    // Called when the Move action is performed or canceled
    private void HandleMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void HandleLook(InputAction.CallbackContext context){
        lookInput = context.ReadValue<Vector2>();
    }
}