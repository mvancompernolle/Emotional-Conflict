using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    float teleportRange = 1.5f;
    List<DrawRayInfo> rays = new List<DrawRayInfo>();
    bool isGreen = false, isBlue = false;
    public RayCastOrigins teleportRayOrigins;
    float teleportSkinWidth = 0.015f;

    public Vector2 wallJumpClimb, wallJumpOff, wallLeap;

    public struct RayCastOrigins
    {
        public Vector2 topLeft, topRight, bottomLeft, bottomRight;
    }

    public struct DrawRayInfo
    {
        public Vector2 rayStart, rayEnd;
        public Vector4 color;
        public DrawRayInfo(Vector2 start, Vector2 end, Vector4 c)
        {
            rayStart = start;
            rayEnd = end;
            color = c;
        }
    }

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

        // attempt to teleport the player
        if (Input.GetMouseButtonDown(0) && canTeleport)
        {
            // attempt to teleport the player
            Debug.Log("attempting teleport");
            performTeleport();
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
        foreach (DrawRayInfo ray in rays)
        {
            Debug.DrawRay(ray.rayStart, ray.rayEnd - ray.rayStart, ray.color);
        }
    }

    void performTeleport()
    {
        float numRaysPerSide = 3;
        canTeleport = false;
        bool nextToWall = false;
        rays.Clear();

        // get teleport angle
        Vector3 playerPos = Camera.main.WorldToScreenPoint(transform.position);
        Vector2 rayDirection = Vector3.Normalize(new Vector2(Input.mousePosition.x, Input.mousePosition.y) - new Vector2(playerPos.x, playerPos.y));
        //Debug.Log("ray: " + rayDirection);

        UpdatTeleportCastOrigins();

        // determine which two corners to cast rays from
        Vector2 rayOrigin1 = teleportRayOrigins.topLeft;
        Vector2 rayOrigin2 = teleportRayOrigins.bottomRight;
        // dumb way to get unit circle angle
        float angle = Vector3.Angle(Vector3.up, rayDirection);
        if (rayDirection.x < 0.0f)
        {
            angle = 360.0f - angle;
        }
        angle = (450 - angle) % 360;
        rayOrigin1 = (Vector3.Dot(rayDirection, Vector3.right) > 0) ? teleportRayOrigins.topRight : teleportRayOrigins.topLeft;
        rayOrigin2 = (Vector3.Dot(rayDirection, Vector3.up) > 0) ? teleportRayOrigins.topLeft : teleportRayOrigins.bottomLeft;

        // get the teleport distance
        float lengthOfChar = 0.0f;
        Bounds bounds = controller.collider.bounds;
        if ((angle <= 45 && angle >= 0) || (angle <= 360 && angle >= 315) || (angle >= 135 && angle <= 225))
        {
            lengthOfChar = (bounds.size.x) / Mathf.Cos(angle * Mathf.Deg2Rad);
        }
        else
        {
            lengthOfChar = (bounds.size.x) / Mathf.Sin(angle * Mathf.Deg2Rad);
        }
        lengthOfChar = Mathf.Abs(lengthOfChar);

        // draw initial teleport rays on outward facing sides
        bool wasHit = true;
        float raySize = lengthOfChar * teleportRange;
        float distFromStart = raySize;
        float distToCurrentOrigin = 0.0f;
        float yOffset = (teleportRayOrigins.topLeft.y - teleportRayOrigins.bottomLeft.y) / (numRaysPerSide-1);
        float xOffset = (teleportRayOrigins.topRight.x - teleportRayOrigins.topLeft.x) / (numRaysPerSide-1);
        List<RaycastHit2D> collisionPoints = new List<RaycastHit2D>();
        List<Vector2> originPositions = new List<Vector2>();

        int loopNum = 0;
        while (wasHit && loopNum < 10) {
            collisionPoints.Clear();
            originPositions.Clear();

            // cast collision detection rays
            for (int i = 0; i < numRaysPerSide; ++i)
            {
                Vector2 originPos = rayOrigin1 + (rayDirection * distToCurrentOrigin) + (-Vector2.up * yOffset * i);
                RaycastHit2D hit = Physics2D.Raycast(originPos, rayDirection, raySize, controller.collisionMask);
                rays.Add(new DrawRayInfo(originPos, originPos + (rayDirection * raySize), isGreen ? new Vector4(0, 1, 0, 1) : new Vector4(1, 1, 0, 1)));
                if (hit)
                {
                    collisionPoints.Add(hit);
                    originPositions.Add(originPos);
                }
                wasHit |= hit ? true : false;
            }
            for (int i = 0; i < numRaysPerSide; ++i)
            {
                Vector2 originPos = rayOrigin2 + (rayDirection * distToCurrentOrigin) + (Vector2.right * xOffset * i);
                RaycastHit2D hit = Physics2D.Raycast(originPos, rayDirection, raySize, controller.collisionMask);
                rays.Add(new DrawRayInfo(originPos, originPos + (rayDirection * raySize), isGreen ? new Vector4(0, 1, 0, 1) : new Vector4(1, 1, 0, 1)));
                collisionPoints.Add(hit);
                originPositions.Add(originPos);
                wasHit |= hit ? true : false;
            }

            if(wasHit && loopNum == 0)
            {
                Debug.Log("was hit: " + wasHit);
                nextToWall = true;
            }

            // if there was a collision, get ray exit points on other side of object
            float maxDist = 0.0f;
            //Debug.Log("collision num: " + collisionPoints.Count);
            for( int i = 0; i < collisionPoints.Count; ++i)
            {
                RaycastHit2D rayHit = collisionPoints[i];
                if (rayHit)
                {
                    Vector2 otherSide = GetExitPoint(rayHit, rayDirection);
                    float dist = Vector2.Distance(originPositions[i], otherSide);
                    if (dist >= maxDist)
                    {
                        maxDist = dist;
                    }
                    //Debug.Log("Intercept Point: " + otherSide);
                    rays.Add(new DrawRayInfo(rayHit.point, otherSide, isBlue ? new Vector4(.5f, .5f, 1, 1) : new Vector4(1, 0, 0, 1)));
                }
            }
            distToCurrentOrigin += maxDist + 0.1f;
            raySize = lengthOfChar;
            //Debug.Log("dist to Origin: " + distToCurrentOrigin);

            isBlue = !isBlue;
            isGreen = !isGreen;
            ++loopNum;
        }

        // if no wall hit, teleport to new location
        if (collisionPoints.Count > 0)
        {
            canTeleport = false;
            if (nextToWall) {
                //velocity += new Vector3(moveSpeed * rayDirection.x, moveSpeed * rayDirection.y, 0.0f);
                transform.position = transform.position + new Vector3(rayDirection.x * (distToCurrentOrigin + lengthOfChar), rayDirection.y * (distToCurrentOrigin + lengthOfChar), 0.0f);
            }
        }
    }

    Vector2 GetExitPoint(RaycastHit2D rayHit, Vector2 rayDirection)
    {
        Bounds bounds = rayHit.collider.bounds;
        float slope = rayDirection.y / rayDirection.x;
        float threshold = 0.01f;

        // handle case when hit on the left
        if (Mathf.Abs(rayHit.point.x - bounds.min.x) <= threshold)
        {
            // solve for the intersect point on the right side
            float yIntersect = slope * (bounds.max.x - rayHit.point.x) + rayHit.point.y;
            if (yIntersect >= bounds.min.y && yIntersect <= bounds.max.y)
            {
                return new Vector2(bounds.max.x, yIntersect);
            }
            else
            {
                // solve for x intercept on top or bottom side
                if (rayDirection.y >= 0)
                {
                    float xPos = rayHit.point.x + (1 / slope) * (bounds.max.y - rayHit.point.y);
                    return new Vector2(xPos, bounds.max.y);
                }
                else
                {
                    float xPos = rayHit.point.x + (1 / slope) * (bounds.min.y - rayHit.point.y);
                    return new Vector2(xPos, bounds.min.y);
                }
            }
        }
        else if (Mathf.Abs(rayHit.point.x - bounds.max.x) <= threshold)
        {
            // solve for the intersect point on the left side
            float yIntersect = slope * (bounds.min.x - rayHit.point.x) + rayHit.point.y;
            if (yIntersect >= bounds.min.y && yIntersect <= bounds.max.y)
            {
                return new Vector2(bounds.min.x, yIntersect);
            }
            else
            {
                // solve for x intercept on top or bottom side
                if (rayDirection.y >= 0)
                {
                    float xPos = rayHit.point.x + (1 / slope) * (bounds.max.y - rayHit.point.y);
                    return new Vector2(xPos, bounds.max.y);
                }
                else
                {
                    float xPos = rayHit.point.x + (1 / slope) * (bounds.min.y - rayHit.point.y);
                    return new Vector2(xPos, bounds.min.y);
                }
            }
        }
        else if (Mathf.Abs(rayHit.point.y - bounds.max.y) <= threshold)
        {
            // check for intercept through bottom
            float xIntersect = rayHit.point.x - (1 / slope) * (bounds.size.y);
            if (xIntersect >= bounds.min.x && xIntersect <= bounds.max.x)
            {
                return new Vector2(xIntersect, bounds.min.y);
            }
            else
            {
                if (rayDirection.x >= 0)
                {
                    float yIntersect = rayHit.point.y + slope * (bounds.max.x - rayHit.point.x);
                    return new Vector2(bounds.max.x, yIntersect);
                }
                else
                {
                    float yIntersect = rayHit.point.y + slope * (bounds.min.x - rayHit.point.x);
                    return new Vector2(bounds.min.x, yIntersect);
                }
            }
        }
        else
        {
            // check for intercept through top
            float xIntersect = rayHit.point.x + (1 / slope) * (bounds.size.y);
            if (xIntersect >= bounds.min.x && xIntersect <= bounds.max.x)
            {
                return new Vector2(xIntersect, bounds.max.y);
            }
            else
            {
                if (rayDirection.x >= 0)
                {
                    float yIntersect = rayHit.point.y + slope * (bounds.max.x - rayHit.point.x);
                    return new Vector2(bounds.max.x, yIntersect);
                }
                else
                {
                    float yIntersect = rayHit.point.y + slope * (bounds.min.x - rayHit.point.x);
                    return new Vector2(bounds.min.x, yIntersect);
                }
            }
        }
    }

    public void UpdatTeleportCastOrigins()
    {
        Bounds bounds = controller.collider.bounds;
        bounds.Expand(-teleportSkinWidth * 2);

        teleportRayOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        teleportRayOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        teleportRayOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        teleportRayOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    void teleportThroughWall(Vector2 rayDirection, float rayLength, RaycastHit2D hit)
    {


    }
}
