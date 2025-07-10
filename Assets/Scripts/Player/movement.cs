using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Movement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 10f;
    [SerializeField] private float airAcceleration = 20f;
    [SerializeField] private float groundAcceleration = 50f;
    [SerializeField] private float maxAirSpeed = 20f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float gravity = -24f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 30f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.5f;

    [Header("Hyper Dash")]
    [SerializeField] private float hyperDashSpeed = 60f;
    [SerializeField] private float hyperDashDuration = 0.2f;
    [SerializeField] private float hyperDashCooldown = 1f;
    [SerializeField] private KeyCode hyperDashKey = KeyCode.Q;

    [Header("Slide")]
    [SerializeField] private float slideSpeed = 20f;
    [SerializeField] private float slideDuration = 0.5f;

    [Header("Slam")]
    [SerializeField] private float slamForce = -50f;

    [Header("Wall")]
    [SerializeField] private float wallSlideSpeed = -2f;
    [SerializeField] private float wallJumpForce = 12f;
    [SerializeField] private float wallJumpSideForce = 8f;
    [SerializeField] private float wallCheckDistance = 1f;

    [Header("Buffering & Coyote")]
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float coyoteTime = 0.2f;

    [Header("Punch Boost")]
    [SerializeField] private float punchBoostForce = 25f;
    [SerializeField] private KeyCode punchKey = KeyCode.Mouse1;
    [SerializeField] private Transform cameraTransform;

    [Header("Parry Momentum")]
    [SerializeField] private float parryMomentumSpeed = 5f;
    [SerializeField] private float parryMomentumDuration = 0.5f;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 inputDir;

    private bool isSliding = false;
    private bool isDashing = false;
    private bool isHyperDashing = false;
    private bool isSlamming = false;
    private bool isWallSliding = false;

    private float dashTimer = 0f;
    private float hyperDashTimer = 0f;
    private float slideTimer = 0f;
    private float dashCooldownTimer = 0f;
    private float hyperDashCooldownTimer = 0f;

    private float jumpBufferCounter = 0f;
    private float coyoteTimeCounter = 0f;

    private Vector3 lastWallNormal;

    private int wallJumpCount = 0;
    private Vector3 lastWallJumpNormal = Vector3.zero;
    private const int maxWallJumpsPerWall = 2;

    private bool canPunch = true;

    private Vector3 parryMomentumVelocity = Vector3.zero;
    private float parryMomentumTimer = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        HandleInput();

        if (controller.isGrounded)
        {
            wallJumpCount = 0;
            lastWallJumpNormal = Vector3.zero;
            canPunch = true;
        }

        HandleHyperDash();
        HandleDash();
        HandleSlide();
        HandleSlam();
        HandleWallSlide();
        HandleJump();
        HandleParryMomentum();

        ApplyMovement();
    }

    void HandleInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        inputDir = (transform.right * x + transform.forward * z).normalized;

        if (controller.isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
            canPunch = true;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (controller.isGrounded && Input.GetKeyDown(KeyCode.LeftControl) && inputDir.magnitude > 0.1f)
        {
            isSliding = true;
            slideTimer = slideDuration;
        }

        if (!controller.isGrounded && Input.GetKeyDown(KeyCode.LeftControl) && !isSlamming && velocity.y < 0f)
        {
            isSlamming = true;
            velocity.y = slamForce;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0f)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }

        if (Input.GetKeyDown(hyperDashKey) && hyperDashCooldownTimer <= 0f)
        {
            isHyperDashing = true;
            hyperDashTimer = hyperDashDuration;
            hyperDashCooldownTimer = hyperDashCooldown;
        }

        if (Input.GetKeyDown(punchKey) && cameraTransform != null && canPunch)
        {
            velocity += (-cameraTransform.forward).normalized * punchBoostForce;
            canPunch = false;
        }
    }

    void HandleDash()
    {
        if (isDashing)
        {
            velocity = inputDir * dashSpeed;
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                isDashing = false;
        }
        else
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }

    void HandleHyperDash()
    {
        if (isHyperDashing)
        {
            velocity = transform.forward * hyperDashSpeed;
            hyperDashTimer -= Time.deltaTime;
            if (hyperDashTimer <= 0f)
                isHyperDashing = false;
        }
        else
        {
            hyperDashCooldownTimer -= Time.deltaTime;
        }
    }

    void HandleSlide()
    {
        if (isSliding)
        {
            velocity = inputDir * slideSpeed;
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f || Input.GetKeyUp(KeyCode.LeftControl))
                isSliding = false;
        }
    }

    void HandleSlam()
    {
        if (controller.isGrounded && isSlamming)
        {
            isSlamming = false;
            velocity.y = 0f;
        }
    }

    void HandleWallSlide()
    {
        isWallSliding = false;

        if (!controller.isGrounded && velocity.y < 0)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.right, out hit, wallCheckDistance))
                StartWallSlide(hit.normal);
            else if (Physics.Raycast(transform.position, -transform.right, out hit, wallCheckDistance))
                StartWallSlide(hit.normal);
        }
    }

    void StartWallSlide(Vector3 wallNormal)
    {
        isWallSliding = true;

        if (lastWallJumpNormal != wallNormal)
        {
            wallJumpCount = 0;
            lastWallJumpNormal = wallNormal;
        }

        lastWallNormal = wallNormal;
        velocity.y = Mathf.Max(velocity.y, wallSlideSpeed);

        if (Input.GetButtonDown("Jump") && wallJumpCount < maxWallJumpsPerWall)
        {
            velocity = -lastWallNormal * wallJumpSideForce;
            velocity.y = wallJumpForce;
            isWallSliding = false;
            wallJumpCount++;
        }
    }

    void HandleJump()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f && !isWallSliding)
        {
            velocity.y = jumpForce;
            jumpBufferCounter = 0f;
        }
    }

    void HandleParryMomentum()
    {
        if (parryMomentumTimer > 0)
        {
            parryMomentumTimer -= Time.deltaTime;
            velocity = parryMomentumVelocity * parryMomentumSpeed;
        }
        else
        {
            parryMomentumTimer = 0f;
            parryMomentumVelocity = Vector3.zero;
        }
    }

    void ApplyMovement()
    {
        if (parryMomentumTimer <= 0)
        {
            if (!isDashing && !isSliding && !isHyperDashing)
            {
                if (controller.isGrounded)
                {
                    Vector3 desiredVelocity = inputDir * walkSpeed;
                    velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, groundAcceleration * Time.deltaTime);
                    velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, groundAcceleration * Time.deltaTime);
                }
                else
                {
                    Vector3 desiredVelocity = inputDir * maxAirSpeed;
                    velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, airAcceleration * Time.deltaTime);
                    velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, airAcceleration * Time.deltaTime);
                }

                velocity.y += gravity * Time.deltaTime;
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;
    }
    public void StartParryMomentum(Vector3 direction)
    {
        parryMomentumVelocity = direction.normalized;
        parryMomentumTimer = parryMomentumDuration;

        isDashing = false;
        isHyperDashing = false;
        isSliding = false;
    }

    public bool IsParryMomentumActive()
    {
        return parryMomentumTimer > 0;
    }
}
