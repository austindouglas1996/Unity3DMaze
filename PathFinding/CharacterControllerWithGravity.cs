using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class CharacterControllerWithGravity : MonoBehaviour
{
    [Tooltip("Speed at which the character walks.")]
    [SerializeField] private float walkSpeed;

    [Tooltip("Speed at which the character runs.")]
    [SerializeField] private float runSpeed;

    [Tooltip("Speed at which the character rotates.")]
    [SerializeField] private float rotateSpeed;

    [Tooltip("Force applied to the character when jumping.")]
    [SerializeField] private float jumpForce;

    [Tooltip("Gravity applied to the character.")]
    [SerializeField] private float gravity;

    [Tooltip("Scale factor for custom gravity when jumping.")]
    [SerializeField] private float customGravityScale = 0.5f;

    [Tooltip("Animator component for the character.")]
    [SerializeField] private Animator animator;

    [Tooltip("Character controller to help with controlling gravity.")]
    [SerializeField] private CharacterController controller;

    [Tooltip("Flag to start the jump.")]
    [SerializeField] private bool StartJump = false;

    private Vector3Int MovingTo;
    private Vector3 moveDirection;
    private bool isGrounded;
    public float JumpTime = 1f;
    private bool IsMoving = false;
    private bool IsJumping = false;
    private float JumpTimeRemaining = 0;

    public void MoveTo(Vector3Int position)
    {
        this.MovingTo = position;
    }

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = controller.isGrounded;

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

        this.MoveCharacter();
        UpdateAnimator();
    }

    private void StartJumping()
    {
        IsJumping = true;
        JumpTimeRemaining = JumpTime;
        StartJump = false;
        moveDirection.y = jumpForce;
    }

    private void HandleJumping()
    {
        if (JumpTimeRemaining > 0)
        {
            JumpTimeRemaining -= Time.deltaTime;
            moveDirection.y -= gravity * customGravityScale * Time.deltaTime;
        }
        else
        {
            IsJumping = false;
            moveDirection.y = 0;
        }
    }

    private void ApplyGravity()
    {
        if (!isGrounded)
        {
            moveDirection.y -= gravity * customGravityScale * Time.deltaTime;
        }
    }

    private void MoveCharacter()
    {
        float distance = Vector3.Distance(transform.position.RoundToInt(), MovingTo);
        Vector3 direction = (this.MovingTo - transform.position.RoundToInt());
        direction.y = 0;

        Vector3 movement = direction * walkSpeed * Time.deltaTime;
        movement.y += moveDirection.y * Time.deltaTime;

        controller.Move(movement);

        if (distance > 1f)
        {
            transform.LookAt(new Vector3(MovingTo.x, transform.position.RoundToInt().y, MovingTo.z));
        }
    }

    private void UpdateAnimator()
    {
        float distance = Vector3.Distance(transform.position.RoundToInt(), MovingTo);

        if (distance > 1f)
        {
            animator.SetFloat("Vertical", 1.0f, 0.1f, Time.deltaTime);
            animator.SetFloat("Horizontal", 0.0f, 0.1f, Time.deltaTime);
            animator.SetFloat("WalkSpeed", 1);

            if (IsJumping)
            {
                animator.SetFloat("WalkSpeed", 0);
            }
        }
        else
        {
            animator.SetFloat("Vertical", 0.0f, 0.1f, Time.deltaTime);
            animator.SetFloat("Horizontal", 0.0f, 0.1f, Time.deltaTime);
        }
    }
}
