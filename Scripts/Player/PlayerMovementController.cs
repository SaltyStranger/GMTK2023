using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    [Header("Jump Controls")]
    public float jumpVelocity;
    // How long the jump button can be held for to continue gaining height.
    public float maxJumpTime;
    // Minimum jump time after being input.
    public float minJumpTime;
    public float fallSpeedCap;
    public int maxBufferJumpFrames;
    public float gravityValue;

    [Header("Walk Controls")]
    // Determines the target speed while walking.
    public float maxWalkSpeed;
    // Number of Frames to go from 0 to the max walking speed.
    public float accelFrames;
    // Percent slowdown when changing direction in air. Should be between 0 and 1, with 0 being unable to control movement in air, and 1 being no different than being on land.
    public float airSlowdownPercent;

    [Header("Internal Handlers Below This Point")]
    // The player Rigidbody. Use to MovePosition (DO NOT USE VELOCITY)
    public Rigidbody2D r2;
    // Variables to track if a collision is occuring on a face. Testing still required. Controlled by PhsyicsContactCheck
    private bool onGround, touchingFront, touchingBack, touchingTop;
    private bool isJumping;
    private float jumpTime;
    int bufferTimer;

    [Header("Attack Controllers")]

    // How far the Raycasts should travel for detection. May need fine-tuning.
    public float checkDistance;
    // Holds what types of Layers should be counted as collidable for the sake of detection in onGround, touching____
    // 0 is default. 1 is while dashing.
    public LayerMask[] whatIsGround;

    // Jump Internal Stuff
    // Holds the positions of collider objects on the player. Iterate through and raytrace to detect collisions.
    public Transform[] feetPos, headPos, frontPos, backPos;

    // Tracks key inputs
    bool jumpInput, leftInput, rightInput, upInput, downInput, pauseCancel;

    bool isTransitioning;

    EnumFacing facing = EnumFacing.RIGHT;

    // Tracks the groundState. 0 is default. 1 is while dashing
    private int groundState = 0;

    // Sceneloader
    // private SceneLoadStandardPacket standardSceneLoadPacket;

    // Connected Objects
    public PlayerBehavior playerBehavior;
    // public PlayerInventory inventory;
    public Animator animator;
    public PlayerModules modules;

    // Keybinds
    private PlayerControls controls;

    // Vector for remembering previous frame's movement.
    Vector2 oldMoveVector = new Vector2(0.0f, 0.0f);

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

    // Start is called before the first frame update
    void Start()
    {
        // Performed = KeyDown, Canceled = KeyUp

        // Jump
        controls.MouseKeyboard.Jump.started += _ => { StartJump(); };
        controls.MouseKeyboard.Jump.canceled += _ => { EndJump(); };

        // Move
        controls.MouseKeyboard.left.started += _ => { startLeft(); };
        controls.MouseKeyboard.left.canceled += _ => { stopLeft(); };
        controls.MouseKeyboard.right.started += _ => { startRight(); };
        controls.MouseKeyboard.right.canceled += _ => { stopRight(); };

        // Up Down

        controls.MouseKeyboard.up.started += _ => { startUp(); };
        controls.MouseKeyboard.up.canceled += _ => { stopUp(); };
        controls.MouseKeyboard.down.started += _ => { startDown(); };
        controls.MouseKeyboard.down.canceled += _ => { stopDown(); };

        // Pause
        controls.MouseKeyboard.pause.started += _ => { updatePause(); };
        controls.MouseKeyboard.pause.canceled += _ => { pauseCancel = false; };
    }

    // Update is called once per frame (Depends on FPS -- Do not use for anything that should run on Physics -- only call for rendering)
    void Update()
    {
        // Make sure the game is unpaused
        if (GamestateHandler.Instance != null && !GamestateHandler.Instance.getIsGamePaused())
        {
            if(onGround && Mathf.Abs(oldMoveVector.x) > 0.0f)
            {
                //animator.SetBool("IsWalking", true);
            }
            if (onGround && Mathf.Abs(oldMoveVector.x) == 0.0f)
            {
                //animator.SetBool("IsWalking", false);
            }
            if(!onGround)
            {
                if(oldMoveVector.y < 0.0f)
                {
                    //animator.SetBool("FallingDownward", true);
                }
                else
                {
                    //animator.SetBool("FallingDownward", false);
                }
            }
        }
    }

    // Update is called once per Physics frame (1/50 of a second)
    private void FixedUpdate()
    {
        // Updates Pointers to indicate which directions the player is making contact with Orange or Black surfaces.
        physicsContactCheck();

        // Move the player while on land.
        physicsLandMove();
    }

    private void physicsLandMove()
    {
        // Sum of all movement sources
        // Gravity
        // Left and Right inputs
        // Jump
        Vector2 moveVector = omniMoveLand();

        // Some modules may override this move vector entirely, or modify it. Apply those changes separately. 
        // moveVector = modules.FixedUpdateModuleOverrides(moveVector);

        // A method that updates values of the move vector according to upcoming collisions. Also snaps the player to tiles it's colliding with.
        moveVector = snapToTiles(moveVector);

        r2.MovePosition(r2.position + moveVector);

        oldMoveVector = moveVector;
    }

    // Some methods will update the value of moveVector to 0 on that axis after snapping.
    private Vector2 snapToTiles(Vector2 moveVector)
    {
        //Debug.Log("Snap");
        // Snap Down, then Up, then Right, then Left
        if (movingDownward(moveVector) > 0.0f)
        {
            moveVector = snapDownward(moveVector, new Vector2(0.0f, 0.0f));
        }

        if (movingUpward(moveVector) > 0.0f)
        {
            //snapUpward(moveVector);
        }

        if (movingRightward(moveVector) > 0.0f)
        {
            moveVector = snapRightward(moveVector, new Vector2(0.0f, moveVector.y));
        }

        if (movingLeftward(moveVector) > 0.0f)
        {
            moveVector = snapLeftward(moveVector, new Vector2(0.0f, moveVector.y));
        }

        // Correction to prevent OOB Corner clips
        if (movingDownward(moveVector) > 0.0f)
        {
            moveVector = snapRightward(moveVector, new Vector2(moveVector.x, 0.0f));
        }

        if (movingUpward(moveVector) > 0.0f)
        {
            moveVector = snapLeftward(moveVector, new Vector2(moveVector.x, 0.0f));
        }

        return moveVector;
    }

    private Vector2 applyGravity()
    {
        if(!onGround)
        {
            // A negative number, determines how far the player will fall this Physics Frame.
            float downval = oldMoveVector.y - gravityValue;

            // If it tries to move down faster than the fall speed cap, stop it!
            if (downval < -fallSpeedCap)
            {
                downval = -fallSpeedCap;
            }

            // Otherwise, fall by the updated amount with Gravity scaling from the previous frame.
            return new Vector2(0.0f, downval);
        }
        else
        {
            // Gravity should do nothing if the player is on the ground, so we make it modify the overall move vector by 0.
            return new Vector2(0.0f, 0.0f);
        }
    }

    // Controls left-right movement on land. Returns the updates to the x-pos from left and right walking inputs.
    private Vector2 omniMoveLand()
    {
        // The Vector to be returned. Begins as 0.
        Vector2 retVec = new Vector2(0.0f, 0.0f);

        // The amount of acceleration allowed per frame. A small optimization could be to calculate this once for all time, but oh well.
        float accelAmount = maxWalkSpeed / accelFrames;

        // If not on the ground, update the accelAmount to the airSlowdown value.
        if(!onGround)
        {
            accelAmount = accelAmount * airSlowdownPercent;
        }

        // If both inputs are held, decel naturally to 0.
        if(leftInput && rightInput)
        {
            if (Mathf.Abs(oldMoveVector.x) > 0.0f)
            {
                retVec.x = approachValue(0.0f, oldMoveVector.x, accelAmount);
            }
        }
        // Otherwise, if only the left input is held, accelerate to max speed to the left. At max speed, hold constant until told otherwise.
        else if (leftInput)
        {
            setFacing(EnumFacing.LEFT);
            retVec.x = approachValue(-maxWalkSpeed, oldMoveVector.x, accelAmount);
        }
        // Or, if the right input is held, accelerate to max speed to the right. At max speed, hold constant until told otherwise.
        else if (rightInput)
        {
            setFacing(EnumFacing.RIGHT);
            retVec.x = approachValue(maxWalkSpeed, oldMoveVector.x, accelAmount);
        }
        // Finally, if no inputs are held, but there is still x motion from the previous frame, decelerate.
        else if(Mathf.Abs(oldMoveVector.x) > 0.0f)
        {
            retVec.x = approachValue(0.0f, oldMoveVector.x, accelAmount);
        }

        // If both inputs are held, decel naturally to 0.
        if (upInput && downInput)
        {
            if (Mathf.Abs(oldMoveVector.y) > 0.0f)
            {
                retVec.y = approachValue(0.0f, oldMoveVector.y, accelAmount);
            }
        }
        // Otherwise, if only the left input is held, accelerate to max speed to the left. At max speed, hold constant until told otherwise.
        else if (upInput)
        {
            //setFacing(EnumFacing.LEFT);
            retVec.y = approachValue(maxWalkSpeed, oldMoveVector.y, accelAmount);
        }
        // Or, if the right input is held, accelerate to max speed to the right. At max speed, hold constant until told otherwise.
        else if (downInput)
        {
            //setFacing(EnumFacing.RIGHT);
            retVec.y = approachValue(-maxWalkSpeed, oldMoveVector.y, accelAmount);
        }
        // Finally, if no inputs are held, but there is still x motion from the previous frame, decelerate.
        else if (Mathf.Abs(oldMoveVector.y) > 0.0f)
        {
            retVec.y = approachValue(0.0f, oldMoveVector.y, accelAmount);
        }

        // Return the retVec.
        return retVec;
    }

    // Adds the jumping component to the movementVector
    private Vector2 jumpVector()
    {
        // A set of functions that lets a jump begin if it was buffered.
        bufferJumpHandler();

        if (isJumping)
        {
            if (touchingTop)
            {
                jumpTime = 0;
                isJumping = false;
                return new Vector2(0.0f, -oldMoveVector.y);
            }
            if (jumpTime > 0)
            {
                jumpTime -= 1;
                return new Vector2(0.0f, jumpVelocity);
            }
            else
            {
                isJumping = false;
            }
        }

        return new Vector2(0.0f, 0.0f);
    }

    private void physicsContactCheck()
    {
        // Does a prelim check and updates isDashing if a dash will begin this frame.
        getGroundState();
        // Check all 4 directions for collisions
        checkIsGrounded();
        if (!onGround) checkIsBonk();
        checkIsTouchingFront();
        checkIsTouchingBack();
    }

    // Gets the ground state and assigns it to groundState variable
    private int getGroundState()
    {
        int state = 0;
        //if(modules.getIsDashing())
        //{
        //    state = 1;
        //}
        groundState = state;
        return state;
    }

    // A set of functions that lets a jump begin if it was buffered.
    private void bufferJumpHandler()
    {
        if(bufferTimer > 0)
        {
            if (onGround && jumpInput)
            {
                initiateJumpFunction();
            }
            else
            {
                bufferTimer--;
            }
        }
    }

    private void StartJump()
    {
        jumpInput = true;
        if (onGround)
        {
            initiateJumpFunction();
        }
        else
        {
            bufferTimer = maxBufferJumpFrames;
        }
    }

    // General methods for starting a jump.
    private void initiateJumpFunction()
    {
        isJumping = true;
        jumpTime = maxJumpTime;
        bufferTimer = 0;
        //animator.SetTrigger("Jump");
    }

    private void EndJump()
    {
        jumpInput = false;
        if (maxJumpTime - jumpTime < minJumpTime) { jumpTime = minJumpTime - (maxJumpTime - jumpTime); }
        else { jumpTime = 0; isJumping = false; }
    }

    // Run on left input pressed
    private void startLeft()
    {
        leftInput = true;
    }

    // Run on left input released
    private void stopLeft()
    {
        leftInput = false;
    }

    // Run on right input pressed
    private void startRight()
    {
        rightInput = true;
    }

    // Run on right input released
    private void stopRight()
    {
        rightInput = false;
    }

    // Run on up input pressed
    private void startUp()
    {
        upInput = true;
    }

    // Run on up input released
    private void stopUp()
    {
        upInput = false;
    }

    // Run on down input pressed
    private void startDown()
    {
        downInput = true;
    }

    // Run on down input released
    private void stopDown()
    {
        downInput = false;
    }

    private bool checkIsGrounded()
    {
        onGround = false;
        foreach (Transform tf in feetPos)
        {
            onGround |= RaycastMultilayer(tf.position, Vector2.down, checkDistance, whatIsGround[groundState]);
            Debug.Log(RaycastMultilayer(tf.position, Vector2.down, checkDistance, whatIsGround[groundState]));
        }
        // A set animator state
        //animator.SetBool("onGround", onGround);
        return onGround;
    }

    private bool checkIsBonk()
    {
        touchingTop = false;
        foreach (Transform tf in headPos)
        {
            touchingTop |= RaycastMultilayer(tf.position, Vector2.up, checkDistance, whatIsGround[groundState]);
        }
        return touchingTop;
    }

    private bool checkIsTouchingFront()
    {
        touchingFront = false;
        if (facing == EnumFacing.RIGHT)
        {
            foreach (Transform tf in frontPos)
            {
                touchingFront |= RaycastMultilayer(tf.position, Vector2.right, checkDistance, whatIsGround[groundState]);
            }
        }
        else
        {
            foreach (Transform tf in frontPos)
            {
                touchingFront |= RaycastMultilayer(tf.position, Vector2.left, checkDistance, whatIsGround[groundState]);
            }
        }
        return touchingFront;
    }

    private bool checkIsTouchingBack()
    {
        touchingBack = false;
        if (facing == EnumFacing.RIGHT)
        {
            foreach (Transform tf in backPos)
            {
                touchingBack |= RaycastMultilayer(tf.position, Vector2.left, checkDistance, whatIsGround[groundState]);
            }
        }
        else
        {
            foreach (Transform tf in backPos)
            {
                touchingBack |= RaycastMultilayer(tf.position, Vector2.right, checkDistance, whatIsGround[groundState]);
            }
        }
        return touchingBack;
    }

    // Public getters for what faces are making contact.
    public bool getTouchingFront()
    {
        return touchingFront;
    }

    public bool getTouchingBack()
    {
        return touchingBack;
    }

    public bool getTouchingTop()
    {
        return touchingTop;
    }

    public bool getOnGround()
    {
        return onGround;
    }

    private void updatePause()
    {
        if (!isTransitioning)
        {
            if (!pauseCancel)
            {
                if (!GamestateHandler.Instance.getIsGamePaused()) StartPause();
                else playerBehavior.resumeGame();

                pauseCancel = true;
            }
        }
    }

    private void StartPause()
    {
        playerBehavior.pauseGame();
    }

    // Turn the player.
    private void setFacing(EnumFacing dir)
    {
        this.facing = dir;
        if (facing == EnumFacing.LEFT)
        {
            transform.localScale = new Vector3(-1, transform.localScale.y, transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z);
        }
    }

    // Helper method to get the facing direction in terms of a readable Enum
    public EnumFacing getFacing()
    {
        return facing;
    }

    private bool RaycastMultilayer(Vector2 origin, Vector2 direction, float distance, params LayerMask[] targetLayers)
    {
        bool ret = false;

        foreach(LayerMask layer in targetLayers)
        {
            ret |= Physics2D.Raycast(origin, direction, distance, layer);
        }
        return ret;
    }

    private float[] RaycastMultilayerPos(Vector2 origin, Vector2 direction, float distance, params LayerMask[] targetLayers)
    {
        Vector2 ret = new Vector2(0.0f,0.0f);

        foreach (LayerMask layer in targetLayers)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, layer);
            
            ret = new Vector2(hit.point.x, hit.point.y);

            if (ret.magnitude != 0.0f)
            {
                float[] retArr = { ret.x, ret.y };
                return retArr;
            }
        }

        return null;
    }

    // Helper method that returns the intitial value modified by up to amount such that it's absolute value is as close to targetVal as possible.
    private float approachValue(float target, float initVal, float amount)
    {
        // If already target, remain target.
        if (initVal == target) return target;
        // If greater than target...
        else if (initVal > target)
        {
            // And subtracting amount would make it less than target...
            if (initVal - amount <= target)
                // Return target.
                return target;
            // And subtracting amount leaves it greater than target...
            else
                // Return that updated amount, it's as close as you can get.
                return initVal - amount;
        }
        // Sim
        else if (initVal < target)
        {
            if (initVal + amount >= target)
                return target;
            else
                return initVal + amount;
        }
        // Catch all, should never run.
        return target;
    }

    // A handful of helper methods for getting the values.
    // Returns the value being moved in that direction (always positive). 0.0f if not moving in that direction.

    private float movingRightward(Vector2 moveVector)
    {
        if (moveVector.x > 0.0f)
        {
            return moveVector.x;
        }
        return 0.0f;
    }

    // Inverts moving forward.
    private float movingLeftward(Vector2 moveVector)
    {
        if (moveVector.x < 0.0f)
        {
            return -moveVector.x;
        }
        return 0.0f;
    }

    private float movingDownward(Vector2 moveVector)
    {
        if(moveVector.y < 0.0f)
        {
            return -moveVector.y;
        }
        return 0.0f;
    }

    private float movingUpward(Vector2 moveVector)
    {
        if (moveVector.y > 0.0f)
        {
            return moveVector.y;
        }
        return 0.0f;
    }

    /*
     * Snap functions (moved to bottom bc they will likely need very little maintenance and are rather large.
     */
    private Vector2 snapDownward(Vector2 moveVector, Vector2 offset)
    {
        // Want to find the highest ground pos so they don't end up snapping down too far at once.
        // Set to negative infinity to start. Will increase if ground is detecting, ending at the highest ground pos found.
        Vector2 maxGroundPos = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (Transform tf in feetPos)
        {
            // Find the Ground Pos if in range.
            float[] groundPosArr = RaycastMultilayerPos(tf.position + new Vector3(offset.x, offset.y, 0.0f), Vector2.down, movingDownward(moveVector), whatIsGround[groundState]);

            // If the ground was found to be in range.
            if (groundPosArr != null)
            {
                // If this is the highest ground found, set maxGroundPos to it.
                Vector2 groundPos = new Vector2(groundPosArr[0], groundPosArr[1]);
                if (groundPos.y > maxGroundPos.y)
                {
                    maxGroundPos.y = groundPos.y;
                }
            }
        }

        // If maxGroundPos was updated, ground was found, so snap to it!.
        if (maxGroundPos.y != float.NegativeInfinity)
        {
            moveVector = new Vector2(moveVector.x, -(feetPos[0].position.y - maxGroundPos.y - (checkDistance / 2.0f)));
        }
        return moveVector;
    }

    private Vector2 snapRightward(Vector2 moveVector, Vector2 offset)
    {
        // Want to find the highest ground pos so they don't end up snapping down too far at once.
        // Set to negative infinity to start. Will increase if ground is detecting, ending at the highest ground pos found.
        Vector2 maxGroundPos = new Vector2(float.PositiveInfinity, float.PositiveInfinity);

        Transform[] usePos = (facing == EnumFacing.LEFT) ? backPos : frontPos;

        foreach (Transform tf in usePos)
        {
            // Find the Ground Pos if in range.
            float[] groundPosArr = RaycastMultilayerPos(tf.position + new Vector3(offset.x, offset.y, 0.0f), Vector2.right, movingRightward(moveVector), whatIsGround[groundState]);

            // If the ground was found to be in range.
            if (groundPosArr != null)
            {
                // If this is the leftmost ground found, set maxGroundPos to it.
                Vector2 groundPos = new Vector2(groundPosArr[0], groundPosArr[1]);
                if (groundPos.x < maxGroundPos.x)
                {
                    maxGroundPos.x = groundPos.x;
                }
            }
        }

        // If maxGroundPos was updated, ground was found, so snap to it!.
        if (maxGroundPos.x != float.PositiveInfinity)
        {
            moveVector = new Vector2(maxGroundPos.x - usePos[0].position.x - (checkDistance / 2.0f), moveVector.y);
        }
        return moveVector;
    }

    private Vector2 snapLeftward(Vector2 moveVector, Vector2 offset)
    {
        // Want to find the highest ground pos so they don't end up snapping down too far at once.
        // Set to negative infinity to start. Will increase if ground is detecting, ending at the highest ground pos found.
        Vector2 maxGroundPos = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        Transform[] usePos = (facing == EnumFacing.LEFT) ? frontPos : backPos;

        foreach (Transform tf in usePos)
        {
            // Find the Ground Pos if in range.
            float[] groundPosArr = RaycastMultilayerPos(tf.position + new Vector3(offset.x, offset.y, 0.0f), Vector2.left, movingLeftward(moveVector), whatIsGround[groundState]);

            // If the ground was found to be in range.
            if (groundPosArr != null)
            {
                // If this is the rightmost ground found, set maxGroundPos to it.
                Vector2 groundPos = new Vector2(groundPosArr[0], groundPosArr[1]);
                if (groundPos.x > maxGroundPos.x)
                {
                    maxGroundPos.x = groundPos.x;
                }
            }
        }

        // If maxGroundPos was updated, ground was found, so snap to it!.
        if (maxGroundPos.x != float.NegativeInfinity)
        {
            moveVector = new Vector2(maxGroundPos.x - usePos[0].position.x + (checkDistance / 2.0f), moveVector.y);
        }
        return moveVector;
    }

    public Animator getAnimator()
    {
        return animator;
    }
}
