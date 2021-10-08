using UnityEngine;

namespace TerrainEngine2D.TopDownDemo
{
    /// <summary>
    /// A very simple player controller for testing
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        private World world;
        private Rigidbody2D body;
        [SerializeField]
        private float speed = 20;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            world = World.Instance;
            transform.position = new Vector3(transform.position.x, transform.position.y, world.EndZPoint);
        }

        private void FixedUpdate()
        {
            body.AddForce(new Vector2(Input.GetAxis("Horizontal") * 10 * speed, Input.GetAxis("Vertical") * 10 * speed));
        }

        private void Update()
        {
            Vector3 clampedPosition = transform.position;
            if (transform.position.x < 0 || transform.position.x > world.WorldWidth - 1)
            {
                clampedPosition.x = Mathf.Clamp(clampedPosition.x, 0, world.WorldWidth - 1);
                transform.position = clampedPosition;
            }
            if(transform.position.y < 0 || transform.position.y > world.WorldHeight - 1)
            {
                clampedPosition.y = Mathf.Clamp(clampedPosition.y, 0, world.WorldHeight - 1);
                transform.position = clampedPosition;
            }
        }
    }
}