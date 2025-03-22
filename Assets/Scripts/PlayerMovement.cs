using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Internal data")]
    public Rigidbody2D rb;
    private bool _isFacingRight = true;
    public Animator animator;
    public Transform respawnPoint;
    public CinemachineCamera playerCamera;
    
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float horizontalMovement;
    
    [Header("Jump")]
    public float jumpForce = 5f;
    public int maxJumps = 2;
    private int _jumpsRemaining;
    
    [Header("GroundCheck")]
    public Transform groundCheckPosition;
    public Vector2 groundCheckSize = new Vector2(.5f, .05f);
    public LayerMask groundLayer;
    private bool _isGrounded;
    
    [Header("WallCheck")]
    public Transform wallCheckPosition;
    public Vector2 wallCheckSize = new Vector2(.5f, .05f);
    public LayerMask wallLayer;

    [Header("Gravity")]
    public float baseGravity = 2f;
    public float maxFallSpeed = 18f;
    public float fallSpeedMultiplier = 2f;
    
    [Header("WallMovement")]
    public float wallSlideSpeed = 2f;
    private bool _isWallSliding;
    
    [Header("WallJumping")]
    private bool _isWallJumping;
    private float _wallJumpDirection;
    private const float WallJumpTime = .5f;
    private float _wallJumpTimer;
    public Vector2 wallJumpPower = new Vector2(5f, 10f);
    
    void Awake()
    {
        if (!IsOwner) return;

        playerCamera = GameObject.FindGameObjectWithTag("CinemachineCamera").GetComponent<CinemachineCamera>();
        playerCamera.Follow = transform;
        
        respawnPoint = GameObject.FindGameObjectWithTag("Respawn").transform;
        transform.position = respawnPoint.position;
    }
    
    void Update()
    {
        if (!IsOwner)
        {
            return;
        }
        
        GroundCheck();
        ProcessGravity();
        // ProcessWallSlide();
        // ProcessWallJump();
        // DoorCheck();
        
        if (!_isWallJumping)
        {
            rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, rb.linearVelocity.y);
            Flip();
        }
        
        animator.SetFloat("yVelocity", rb.linearVelocity.y);
        animator.SetFloat("magnitude", rb.linearVelocity.magnitude);
        animator.SetBool("isWallSliding", _isWallSliding);
    }

    #region Move and jump

    private void ProcessGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = baseGravity * fallSpeedMultiplier;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -maxFallSpeed));
        }
        else
        {
            rb.gravityScale = baseGravity;
        }
    }
    
    public void Move(InputAction.CallbackContext context)
    {
        horizontalMovement = context.ReadValue<Vector2>().x;
    }
    
    public void Jump(InputAction.CallbackContext context)
    {
        if (_jumpsRemaining > 0)
        {
            if (context.performed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                _jumpsRemaining --;
                animator.SetTrigger("jump");
            } 
            else if (context.canceled)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * .5f);
                _jumpsRemaining --;
                animator.SetTrigger("jump");
            }
        }
        
        // Wall Jump
        if (context.performed && _wallJumpTimer > 0f){
            _isWallJumping = true;
            rb.linearVelocity = new Vector2(_wallJumpDirection * wallJumpPower.x, wallJumpPower.y);
            _wallJumpTimer = 0;
            animator.SetTrigger("jump");

            if (transform.localScale.x != _wallJumpDirection)
            {
                _isFacingRight = !_isFacingRight;
                Vector3 ls = transform.localScale;
                ls.x *= -1f;
                transform.localScale = ls;
            }
            
            Invoke(nameof(CancelWallJump), WallJumpTime + .1f);
        }
    }
    
    private void GroundCheck()
    {
        if (Physics2D.OverlapBox(groundCheckPosition.position, groundCheckSize, 0, groundLayer))
        {
            _jumpsRemaining = maxJumps;
            _isGrounded = true;
        }
        else
        {
            _isGrounded = false;
        }
    }

    #endregion

    #region Wall slide and jump

    private bool WallCheck()
    {
        return Physics2D.OverlapBox(wallCheckPosition.position, wallCheckSize, 0, wallLayer);
    }

    private void ProcessWallSlide()
    {
        if (!_isGrounded && WallCheck() & horizontalMovement != 0)
        {
            _isWallSliding = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
        }
        else
        {
            _isWallSliding = false;
        }
    }

    private void ProcessWallJump()
    {
        if (_isWallSliding)
        {
            _isWallJumping = false;
            _wallJumpDirection = -transform.localScale.x;
            _wallJumpTimer = WallJumpTime;

            CancelInvoke(nameof(CancelWallJump));
        }
        else if (_wallJumpTimer > 0f)
        {
            _wallJumpTimer -= Time.deltaTime;
        }
    }
    
    private void CancelWallJump()
    {
        _isWallJumping = false;
    }

    #endregion

    private void Flip()
    {
        if (_isFacingRight && horizontalMovement < 0 || !_isFacingRight && horizontalMovement > 0)
        {
            _isFacingRight = !_isFacingRight;
            Quaternion newRotation = Quaternion.Euler(0f, _isFacingRight ? 0f : 180f, 0f);
            transform.rotation = newRotation;
        }
    }
    
    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(groundCheckPosition.position, groundCheckSize);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(wallCheckPosition.position, wallCheckSize);
    }
}
