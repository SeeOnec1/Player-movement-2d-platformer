using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{

    [Header("Basic Movement")]
    [SerializeField] private float moveSpeed;
    private float moveInput;
    [SerializeField] private Rigidbody2D rb;
    private bool isFacingRight = true;

    [Header("Jump")]
    [SerializeField] private Transform groundPos;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Vector2 groundCheck;
    [SerializeField] private float jumpingPower;
    private bool isGrounded;
    [SerializeField] private float normalGravity, modifiedGravity;
    private bool isJumping;
    private float coyoteTime = 0.2f;
    private float coyoteTimeCounter;
    private float jumpBufferTime = 0.1f;
    private float jumpBufferCounter;

    [Header("Dash")]
    [SerializeField] private float dashingPower;
    [SerializeField] private float dashingTime;
    [SerializeField] private float dashingCooldownTime;
    private bool isDashing;
    private bool canDash;
    [SerializeField] private TrailRenderer ren;

    [Header("Death")]
    private bool isDead;

    [Header("Camera Shake")]
    [SerializeField] private float shakeIntensity = 3f;
    [SerializeField] private float shakeTime = 0.23f;
    private GameObject cinemachineCamera;
    private CinemachineVirtualCamera vCam;
    CinemachineBasicMultiChannelPerlin cbmcp;

    [Header("Fire")]
    private bool hasFire;
    [SerializeField] private GameObject Fireball;
    [SerializeField] private Transform fireballPos;
    [SerializeField] private GameObject fireOnPlayer;
    private GameObject EndPot;

    [Header("Misc")]
    [SerializeField] private bool gizmoDraw;


    private void Start()
    {
        isDead = false;
        rb.gravityScale = normalGravity;
        canDash = true;
        isJumping = false;
        hasFire = false;

        #region CameraShake
        cinemachineCamera = GameObject.FindGameObjectWithTag("Cinemachine");
        vCam = cinemachineCamera.GetComponent<CinemachineVirtualCamera>();
        cbmcp = vCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        cbmcp.m_AmplitudeGain = 0;
        #endregion
    }

    private void Update()
    {
        #region Checks

        if (isDashing)
        {
            return;
        }

        if (Physics2D.OverlapBox(groundPos.position, groundCheck, 0, groundLayer))
        {
            isGrounded = true;
        }
        else isGrounded = false;

        moveInput = Input.GetAxisRaw("Horizontal");

        #endregion

        #region Dash
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDead && !hasFire)
        {
            StartCoroutine(Dash());
            StartCoroutine(CameraShake());
        }
        #endregion

        #region Jump


        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (coyoteTimeCounter > 0f && jumpBufferCounter > 0f && !isJumping)
        {
            if (!hasFire)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
                jumpBufferCounter = 0f;

                StartCoroutine(JumpCooldown());
            }
        }

        if (Input.GetKeyUp(KeyCode.Space) && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y); //Hollow Knight Jump (y* 0.5f)
            coyoteTimeCounter = 0f;
        }

        /*
        if (Input.GetKeyDown(KeyCode.Space) && !isDead && !hasFire)
        {
            if (isGrounded)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpingPower);           
            }
        }*/

        #endregion

        #region Fire

        if (hasFire)
        {
            fireOnPlayer.SetActive(true);

            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                hasFire = false;
                Instantiate(Fireball, fireballPos.position, Quaternion.identity);
            }
        }
        else if (!hasFire)
        {
            fireOnPlayer.SetActive(false);
        }

        #endregion

        #region Restart

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        #endregion

    }

    private void FixedUpdate()
    {
        if (isFacingRight == false && moveInput > 0)
        {
            Flip();
        }
        else if (isFacingRight == true && moveInput < 0)
        {
            Flip();
        }

        #region Run    
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
        #endregion

        if (!isDashing)
        {
            if (rb.velocity.y >= 0f)
            {
                rb.gravityScale = normalGravity; //ResetingVelocity
            }
            else if (rb.velocity.y < 0f)
            {
                rb.gravityScale = modifiedGravity; //FasterFalling

                if (rb.velocity.y < -20f) //TerminalVelocity
                {
                    rb.velocity = new Vector2(rb.velocity.x, -25f);
                }
            }
        }
    }

    private void Flip()
    {
        if ((isFacingRight && moveInput < 0f) || (!isFacingRight && moveInput > 0f))
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        float originalGravity = normalGravity;
        rb.gravityScale = 0f;
        rb.velocity = new Vector2(transform.localScale.x * dashingPower, 0f);
        ren.emitting = true;
        yield return new WaitForSeconds(dashingTime);

        ren.emitting = false;
        rb.gravityScale = originalGravity;
        isDashing = false;
        yield return new WaitForSeconds(dashingCooldownTime);

        canDash = true;
    }

    private IEnumerator JumpCooldown()
    {
        isJumping = true;
        yield return new WaitForSeconds(0.3f);
        isJumping = false;
    }

    public void FireAcquired()
    {
        hasFire = true;
    }

    public void FireLit()
    {
        if (hasFire)
        {
            hasFire = false;
            Debug.Log("FireLit");
        }

    }

    #region CamShake
    IEnumerator CameraShake()
    {
        cbmcp.m_AmplitudeGain = shakeIntensity;
        yield return new WaitForSeconds(shakeTime);

        cbmcp.m_AmplitudeGain = 0;
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (gizmoDraw)
        {
            Gizmos.DrawWireCube(groundPos.position, groundCheck);
        }
    }
}
