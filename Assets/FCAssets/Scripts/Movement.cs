using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Movement : MonoBehaviour
{
    public GameObject Chat;
    public GameObject ChatMessage;
    public GameObject Options;
    public GameObject EmotesMenu;
    public GameObject ModeVotingMenu;
    public GameObject VotingMenu;
    public GameObject GameCanvas;
    public GameObject InitialCountdown;

    public GameObject MPCanvas;
    public Rigidbody PlayerBody;
    public Transform PlayerPosition;
    public GameObject PlayerModel;

    public float spawnX;
    public float spawnY;
    public float spawnZ;

    private Animator animator;

    public Camera PlayerCamera;
    public LayerMask layerMask;
    public LayerMask finishLine;
    public float playerHeight = 2f;
    public float dragOnGround = 5f;
    bool onGround = false;
    bool topWall = false;
    bool finishedMap;

    public Transform orientation;
    float horizontal;
    float vertical;
    Vector3 direction;

    private int jumpCooldown = 10;
    private int timeSinceLastJump = 0;
    public float jumpForce;
    public float playerSpeed;
    public float airMultiplier;
    float defaultSpeed = 10f;
    float sprintSpeed = 15f;
    float crouchSpeed = 5f;
    float sprintFov = 80f;
    float defaultFov = 70f;
    float currentFov = 70f;
    float velocityThreshold = 0.01f;
    public float walljumpBufferWindow = 60f;
    public float climbJumpBufferWindow = 60f;
    bool crouched = false;
    public Vector3 originalScale;

    private RaycastHit slopeHit;
    private float currentSlopeAngle = 0f;
    bool currentlyOnSlope = false;
    bool justHitSlope = false;
    bool jumpFromSlope = false;
    bool momentumJump = false;

    public float raycastDistance = 1f;
    public float walljumpForce = 6f;

    private int wallJumpCooldown = 0;
    private Vector3 lastDifference = new Vector3(-99,-99,-99);

    private int climbJumpCooldown = 10;

    bool pressedJump = false;
    float currentBuffer = 0f;
    float climbJumpBuffer = 0f;

    public TextMeshProUGUI speedText;
    public TextMeshProUGUI timer;
    public TextMeshProUGUI hangText;

    float time;
    float endTime;
    public GameObject congratsScreen;
    public TextMeshProUGUI endTimeText;

    public Transform CameraBodyTransform;
    public Transform DefaultPoint;

    private bool blockInputs = false;

    private List<GameObject> Collisions = new List<GameObject>();

    Vector3 RoundVector(Vector3 vector)
    {
        float roundedX = Mathf.Round(vector.x);
        float roundedY = Mathf.Round(vector.y);
        float roundedZ = Mathf.Round(vector.z);

        return new Vector3(roundedX, roundedY, roundedZ);
    }


    string FormatTime(float totalSeconds)
    {
        int minutes = Mathf.FloorToInt(totalSeconds / 60);
        int seconds = Mathf.FloorToInt(totalSeconds % 60);
        int milliseconds = Mathf.FloorToInt((totalSeconds - Mathf.FloorToInt(totalSeconds)) * 1000);
        return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
    }

    void ShowEndScreen()
    {
        if (!congratsScreen.activeSelf)
        {
            PlayerBody.velocity = new Vector3(0f, 0f, 0f);
            endTime = time;
            time = 0f;
            congratsScreen.SetActive(true);
            endTimeText.text = "Final Time: " + FormatTime(endTime);
            Debug.Log("Finished Map");
            speedText.text = "";
            timer.text = "";
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }


    void Start()
    {
        animator = PlayerModel.GetComponent<Animator>();

        Time.timeScale = 1.0f;
        spawnX = PlayerPosition.position.x;
        spawnY = PlayerPosition.position.y;
        spawnZ = PlayerPosition.position.z;

        // Floating point sync
        CameraBodyTransform.position = DefaultPoint.position;

        // Sets the timer to zero
        time = 0f;
    }


    void Update()
    {
        //Debug.Log("Finished Map: " + finishedMap);
        Vector3 forward = PlayerPosition.TransformDirection(Vector3.forward) * 10;

        // Climb range
        Debug.DrawRay(PlayerPosition.position + Vector3.up*5, forward, Color.green);
        Debug.DrawRay(PlayerPosition.position + Vector3.up*3, forward, Color.green);

        // Walljump range
        Debug.DrawRay(PlayerPosition.position + Vector3.up, forward, Color.red);

        // Raycasts for ground, finish line and top wall

        RaycastHit hit;

        onGround = Physics.BoxCast(PlayerPosition.position + (Vector3.up), new Vector3(0.5f, 0.1f, 0.5f), Vector3.down, PlayerPosition.rotation, 1, layerMask);
        //Debug.Log("Onground? " + onGround);
        finishedMap = Physics.BoxCast(PlayerPosition.position - (Vector3.down), new Vector3(0.49f, 0.49f, 0.49f), Vector3.down, PlayerPosition.rotation, 1, finishLine);

        Debug.DrawRay(transform.position, Vector3.down * (playerHeight + 0.3f), finishedMap ? Color.green : Color.red);

        topWall = Physics.Raycast(transform.position + Vector3.up * 2.5f, Vector3.up, out hit, playerHeight + 1f, layerMask);
        Debug.DrawRay(transform.position + Vector3.up * 2.5f, Vector3.up * (playerHeight + 1f), topWall ? Color.red : Color.green);


        // For debugging

        float speed = PlayerBody.velocity.magnitude;
        speedText.text = "Speed: " + speed;


        timeSinceLastJump++; // ??

        //
        // Climb raycasts
        //


        //
        // TIMER & ENDSCREEN
        //

        if (!finishedMap)
        {
            if (GameCanvas.activeSelf){
                time += Time.deltaTime;
                //Debug.Log(time);
                timer.text = FormatTime(time);
            }
        }
        else
        {
            ShowEndScreen();
        }

        //
        // HANDLE INPUTS
        //


        // Block inputs for GUIs

        if (Chat.activeSelf || Options.activeSelf || congratsScreen.activeSelf || EmotesMenu.activeSelf || VotingMenu.activeSelf || ModeVotingMenu.activeSelf) {
            blockInputs = true;

            if (!MPCanvas.activeSelf){
                Time.timeScale = 0f;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (InitialCountdown.activeSelf) {
            blockInputs = true;
        }
        else if (EventSystem.current.currentSelectedGameObject == ChatMessage){
            blockInputs = true;
        }
        else {
            blockInputs = false;

            Time.timeScale = 1.0f;

            // Hide and lock cursor when not in a GUI

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Set player orientation to the mouse input

            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
            if(Mathf.Abs(vertical) < 1f)
            {
                vertical = 0f;
            }

            if(Mathf.Abs(horizontal) < 1f)
            {
                horizontal = 0f;
            }
            //Debug.Log("Inputs Not Blocked");

        }
        


         // Sprinting

        if (!crouched && onGround)
        {
            if (Input.GetKey(KeyCode.LeftShift) && !blockInputs)
            {
                animator.SetBool("Run", true);
                currentFov = sprintFov;
                playerSpeed = sprintSpeed;
            }
        }
        if (Input.GetKeyUp(KeyCode.LeftShift) && !blockInputs)
        {
            animator.SetBool("Run", false);
            currentFov = defaultFov;
            playerSpeed = defaultSpeed;
        }
        PlayerCamera.fieldOfView = Mathf.Lerp(PlayerCamera.fieldOfView, currentFov, Time.deltaTime * 5f);

        // Crouching

        if (Input.GetKeyDown(KeyCode.LeftControl) && !blockInputs)
        {
            crouched = true;
            Vector3 newScale = new Vector3(originalScale.x, originalScale.y / 2f, originalScale.z);
            transform.localScale = newScale;
            PlayerBody.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            playerSpeed = crouchSpeed;
        }
        if (Input.GetKeyUp(KeyCode.LeftControl) && !topWall)
        {
            crouched = false;
            transform.localScale = originalScale;
            playerSpeed = defaultSpeed;
        }

        if(!Input.GetKey(KeyCode.LeftControl) && crouched && !topWall){
            crouched = false;
            transform.localScale = originalScale;
            playerSpeed = defaultSpeed;
        }

        //
        // ANIMATIONS
        //

        // Stop default animation for emotes

        if (RoundVector(CameraBodyTransform.position) != RoundVector(DefaultPoint.position) && ((Input.GetKeyDown(KeyCode.W) && !blockInputs) || (Input.GetKeyDown(KeyCode.S) && !blockInputs) || (Input.GetKeyDown(KeyCode.A) && !blockInputs) || (Input.GetKeyDown(KeyCode.D) && !blockInputs) || (Input.GetKeyDown(KeyCode.Space) && !blockInputs) || (Input.GetKeyDown(KeyCode.R) && !blockInputs))) {
            CameraBodyTransform.position = DefaultPoint.position;
            animator.Play("Main", -1, 0f);
        }

        // Set animation values

        if (!blockInputs){
            animator.SetFloat("vertical", Input.GetAxis("Vertical"));
            animator.SetFloat("horizontal", Input.GetAxis("Horizontal"));
        }

        if (Input.GetKeyDown(KeyCode.Space) && !blockInputs)
        {
            if (!onGround)
            {
                pressedJump = true;
                currentBuffer = 0;
                climbJumpBuffer = 0;
            }
        }
        if (Input.GetKeyUp(KeyCode.Space) && !blockInputs)
        {
            pressedJump = false;
        }
    }

     bool onSlope()
     {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight + 1f))
        {
            currentSlopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
            if(currentSlopeAngle != 0 && currentSlopeAngle <= 45)
            {
                return true;
            }
        }
        return false;
     }

    void SpeedCap()
    {
        Vector3 current = new Vector3(PlayerBody.velocity.x, 0f, PlayerBody.velocity.z);
        if (!momentumJump)
        {
            if (current.magnitude > playerSpeed)
            {
                Vector3 cap = current.normalized;
                cap = current.normalized * playerSpeed;
                PlayerBody.velocity = new Vector3(cap.x, PlayerBody.velocity.y, cap.z);
            }
        } else
        {
            Vector3 cap = current;
            PlayerBody.velocity = new Vector3(cap.x, PlayerBody.velocity.y, cap.z);
        }
    }

    void FixedUpdate()
    {
        // Handle speed capping
        SpeedCap();

        // Handle "restarting"

        if (PlayerPosition.position.y < -100 || (Input.GetKey(KeyCode.R) && !blockInputs))
        {
            Vector3 spawn = new Vector3(spawnX, spawnY + 0.2f, spawnZ);
            PlayerBody.velocity = new Vector3(0f, 0f, 0f);
            PlayerPosition.position = spawn;

            // Reset the timer

            time = 0f;
        }

        // Handle player character movement

        if (!blockInputs){
            direction = orientation.forward * vertical + orientation.right * horizontal;
            if (onGround/* || Mathf.Abs(PlayerBody.velocity.y) <= velocityThreshold*/)
            {
                PlayerBody.AddForce(direction.normalized * playerSpeed * 10f, ForceMode.Force);
            }
            else{
                PlayerBody.AddForce(direction.normalized * playerSpeed * 10f * airMultiplier, ForceMode.Force);
            }
        }

        if (pressedJump)
        {
            currentBuffer += 1;
            climbJumpBuffer += 1;
        }
       //   animator.SetBool("CanWallJump", false); (Debug)


        // Stop jump animation (must be before jump handling)

        if (onGround){
            animator.SetBool("Jump", false);
            //Debug.Log("on the ground !");
        }

        // Handle slope enter
        if (onSlope() && !currentlyOnSlope)
        {
            justHitSlope = true;
            currentlyOnSlope = true;
        }
        else
        {
            justHitSlope = false;
        }

        if (!onSlope())
        {
            currentlyOnSlope = false;
        }

        // Handle jumps

        if ((onGround) && !onSlope())
        {
            PlayerBody.drag = dragOnGround;

            if ((Input.GetKey(KeyCode.Space) && !blockInputs) && !topWall && (!onSlope() || (onSlope() && PlayerBody.velocity.y > 0)))
            {
                if (jumpCooldown >= 10)
                {
                    animator.SetBool("Jump", true);
                    PlayerBody.velocity = new Vector3(PlayerBody.velocity.x, 0f, PlayerBody.velocity.z);

                    Debug.Log("Regular Jump");

                    if (PlayerBody.velocity.magnitude < 20)
                        PlayerBody.AddForce(transform.up * jumpForce, ForceMode.Impulse);
                    else
                    {
                        Debug.Log("Momentum Jump");
                        momentumJump = true;
                        Vector3 horizontalVelocity = new Vector3(PlayerBody.velocity.x * 0.7f, 0f, PlayerBody.velocity.z * 0.7f);
                        PlayerBody.AddForce(transform.up * jumpForce, ForceMode.Impulse);
                    }
                    jumpCooldown = 0;
                }
            }
        }

        //
        // SLOPE DYNAMICS
        //

        else if (onSlope() && PlayerBody.velocity.y < 0)
        {
            // Initial slope force
            if (justHitSlope)
            {
                Debug.Log("JustHitSlope");
                PlayerBody.velocity = new Vector3(0f, 0f, 0f);
                Debug.Log("slope angle: " + currentSlopeAngle);
                PlayerBody.AddForce(Vector3.down * 1500f, ForceMode.Force);
                direction = orientation.forward * vertical + orientation.right * horizontal;
                PlayerBody.AddForce(direction.normalized * currentSlopeAngle * 20f, ForceMode.Force);
                justHitSlope = false;
            }
            // Add force forward while on slope
            else
            {
                direction = orientation.forward * vertical + orientation.right * horizontal;
                PlayerBody.AddForce(direction.normalized * currentSlopeAngle * 20f, ForceMode.Force);
            }

            // Handle slope jumping
            if ((Input.GetKey(KeyCode.Space) && !blockInputs) && !topWall && crouched)
            {
                if (jumpCooldown >= 10)
                {
                    jumpFromSlope = true;
                    momentumJump = true;
                    animator.SetBool("Jump", true);
                    PlayerBody.velocity = new Vector3(PlayerBody.velocity.x, 0f, PlayerBody.velocity.z);
                    Debug.Log("Slope Jump");
                    Vector3 horizontalVelocity = new Vector3(PlayerBody.velocity.x, 0f, PlayerBody.velocity.z);
                    PlayerBody.AddForce(transform.up * jumpForce + horizontalVelocity * 2f, ForceMode.Impulse);
                    jumpCooldown = 0;
                }
            }
        }

        //Debug.Log("On Slope? " + onSlope() + ", Velocity: " + PlayerBody.velocity.y);

        // Add force down while un slope

        if (onSlope() && !jumpFromSlope && PlayerBody.velocity.y < 0 && !justHitSlope)
        {
            //Debug.Log("Pushing down");
            PlayerBody.AddForce(Vector3.down * 80f, ForceMode.Force);
        } else
        {
            //Debug.Log("Not Pushing down");
        }

        // Allow slope jump

        if (jumpFromSlope && onGround && jumpCooldown>= 10)
        {
            jumpFromSlope = false;
        }

        //
        // MOMENTUM
        //

        if (!onGround)
        {
            PlayerBody.drag = 1f;
        }

        if (momentumJump)
        {
            PlayerBody.drag = 0.7f;
        }


        if ((momentumJump && onGround && (PlayerBody.velocity.magnitude < 20) && jumpCooldown >= 10) || (!Input.GetKey(KeyCode.Space) && onGround))
        {
            momentumJump = false;
        }

        // Increment jump cooldowns
        jumpCooldown += 1;
        wallJumpCooldown += 1;
        climbJumpCooldown += 1;
        
    }

    void OnTriggerExit(Collider other)
    {
        animator.SetBool("Hang", false);
        Debug.Log("exited");
        hangText.text = "";
    }

    void OnTriggerStay(Collider collisionInfo)
    {

         Vector3 climbCollisionPointHigh = collisionInfo.ClosestPoint(PlayerPosition.position + Vector3.up*5);

        bool isClimbing = false;

        if (climbCollisionPointHigh.y < (PlayerPosition.position + Vector3.up*5).y){
            Debug.Log("hanging range");
            if ((Input.GetKey(KeyCode.Q)) && (climbJumpCooldown >= 40)){
                PlayerBody.velocity = new Vector3 (PlayerBody.velocity.x, 0, PlayerBody.velocity.z);
                PlayerBody.angularVelocity = new Vector3(PlayerBody.angularVelocity.x, 0f, PlayerBody.angularVelocity.z);

                Debug.Log("Clinging");
                animator.SetBool("Hang", true);

                PlayerBody.AddForce(transform.up * 0.2f, ForceMode.Impulse); // Hotfix (player still falls even tho there is no y velocity. IDK WHY)
                // force up
                isClimbing = true;

                  hangText.text = "Hanging";
            }
            else if (climbJumpCooldown >= 40) {
                 hangText.text = "Hold Q to hang";
            }
        }
        else{
            hangText.text = "";
            //Debug.Log("not colliding");
        }


        // Get collision points and difference
        float radius = 0.5f;
        Vector3 collisionPoint = collisionInfo.ClosestPoint(PlayerPosition.position);

        Vector3 difference = collisionPoint - PlayerPosition.position;

        // Set Y difference to 0
        difference.y = 0;

        // If point is exactly on one axis, the distance should always be the same as the radius
        if (difference.x == 0)
        {
            if (difference.z > 0)
            {
                difference.z = radius;
            }
            else
            {
                difference.z = -radius;
            }
        } else if (difference.z == 0)
        {
            if(difference.x > 0)
            {
                difference.x = radius;
            } 
            else 
            {
                difference.x = -radius; 
            }
        } else
        {
            // Calculate M
            float m = ((-difference.z) - difference.z) / ((-difference.x) - difference.x);

            // Calculate Intersection between the line and the circle
            float newX = Mathf.Sqrt((radius * radius) * (1 / (1 + (m * m))));

            // If our X was negative, we need the negative X result
            if (difference.x < 0)
            {
                newX = -newX;
            }
            float newY = m * newX;

            // Set new standardized values as the difference
            difference = new Vector3(newX, 0, newY);
        }

        // Handle walljumps and climbjumps
        if (!onGround)
        {
            //Debug.Log("current buffer " + currentBuffer + ", pressed jump: " + pressedJump);
            if ((Input.GetKey(KeyCode.Space))){
                if (isClimbing && pressedJump && (climbJumpCooldown >= 40) && (wallJumpCooldown >= 30) && (climbJumpBuffer <= climbJumpBufferWindow))
                {
                    climbJumpCooldown = 0;
                    climbJumpBuffer = 0;
                    pressedJump = false;
                    PlayerBody.AddForce(transform.up * 18 + (PlayerPosition.forward * -5), ForceMode.Impulse);

                    PlayerBody.velocity = new Vector3(0f, PlayerBody.velocity.y, 0f);
                    PlayerBody.angularVelocity =  new Vector3(0f, PlayerBody.angularVelocity.y, 0f);

                    animator.SetBool("Hang", false);
                    animator.SetBool("Jump", true);

                    Debug.Log("Climb Jumped up");

                }
                else if ((pressedJump && (wallJumpCooldown >= 30) && (climbJumpCooldown >= 30) && ((difference != lastDifference) || wallJumpCooldown > 95)) && ((collisionPoint.y < (PlayerPosition.position + Vector3.up).y))  && (climbJumpCooldown >= 30) && (currentBuffer <= walljumpBufferWindow)) // Allow walljumping if 100 ticks have passed even if it is on the same wall. // Make sure it is in range for a walljump
                {
                    wallJumpCooldown = 0;
                    currentBuffer = 0;
                    lastDifference = difference;
                    pressedJump = false;
                    PlayerBody.velocity = new Vector3(PlayerBody.velocity.x, 0f, PlayerBody.velocity.z);
                    PlayerBody.angularVelocity = new Vector3(PlayerBody.angularVelocity.x, 0f, PlayerBody.angularVelocity.z);

                    PlayerBody.AddForce(difference * -12f + (transform.up * 11.5f), ForceMode.Impulse);
                     Debug.Log("Wall Jumped");
                }
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (!onSlope() && collision.gameObject.layer == 6)
        {
            float highestY = 0;
            foreach (ContactPoint contact in collision.contacts)
            {
                //print(contact.thisCollider.name + " hit " + contact.otherCollider.name);
                Vector3 Diff = contact.point - PlayerPosition.position;
                //print(Diff);
                if (Diff.y > highestY)
                {
                    highestY = Diff.y;
                }
                // Visualize the contact point
                //Debug.DrawRay(contact.point, contact.normal, Color.white);
            }
            //print("Highest Y of collision: " + highestY);
            if (highestY < 0.6 && highestY > 0.01)
            {
                PlayerPosition.position = new Vector3(PlayerPosition.position.x, PlayerPosition.position.y + highestY + 0.05f, PlayerPosition.position.z);
            }
        }
    }
}
