using UnityEngine;
using TerrainEngine2D.Physics;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D.SideScrollerDemo
{
    /// <summary>
    /// A 2D player controller for traversing blocky terrain
    /// </summary>
    public class PlayerController : PhysicsObject
    {
        private const float HorizontalTerrainHopAmount = 0.05f;

        [Header("Objects and Components")]
        [SerializeField]
        private SpriteRenderer graphicSpriteRenderer;
        [SerializeField]
        private Transform armTransform;

        private Animator animator;
        private BoxCollider2D boxCollider2D;
        private LayerMask terrainLayerMask;

        [Header("Player Properties")]
        [SerializeField]
        [Tooltip("The maximum speed of the player")]
        private float maxSpeed = 10;
        [SerializeField]
        [Tooltip("The amount of vertical force applied to the player when jumping")]
        private float jumpForce = 10;

        private bool jumping;
        public bool Jumping
        {
            get { return jumping; }
        }

        protected override void Awake()
        {
            base.Awake();
            animator = GetComponent<Animator>();
            boxCollider2D = GetComponent<BoxCollider2D>();
            terrainLayerMask = LayerMask.GetMask("Terrain");
        }

        protected override void Start()
        {
            base.Start();
            //Set the z layer order so that the player is directly in front of the Tree layer
            transform.position = new Vector3(transform.position.x, transform.position.y, world.GetBlockLayer((byte)Layers.Tree).ZLayerOrderEnd - world.ZBlockDistance / 4f);
        }

        protected override void Update()
        {
            base.Update();
            //Update the animation parameters
            animator.SetBool("Grounded", grounded);
            animator.SetBool("Jumping", jumping);
            animator.SetFloat("HorizontalVelocity", Mathf.Abs(velocity.x));

            //Rotate the arm to point to the position of the cursor
            Vector3 cursorPositionRelativePlayer = Camera.main.ScreenToWorldPoint(Input.mousePosition) - armTransform.position;
            cursorPositionRelativePlayer.Normalize();
            float angle = Mathf.Atan2(cursorPositionRelativePlayer.y, cursorPositionRelativePlayer.x) * Mathf.Rad2Deg;
            if (graphicSpriteRenderer.transform.localScale.x < 0)
                angle += 180;
            armTransform.eulerAngles = new Vector3(0, 0, angle);
        }

        protected override void CalculateTargetVelocity()
        {
            base.CalculateTargetVelocity();

            float speed = maxSpeed;
            //Slow down the player if in a fluid 
            if (inFluid)
                speed /= 2f;
            //Set the horiontal speed of the player
            targetVelocity.x = Input.GetAxis("Horizontal") * speed;

            //Flip the players graphic to face the direction of horizontal movement
            if (targetVelocity.x != 0)
            {
                if (targetVelocity.x > 0)
                    graphicSpriteRenderer.transform.localScale = new Vector3(1, 1, 1);
                else
                    graphicSpriteRenderer.transform.localScale = new Vector3(-1, 1, 1);
            }

            //Jump if the player is touching the ground and the 'Jump' input button is pressed
            if (grounded && Input.GetButtonDown("Jump"))
            {
                jumping = true;
                //Set the vertical speed to make the player jump
                //If the player is in a fluid only use 3/4 of force
                if(inFluid)
                    velocity.y += jumpForce / 4f * 3;
                else
                    velocity.y += jumpForce;
            }
        }

        protected override void HandleCollision(RaycastHit2D hit, ref float distance, ref Vector2 movement, bool vertical)
        {
            Vector2 hitSurfaceNormal = hit.normal;
            //Set the object as grounded if standing on a flat surface
            if (hitSurfaceNormal == Vector2.up)
            {
                grounded = true;
                jumping = false;
            }
            //Player will try to hop up to the next terrain block (a single grid unit)
            if (!vertical && hit.collider.gameObject.layer == LayerMask.NameToLayer("Terrain") && grounded)
            {
                //The distance and direction the player will move
                Vector2 terrainHop = Vector2.zero;
                //Move up to the next grid unit with padding to ensure the player does not go into another collider
                terrainHop.y = gridPosition.y + 1.0f + ColliderPadding - boxCollider2D.bounds.min.y;

                //Ensure the player moves horizontally onto the next terrain block so that it will not fall back down
                float horizontalMovement = hit.distance + HorizontalTerrainHopAmount;
                if(distance < horizontalMovement)
                    terrainHop.x = horizontalMovement - distance;

                //Check to see if the player can move up to the next terrain block without colliding with anything
                Vector2 direction = Vector2.right;
                //The current position of the player collider with the additional hop in vertical positioning
                Vector2 colliderPosition = body.position + boxCollider2D.offset;
                colliderPosition.y += terrainHop.y;

                if (targetVelocity.x < 0)
                    direction = Vector2.left;

                RaycastHit2D[] hits = Physics2D.BoxCastAll(colliderPosition, boxCollider2D.size, 0, direction, distance + terrainHop.x, contactFilter.layerMask);
                bool noCollision = true;
                foreach (RaycastHit2D tempHit in hits)
                {
                    if (tempHit.collider != null)
                    {
                        if (tempHit.collider.gameObject != gameObject)
                        {
                            noCollision = false;
                            break;
                        }
                    }
                }
                if (noCollision)
                {
                    //Ensure the player will be moving onto a terrain block
                    Vector2 raycastDirection = Vector2.right;
                    if (targetVelocity.x < 0)
                        raycastDirection = Vector2.left;
                    RaycastHit2D hitCheck = Physics2D.Raycast(gridPosition + new Vector2(0.5f, 0.5f), raycastDirection, 1, terrainLayerMask);
                    if (hitCheck.collider != null)
                    {
                        velocity.y = 0;
                        distance += terrainHop.x;
                        body.position += Vector2.up * terrainHop.y;
                        return;
                    }
                }
            }
            base.HandleCollision(hit, ref distance, ref movement, vertical);
        }

    }
}