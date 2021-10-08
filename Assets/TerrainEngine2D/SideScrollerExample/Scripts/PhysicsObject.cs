using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

//Custom Physics Object
//Developed following Unity tutorial:
//https://learn.unity.com/tutorial/live-session-2d-platformer-character-controller

namespace TerrainEngine2D.Physics
{
    /// <summary>
    /// An object that uses custom physics
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PhysicsObject : MonoBehaviour
    {
        protected const float MinMoveDistance = 0.001f;
        protected const int MaxNumberColliderHits = 16;
        protected const float ColliderPadding = 0.01f;
        protected const float MinFluidWeight = 0.1f;

        protected World world;
        protected FluidDynamics fluidDynamics;

        protected Rigidbody2D body;
        protected ContactFilter2D contactFilter;
        protected RaycastHit2D[] hits;

        protected Vector2 velocity;
        /// <summary>
        /// The current speed and direction of the object
        /// </summary>
        public Vector2 Velocity
        {
            get { return velocity; }
        }
        protected Vector2 targetVelocity;
        /// <summary>
        /// The target speed and direction of the object
        /// </summary>
        public Vector2 TargetVelocity
        {
            get { return velocity; }
        }

        protected bool grounded;
        /// <summary>
        /// Whether the object is touching the ground
        /// </summary>
        public bool Grounded
        {
            get { return grounded; }
        }

        protected bool inFluid;
        /// <summary>
        /// Whether the object is in a fluid
        /// </summary>
        public bool InFluid
        {
            get { return inFluid; }
        }

        protected Vector2Int gridPosition;
        /// <summary>
        /// The current position of the object in grid units
        /// </summary>
        public Vector2Int GridPosition
        {
            get { return gridPosition; }
        }
        [Header("Physics Properties")]
        [SerializeField]
        [Tooltip("The amount to scale gravity on this physics object")]
        protected float gravityScale = 1;

        [SerializeField]
        [Tooltip("The amount of upward force applied by fluid blocks")]
        protected float fluidBuoyancy = 0;

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        protected virtual void Start()
        {
            world = World.Instance;
            fluidDynamics = FluidDynamics.Instance;
            hits = new RaycastHit2D[MaxNumberColliderHits];
            contactFilter.useTriggers = false;
            contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            contactFilter.useLayerMask = true;
        }

        protected virtual void Update()
        {
            if (gridPosition.x > 0 && gridPosition.y > 0 && gridPosition.x < world.WorldWidth && gridPosition.y < world.WorldHeight)
            {
                inFluid = fluidDynamics.GetFluidBlock((int)gridPosition.x, (int)gridPosition.y).Weight > MinFluidWeight;
            }
            CalculateTargetVelocity();
        }

        protected virtual void FixedUpdate()
        {
            grounded = false;
            //Apply gravity 
            velocity += Physics2D.gravity * (gravityScale * Time.deltaTime);
            //Apply fluid buoyancy
            if(inFluid)
                velocity += Vector2.up * (fluidBuoyancy * Time.deltaTime);
            //Set horizontal movement
            velocity.x = targetVelocity.x;

            Move(velocity.y * Time.deltaTime, true);
            Move(velocity.x * Time.deltaTime, false);

            gridPosition = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
        }

        /// <summary>
        /// Move the physics object
        /// </summary>
        /// <param name="distance">The distance to move the object by</param>
        /// <param name="vertical">Whether this is vertical or horizontal movement</param>
        protected virtual void Move(float distance, bool vertical)
        {
            Vector2 movement = Vector2.zero;
            if (vertical)
                movement.y = distance;
            else
                movement.x = distance;
            distance = Mathf.Abs(distance);
            //Collision checking
            if (distance > MinMoveDistance)
            {
                int hitCount = body.Cast(movement, contactFilter, hits, distance + ColliderPadding);
                for (int i = 0; i < hitCount; i++)
                    HandleCollision(hits[i], ref distance, ref movement, vertical);
            }
            //Move the body in the direction of the movement at the modified distance
            body.position += movement.normalized * distance;
        }

        /// <summary>
        /// Handle a potential collision with another GameObject
        /// </summary>
        /// <param name="hit">Information about the collision hit</param>
        /// <param name="distance">The distance the object will move</param>
        /// <param name="movement">The distance and direction the object will move</param>
        /// <param name="vertical">Whether this is vertical or horizontal movement</param>
        protected virtual void HandleCollision(RaycastHit2D hit, ref float distance, ref Vector2 movement, bool vertical)
        {
            Vector2 hitSurfaceNormal = hit.normal;
            //Set the object as grounded if standing on a flat surface
            if (hitSurfaceNormal == Vector2.up)
                grounded = true;
            //Cancel out the portion of velocity that would be stopped by the collision
            float projectedVelocity = Vector2.Dot(velocity, hitSurfaceNormal);
            if (projectedVelocity < 0)
                velocity -= projectedVelocity * hitSurfaceNormal;
            //Prevent the object from moving into a collider
            float hitDistance = hit.distance - ColliderPadding;
            if (hitDistance < distance)
                distance = hitDistance;
        }

        /// <summary>
        /// Calculate the target velocity
        /// Controls the objects movement
        /// </summary>
        protected virtual void CalculateTargetVelocity()
        {
            targetVelocity = Vector2.zero;
        }
    }
}

