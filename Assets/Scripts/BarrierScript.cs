using UnityEngine;
public class BarrierScript : MonoBehaviour
{
    [SerializeField] private GameObject circleEffectPrefab;
    private Vector2 lastSpawnPos;
    private float spawnMoveThreshold = 0.05f;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        Vector2 contact = GetContactPoint(collision);
        SpawnCircle(contact);
        lastSpawnPos = contact;
        EnforceGravity(collision.gameObject);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        EnforceGravity(collision.gameObject);
        HandleStay(collision.gameObject, GetContactPoint(collision));
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        PlayerStep player = collision.gameObject.GetComponent<PlayerStep>();
        if (player != null) player.againstBarrier = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Vector2 contact = other.ClosestPoint(transform.position);
        SpawnCircle(contact);
        lastSpawnPos = contact;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        HandleStay(other.gameObject, other.ClosestPoint(transform.position));
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerStep player = other.GetComponent<PlayerStep>();
        if (player != null) player.againstBarrier = false;
    }

    private void HandleStay(GameObject playerObj, Vector2 contact)
    {
        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
        PlayerStep player = playerObj.GetComponent<PlayerStep>();
        if (playerRb == null || player == null) return;

        float dx = playerObj.transform.position.x - transform.position.x;
        bool movingIntoBarrier = (dx < 0 && playerRb.velocity.x > 0.1f) || (dx > 0 && playerRb.velocity.x < -0.1f);

        player.againstBarrier = movingIntoBarrier;
        if (!movingIntoBarrier) return;

        playerRb.velocity = new Vector2(0f, playerRb.velocity.y);

        if (Vector2.Distance(contact, lastSpawnPos) >= spawnMoveThreshold)
        {
            SpawnCircle(contact);
            lastSpawnPos = contact;
        }
    }

    void SpawnCircle(Vector2 pos)
    {
        if (circleEffectPrefab == null) return;
        Instantiate(circleEffectPrefab, new Vector3(pos.x, pos.y, transform.position.z), Quaternion.identity);
    }

    Vector2 GetContactPoint(Collision2D collision)
    {
        return collision.contacts.Length > 0 ? collision.contacts[0].point : (Vector2)transform.position;
    }

    void EnforceGravity(GameObject playerObj)
    {
        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
        if (playerRb != null) playerRb.gravityScale = 1f;
    }
}