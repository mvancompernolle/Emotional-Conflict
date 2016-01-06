using UnityEngine;
using System.Collections;

public class Controller2D : RayCastController {

    public CollisionInfo collisions;

    public struct CollisionInfo
    {
        public bool above, below, left, right;
        public int faceDir;

        public void Reset()
        {
            above = below = left = right = false;
        }
    }

	// Use this for initialization
	public override void Start () {
	    base.Start();
        collisions.faceDir = 1;
	}
	
    public void Move(Vector3 velocity){
        UpdateRayCastOrigins();
        collisions.Reset();

        if (velocity.x != 0)
        {
            collisions.faceDir = (int) Mathf.Sign(velocity.x);
        }
        HorizontalCollisions(ref velocity);
        if (velocity.y != 0.0f)
        {
            VerticalCollisions(ref velocity);
        }
        transform.Translate(velocity);
    }

    public void HorizontalCollisions(ref Vector3 velocity){
        float dirX = collisions.faceDir;
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        if (Mathf.Abs(velocity.x) < skinWidth)
        {
            rayLength = 2 * skinWidth;
        }

        for (int i = 0; i < numHorizontalRays; ++i)
        {
            Vector2 rayOrigin = (dirX == -1) ? rayCastOrigins.bottomLeft : rayCastOrigins.bottomRight;
            rayOrigin += Vector2.up * (i * rayHorizontalOffset);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * dirX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * dirX * rayLength, Color.blue);

            if (hit)
            {
                velocity.x = (hit.distance - skinWidth) * dirX;
                rayLength = hit.distance;
                collisions.left = dirX == -1;
                collisions.right = dirX == 1;
            }
        }
    }

    public void VerticalCollisions(ref Vector3 velocity)
    {
        float dirY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;

        for (int i = 0; i < numVerticalRays; ++i)
        {
            Vector2 rayOrigin = (dirY == -1) ? rayCastOrigins.bottomLeft : rayCastOrigins.topLeft;
            rayOrigin += Vector2.right * (i * rayVerticalOffset + velocity.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * dirY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * dirY * rayLength, Color.blue);

            if (hit)
            {
                velocity.y = (hit.distance - skinWidth) * dirY;
                rayLength = hit.distance;
                collisions.below = dirY == -1;
                collisions.above = dirY == 1;
            }
        }
    }
}
