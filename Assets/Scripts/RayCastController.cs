using UnityEngine;
using System.Collections;

[RequireComponent (typeof(BoxCollider2D))]
public class RayCastController : MonoBehaviour {

    public RayCastOrigins rayCastOrigins;
    public LayerMask collisionMask; 
    protected float skinWidth = 0.015f;
    protected float rayVerticalOffset, rayHorizontalOffset;
    protected int numVerticalRays = 2, numHorizontalRays = 2;
    public BoxCollider2D collider;

    public struct RayCastOrigins
    {
        public Vector2 topLeft, topRight, bottomLeft, bottomRight;
    }

    public virtual void Awake(){
        collider = GetComponent<BoxCollider2D>();
    }

	// Use this for initialization
    public virtual void Start()
    {
        CalculateRaySpacing();
	}

    public void UpdateRayCastOrigins()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(-skinWidth * 2);

        rayCastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        rayCastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        rayCastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        rayCastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    public void CalculateRaySpacing()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(-skinWidth * 2);

        // make sure there are at least 2 rays
        Mathf.Clamp(numHorizontalRays, 2, Mathf.Infinity);
        Mathf.Clamp(numVerticalRays, 2, Mathf.Infinity);

        rayVerticalOffset = bounds.size.y / (numVerticalRays - 1);
        rayHorizontalOffset = bounds.size.x / (numHorizontalRays - 1);
    }
}
