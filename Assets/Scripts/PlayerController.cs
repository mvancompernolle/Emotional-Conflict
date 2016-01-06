using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Controller2D))]
public class PlayerController : MonoBehaviour
{
    float maxJumpVelocity, minJumpVelocity;
    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = 0.4f;
    float accelerationTimeAirborne = 0.2f;
    float acceleartionTimeGrounded = 0.1f;
    float moveSpeed = 6;
    float gravity;
    Vector3 velocity;
    float velocityXSmoothing;
    Controller2D controller;
    public float wallSlideSpeedMax = 3;
    public float wallStickTime = 0.25f;
    float timeToWallUnstick;
    bool canTeleport = false;


    Vector2 ray1Start, ray1End, ray2Start, ray2End;

    int count = 0;

    public Vector2 wallJumpClimb, wallJumpOff, wallLeap;

    // Use this for initialization
    void Start()
    {
        // get the player controller that handles movement and collisions
        controller = GetComponent<Controller2D>();
        // deltaMovement = velocityInitiial * time + (acceleration * time^2)/2
        // jumpHeight = (gravity * timeToJumpApex^2)/2
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        // velFinal = velInitial + acceleration * time
        maxJumpVelocity = Mathf.Abs(gravity * timeToJumpApex);
        // velFinal^2 = velInit^2 + 2 * accleration * displacement
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
    }

    // Update is called once per frame
    void Update()
    {
        // check to see if player can teleport
        if (controller.collisions.below || controller.collisions.above || controller.collisions.left || controller.collisions.right)
        {
            canTeleport = true;
        }
        if (Input.GetMouseButtonDown(0) && canTeleport)
        {
            // attempt to teleport the player
            Debug.Log("attempting teleport");
            performTeleport();
        }

        // get player input
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        // get whether wall is on the left or right
        int wallDirX = (controller.collisions.left) ? -1 : 1;

        // set player horizontal movement based on input
        float targetVelX = input.x * moveSpeed;
        // smoothly changes velocity to target velocity
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelX, ref velocityXSmoothing, (controller.collisions.below) ? acceleartionTimeGrounded : accelerationTimeAirborne);

        bool wallSliding = false;
        // if on a wall and not on the floor and sliding down then player is wallsliding
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true;
            // if falling faster than wall slide speed, set fall speed to wall slide speed
            if (velocity.y < -wallSlideSpeedMax)
            {
                velocity.y = -wallSlideSpeedMax;
            }

            // if still stuck on wall
            if (timeToWallUnstick > 0)
            {
                // disable smooth velocity shifting
                velocityXSmoothing = 0.0f;
                // stop horizontal movement
                velocity.x = 0.0f;
                // if horizontal input is away from the wall stuck to
                if (input.x != wallDirX && input.x != 0)
                {
                    // reduce wall stick time
                    timeToWallUnstick -= Time.deltaTime;
                }
                else
                {
                    // if moving into wall, reset wall stick time to max
                    timeToWallUnstick = wallStickTime;
                }
            }
            // if wall stick time is up, reset wall stick time variable
            else
            {
                timeToWallUnstick = wallStickTime;
            }
        }

        // if jump pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // if jumping while wall sliding
            if (wallSliding)
            {
                // if jumping towards wall while wallsliding
                if (wallDirX == input.x)
                {
                    velocity.x = -wallDirX * wallJumpClimb.x;
                    velocity.y = wallJumpClimb.y;
                }
                // if jumping off wall
                else if (input.x == 0)
                {
                    velocity.x = -wallDirX * wallJumpOff.x;
                    velocity.y = wallJumpOff.y;
                }
                // if leaping away from wall
                else
                {
                    velocity.x = -wallDirX * wallLeap.x;
                    velocity.y = wallLeap.y;
                }
            }
            // if jumping while on the ground
            if (controller.collisions.below)
            {
                velocity.y = maxJumpVelocity;
            }
        }
        // if jump is released
        if (Input.GetKeyUp(KeyCode.Space))
        {
            // if jumping faster than min jump, reduce to min jump speed
            if (velocity.y > minJumpVelocity)
                velocity.y = minJumpVelocity;
        }

        // apply gravity to y velocity
        velocity.y += gravity * Time.deltaTime;
        // tell controller to move the player by current velocity
        controller.Move(velocity * Time.deltaTime);

        // if player collides with an obstacle above or below, stop vertical movement
        if (controller.collisions.above || controller.collisions.below)
        {
            velocity.y = 0.0f;
        }

        // draw teleport rays for testing
        Debug.DrawRay(ray1Start, ray1End - ray1Start, Color.red);
        Debug.DrawRay(ray2Start, ray2End - ray2Start, Color.red);
    }

    void performTeleport()
    {
        // get teleport angle
        Vector3 playerPos = Camera.main.WorldToScreenPoint(transform.position);
        Vector2 rayDirection = Vector3.Normalize(new Vector2(Input.mousePosition.x, Input.mousePosition.y) - new Vector2(playerPos.x, playerPos.y));
        //Debug.Log("ray: " + rayDirection);

        // determine which two corners to cast rays from
        Vector2 rayOrigin1 = controller.rayCastOrigins.topLeft;
        Vector2 rayOrigin2 = controller.rayCastOrigins.bottomRight;
        // dumb way to get unit circle angle
        float angle = Vector3.Angle(Vector3.up, rayDirection);
        if (rayDirection.x < 0.0f)
        {
            angle = 360.0f - angle;
        }
        angle = (450 - angle) % 360;
        if ((angle > 90 && angle < 180) || (angle > 270 && angle < 360))
        {
            rayOrigin1 = controller.rayCastOrigins.topRight;
            rayOrigin2 = controller.rayCastOrigins.bottomLeft;
        }

        // get the teleport distance
        float rayLength = 0.0f;
        Bounds bounds = controller.collider.bounds;
        if ((angle <= 45 && angle >= 0) || (angle <= 360 && angle >= 315) || (angle >= 135 && angle <= 225))
        {
            rayLength = (bounds.size.x / 2.0f) / Mathf.Cos(angle * Mathf.Deg2Rad);
        }
        else
        {
            rayLength = (bounds.size.x / 2.0f) / Mathf.Sin(angle * Mathf.Deg2Rad);
        }
        rayLength = Mathf.Abs(rayLength);
        //Debug.Log("ray length: " + rayLength);

        // draw 2 rays and see if they hit any obstacles
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin1, rayDirection, rayLength * 1.5f, controller.collisionMask);
        bool wasHit = false;
        if (hit)
        {
            Debug.Log("hit 1!");
            ray1Start = rayOrigin1;
            ray1End = rayOrigin1 + rayDirection * 1.5f;
            wasHit = true;
        }
        hit = Physics2D.Raycast(rayOrigin2, rayDirection, rayLength * 1.5f, controller.collisionMask);
        if (hit)
        {
            Debug.Log("hit 2!");
            ray2Start = rayOrigin2;
            ray2End = rayOrigin2 + rayDirection * 1.5f;
            wasHit = true;
        }

        // if no wall hit, teleport to new location
        if (!wasHit)
        {
            canTeleport = false;
            transform.Translate(rayDirection.x * 1.5f, rayDirection.y * 1.5f, 0.0f);
        }
    }

    void teleportThroughWall(Vector2 rayDirection, float rayLength, RaycastHit2D hit){
        

    }
}