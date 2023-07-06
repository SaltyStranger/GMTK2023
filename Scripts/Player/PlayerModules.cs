using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerModules : MonoBehaviour
{
    // Series of bools to toggle individual modules in the editor.
    // Allow their respective movement type only if true.
    [Header("Module Toggle Variables")]
    [SerializeField]
    private bool dashEnabled;

    // Variables for the individual modules
    [Header("Dash Controls")]
    [SerializeField]
    private float dashSpeed;
    [SerializeField]
    private int dashTime;
    [SerializeField]
    private int dashCooldown;

    // Attach the PlayerMovementController
    [SerializeField]
    private PlayerMovementController pmc;

    // Prefabs used for various rendering effects
    [SerializeField]
    private SpriteRenderer playerSpriteRenderer;
    [SerializeField]
    private GameObject spectrePrefab;

    // Keybinds
    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    // INTERNAL VALUES
    
    // TRUE when the player is dashing and dash distance timer has not reached 0.
    private bool isDashing;
    // Tracks how long the player has been dashing (Dash force ends when this reaches dashTime)
    private int dashTimeElapsed;
    // Tracks the dash cooldown. Can dash again when this reaches 0. Begins counting down when dash is terminated
    private int dashCurrCD;
    // Tracks the facing direction when a dash is started.
    private EnumFacing dashDir;

    private bool dashState = false;


    private void Start()
    {
        // Dash
        controls.MouseKeyboard.dash.started += _ => { StartDash(); };
        controls.MouseKeyboard.dash.canceled += _ => { StopDash(); };
    }

    public Vector2 FixedUpdateModuleOverrides(Vector2 moveVector)
    {
        moveVector = dashLoop(moveVector);
        return moveVector;
    }

    private Vector2 dashLoop(Vector2 moveVector)
    {
        // Run this loop only if the Dash Module is enabled
        if (dashEnabled)
        {
            // If trying to start a dash (i.e. holding dash key) and dash cooldown is 0
            if (isDashing && dashCurrCD == 0)
            {
                // This if statement is effectively an "on dash actually started"
                if(dashTimeElapsed == 0)
                {
                    pmc.getAnimator().SetTrigger("StartDash");
                    pmc.getAnimator().SetBool("IsDashing", true);
                    dashDir = pmc.getFacing();
                    dashState = true;
                }

                // The actual dash
                moveVector.x = dashSpeed * ((dashDir == EnumFacing.LEFT) ? -1 : 1);
                moveVector.y = 0;

                // Increment dash timer
                dashTimeElapsed++;
                // Draw Lingering Spectre
                LingeringSpectre.createSpectre(0.5f, 5, spectrePrefab, playerSpriteRenderer.sprite, transform.position + new Vector3(0.0f, 0.5f, 0.0f), Quaternion.identity, (pmc.getFacing() == EnumFacing.LEFT ? new Vector3(-1, 1, 1) : new Vector3(1, 1, 1)));

                // End the dash at the max amount of time allowed to dash
                if (dashTimeElapsed >= dashTime)
                {
                    StopDash();
                }

                // End the dash if direction is changed while dashing
                if(dashDir != pmc.getFacing())
                {
                    StopDash();
                }

                // End the dash if a wall is collided with
                if (pmc.getTouchingFront())
                {
                    StopDash();
                }
            }
            else if (dashCurrCD > 0)
            {
                dashCurrCD--;
            }
        }
        return moveVector;
    }

    private void StartDash()
    {
        isDashing = true;
    }

    private void StopDash()
    {
        isDashing = false;
        dashState = false;
        pmc.getAnimator().SetBool("IsDashing", false);
        // Yes, it breaks without this check due to releasing the key.
        if(dashCurrCD <= 0)
        {
            dashCurrCD = dashCooldown;
        }
        dashTimeElapsed = 0;
    }

    public bool getIsDashing()
    {
        // Do a prelim dash check for the sake of ground detection
        // Run this loop only if the Dash Module is enabled
        if (dashEnabled)
        {
            // If trying to start a dash (i.e. holding dash key) and dash cooldown is 0
            if (isDashing && dashCurrCD == 0)
            {
                dashState = true;
            }
        }
        return dashState;
    }
}
