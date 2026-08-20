using UnityEngine;


/// <summary>
/// Manages player interactions with a barrier in the scene, including detecting contact, enforcing movement
/// restrictions, and spawning visual effects when the player pushes against the barrier.
/// </summary>


public class BarrierScript : MonoBehaviour
{
    [SerializeField] private GameObject circleEffectPrefab;


    [Tooltip("Extra distance around this barrier that still counts as the player touching it. Must be larger than the gap PlayerStep leaves when it stops the player, which is about 0.05.")]
    private float contactMargin = 0.15f;


    // Shared by every barrier so the player is only searched for once, not once per barrier.
    private static PlayerStep player;


    private Collider2D barrierCollider;

    // True while the player is mid-push. A circle spawns only on the frame this turns true, which keeps it to one per push instead of one per frame.
    private bool pushActive = false;




    void Awake()
    {
        barrierCollider = GetComponent<Collider2D>();
    }




    void Update()
    {
        // Deliberately not driven by collision callbacks. PlayerStep stops the player a fraction of
        // a unit short of the barrier, so they often never physically touch it and no collision
        // event ever fires. Checking positions directly works whether or not contact happens.
        if (player == null)
        {
            player = FindObjectOfType<PlayerStep>();
            if (player == null) return;
        }


        if (player.coll == null) return;


        Bounds barrier = barrierCollider.bounds;
        Bounds playerBounds = player.coll.bounds;


        bool touching = playerBounds.max.x > barrier.min.x - contactMargin && playerBounds.min.x < barrier.max.x + contactMargin && playerBounds.max.y > barrier.min.y - contactMargin && playerBounds.min.y < barrier.max.y + contactMargin;

        if (!touching)
        {
            pushActive = false;
            return;
        }


        // Which side is the player on, and check if they are holding a direction into the barrier
        float dx = playerBounds.center.x - barrier.center.x;
        bool pushing = (dx < 0 && player.dirX > 0f) || (dx > 0 && player.dirX < 0f);

        if (pushing && !pushActive)
        {
            float edgeX = dx < 0 ? barrier.min.x : barrier.max.x;
            SpawnCircle(new Vector2(edgeX, playerBounds.center.y));
        }

        pushActive = pushing;
    }




    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        EnforceGravity(collision.gameObject);
    }




    void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        EnforceGravity(collision.gameObject);
        StopHorizontalMovement(collision.gameObject);
    }




    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        StopHorizontalMovement(other.gameObject);
    }




    /// <summary>
    /// Stops the horizontal movement of the specified player object by setting its horizontal velocity to zero.
    /// </summary>
    /// <param name="playerObj">The player object whose horizontal movement will be stopped. Must have a Rigidbody2D component attached.</param>
    private void StopHorizontalMovement(GameObject playerObj)
    {
        if (!pushActive) return;

        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
        if (playerRb != null) playerRb.velocity = new Vector2(0f, playerRb.velocity.y);
    }




    /// <summary>
    /// Spawns a circular visual effect at the specified position.
    /// </summary>
    /// <param name="pos">The position, in world coordinates, where the circle effect will be instantiated.</param>
    void SpawnCircle(Vector2 pos)
    {
        if (circleEffectPrefab == null) return;
        Instantiate(circleEffectPrefab, new Vector3(pos.x, pos.y, transform.position.z), Quaternion.identity);
    }




    /// <summary>
    /// Ensures that gravity is applied to the specified player object by setting its Rigidbody2D gravity scale to 1.
    /// </summary>
    /// <param name="playerObj">The player object to which gravity should be enforced. Must have a Rigidbody2D component attached.</param>
    void EnforceGravity(GameObject playerObj)
    {
        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
        if (playerRb != null) playerRb.gravityScale = 1f;
    }
}