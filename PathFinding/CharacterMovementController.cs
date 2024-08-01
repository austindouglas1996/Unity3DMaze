using System.Threading.Tasks;
using UnityEngine;

public abstract class CharacterMovementController : CellMovementController
{
    [Tooltip("Speed at which the character walks.")]
    [SerializeField] protected float WalkSpeed;

    [Tooltip("Speed at which the character runs.")]
    [SerializeField] protected float RunSpeed;

    [Tooltip("Speed at which the character rotates.")]
    [SerializeField] protected float RotateSpeed;

    [Tooltip("Force applied to the character when jumping.")]
    [SerializeField] protected float JumpForce;

    [Tooltip("Gravity applied to the character.")]
    [SerializeField] protected float Gravity;

    [Tooltip("Scale factor for custom gravity when jumping.")]
    [SerializeField] protected float CustomGravityScale = 0.5f;

    [Tooltip("Flag to start the jump.")]
    [SerializeField] protected bool StartJump = false;

    [Header("Optional")]
    [Tooltip("Animator component for the character.")]
    [SerializeField] private Animator Animator;

    [Tooltip("Character controller to help with controlling gravity.")]
    [SerializeField] private CharacterController Controller;

    /// <summary>
    /// Direction the character is moving in.
    /// </summary>
    private Vector3 moveDirection;

    /// <summary>
    /// Jumping variables.
    /// </summary>
    public float JumpTime = 1f;
    protected bool IsJumping = false;
    protected float JumpTimeRemaining = 0; 

    /// <summary>
    /// Returns whether this character is running.
    /// </summary>
    public bool IsRunning = false;

    protected override async Task Start()
    {
        await base.Start();
    }

    protected override async Task Update()
    {
        if (StartJump)
        {
            StartJumping();
        }

        if (IsJumping)
        {
            HandleJumping();
        }
        else
        {
            ApplyGravity();
        }

        await base.Update();
    }

    public void StartJumping()
    {
        IsJumping = true;
        JumpTimeRemaining = JumpTime;
        StartJump = false;
        moveDirection.y = JumpForce;
    }

    /// <summary>
    /// Update the character movement.
    /// </summary>
    /// <param name="currentCell"></param>
    /// <param name="destinationCell"></param>
    /// <returns></returns>
    protected override bool UpdateMovement(Cell currentCell, Cell destinationCell)
    {
        Vector3Int A = transform.position.RoundToInt();
        Vector3Int B = destinationCell.Position;

        float distance = Vector3.Distance(A,B);
        Vector3 direction = (B - A);

        if (distance < 2f)
        {
            //transform.LookAt(new Vector3(B.x, A.y, B.z));
            return true;
        }

        // We make sure to set Y to zero here, so we use the 'jump' y.
        //direction.y = 0;

        Vector3 movement = direction * (IsRunning ? RunSpeed : WalkSpeed) * Time.deltaTime;
        movement.y += moveDirection.y * Time.deltaTime;

        MoveCharacter(movement);

        return false;
    }

    /// <summary>
    /// Move the character position.
    /// </summary>
    /// <param name="change"></param>
    protected virtual void MoveCharacter(Vector3 change)
    {
        if (this.Controller != null)
        {
            this.Controller.Move(change);
            this.UpdateAnimator(true);
        }
        else
        {
            this.transform.position += change;
        }
    }

    /// <summary>
    /// Handle jumping logic. Like, modify the gravity and Y position until we are out of jumps.
    /// </summary>
    private void HandleJumping()
    {
        if (JumpTimeRemaining > 0)
        {
            JumpTimeRemaining -= Time.deltaTime;
            moveDirection.y -= Gravity * CustomGravityScale * Time.deltaTime;
        }
        else
        {
            IsJumping = false;
            moveDirection.y = 0;
        }
    }

    /// <summary>
    /// Apply gravity on the character. Pushing them down.
    /// </summary>
    private void ApplyGravity()
    {
        if (this.Controller != null)
            moveDirection.y -= Gravity * CustomGravityScale * Time.deltaTime;
    }

    /// <summary>
    /// Update the <see cref="Animator"/>.
    /// </summary>
    /// <param name="moving"></param>
    private void UpdateAnimator(bool moving)
    {
        if (moving)
        {
            Animator.SetFloat("Vertical", 1.0f, 0.1f, Time.deltaTime);
            Animator.SetFloat("Horizontal", 0.0f, 0.1f, Time.deltaTime);
            Animator.SetFloat("WalkSpeed", 1);

            if (IsJumping)
            {
                Animator.SetFloat("WalkSpeed", 0);
            }
        }
        else
        {
            Animator.SetFloat("Vertical", 0.0f, 0.1f, Time.deltaTime);
            Animator.SetFloat("Horizontal", 0.0f, 0.1f, Time.deltaTime);
        }
    }
}