using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class MenuPlayer : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] public Animator anim;
    [SerializeField] public SpriteRenderer sprite;
    [SerializeField] private Transform visual;
    private BoxCollider2D coll;
    public float dirX = 0f;
    public bool jumpKey = false;
    public bool swingKey = false;
    public bool swingKeyR = false;
    private float dirY = 0f;

    // Swinging Variables
    private float ropeAngle = 0f;
    private float ropeAngleVelocity = 0f;
    private float ropeX = 0f;
    private float ropeY = 0f;
    private float grappleX = 0f;
    private float grappleY = 0f;
    private float ropeLength = 0f;
    private bool swingEnd = false;
    private float accelerationRate = -0.02f;
    [SerializeField] private GameObject swingPoint;

    [SerializeField] private LayerMask jumpableGround;
    private float hsp = 4f; // Horizontal speed
    private float jspd = 5f;    // Jump speed
    [SerializeField] public GameObject ropeSegmentPrefab; // Assign in Inspector
    private float ropeSegmentLength = 0.15f; // Distance between segments
    private List<GameObject> ropeSegments = new List<GameObject>(); // Track segments
    private Queue<GameObject> ropeSegmentPool = new Queue<GameObject>();
    private int maxPoolSize = 200; // Optional limit

    private enum MovementState { idle, running, jumping, falling, swinging, endswing, crawling, zip, groundshoot, airshoot, crawlshoot, punch1, punch2, punch3, punch4, airkick, airpunch, kick1, kick2, uppercut, launched, hurt1, hurt2, block1, block2, block3, block4, death }
    public enum PlayerState { normal, swing, crawl, quickzip, dashenemy, hurt, death }
    public PlayerState pState;

    // Sound Files
    [SerializeField] private AudioSource audioSrc;
    [SerializeField] private AudioClip sndJump;
    [SerializeField] private AudioClip sndJump2;
    [SerializeField] private AudioClip sndSwing;
    [SerializeField] private AudioClip sndSwing2;
    [SerializeField] private AudioClip sndSwing3;
    [SerializeField] private AudioClip sndLand;
    [SerializeField] private AudioClip sndLand2;
    [SerializeField] private AudioClip sndHardLand;
    [SerializeField] private AudioClip sndHardLand2;
    [SerializeField] private AudioClip sndWebSnap;
    [SerializeField] private AudioClip sndWebRelease;
    [SerializeField] private AudioClip sndWebTension;
    [SerializeField] private AudioClip sndWebTension2;
    [SerializeField] private AudioClip sndWebTension3;
    [SerializeField] private AudioClip sndWebShoot;
    [SerializeField] private AudioClip sndStep;
    [SerializeField] private AudioClip sndStep2;

    private bool wasGrounded = false;
    private int alarm1 = 0;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<BoxCollider2D>();
        pState = PlayerState.normal;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(1 / Time.unscaledDeltaTime);  // FPS Counter
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (swingEnd && stateInfo.IsName("Player_Swing_End") && stateInfo.normalizedTime >= 1f)
            swingEnd = false;

        if (alarm1 > 0)
        {
            alarm1 -= 1;
        }
        else
        {
            if (pState == PlayerState.swing)
            {
                AudioClip[] clips = { sndWebTension, sndWebTension2, sndWebTension3 };
                AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
                audioSrc.PlayOneShot(randomClip);
                alarm1 = 400;
            }
        }

        switch (pState)
        {
            case PlayerState.normal:
            {
                visual.rotation = Quaternion.Euler(0, 0, 0);

                bool movementKey = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
                bool anyKey = Input.anyKey;
                bool otherKeyPressed = anyKey && !movementKey;

                if (!otherKeyPressed)
                {
                    coll.size = new Vector2(0.8397379f, 1.615343f);
                    coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                }

                rb.velocity = new Vector2(dirX * hsp, rb.velocity.y);    // Moving character based on left or right arrow key

                if (jumpKey && Grounded())      // Jump code
                {
                    AudioClip[] clips = { sndJump, sndJump2 };
                    int index = UnityEngine.Random.Range(0, clips.Length + 1); // +1 to include "no sound"

                    if (index < clips.Length)
                        audioSrc.PlayOneShot(clips[index]);

                    rb.velocity = new Vector2(rb.velocity.x, jspd);
                }

                if (swingKey && !Grounded())      // Swing code
                {
                    rb.gravityScale = 0;
                    grappleX = swingPoint.transform.position.x;
                    grappleY = swingPoint.transform.position.y;
                    ropeX = transform.position.x;
                    ropeY = transform.position.y;
                    ropeAngleVelocity = 0f;
                    ropeAngle = Mathf.Atan2(ropeY - grappleY, ropeX - grappleX) * Mathf.Rad2Deg;
                    ropeLength = Vector2.Distance(new Vector2(grappleX, grappleY), new Vector2(ropeX, ropeY));
                    coll.size = new Vector2(1.339648f, 1.561783f);
                    coll.offset = new Vector2(-0.6135812f, -0.6907219f);
                    AudioClip[] clips = { sndSwing, sndSwing2, sndSwing3 };
                    AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
                    audioSrc.PlayOneShot(randomClip);
                    alarm1 = 400;
                    swingEnd = false;
                    pState = PlayerState.swing;
                }

                if (!wasGrounded && Grounded() && pState == PlayerState.normal) // Landing Sound Code
                {
                    float fallSpeed = rb.velocity.y;

                    if (fallSpeed < -10f)
                    {
                        AudioClip[] clips = { sndHardLand, sndHardLand2 };
                        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
                        audioSrc.PlayOneShot(randomClip);
                    }
                    else
                    {
                        AudioClip[] clips = { sndLand, sndLand2 };
                        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
                        audioSrc.PlayOneShot(randomClip);
                    }
                }

                wasGrounded = Grounded();
            }
            break;

            case PlayerState.swing:
            {
                float ropeAngleAcceleration = accelerationRate * Mathf.Cos(ropeAngle * Mathf.Deg2Rad); //-0.02
                ropeAngleAcceleration += dirX * 0.04f;
                ropeLength += dirY * 0.01f;
                ropeLength = Mathf.Max(ropeLength, 0f);
                ropeAngleVelocity += ropeAngleAcceleration;
                ropeAngle += ropeAngleVelocity;
                ropeAngleVelocity *= 0.99f;
                ropeX = grappleX + Mathf.Cos(ropeAngle * Mathf.Deg2Rad) * ropeLength;
                ropeY = grappleY + Mathf.Sin(ropeAngle * Mathf.Deg2Rad) * ropeLength;

                rb.MovePosition(new Vector2(ropeX, ropeY));

                Vector2 ropeDirection = new Vector2(ropeX - grappleX, ropeY - grappleY).normalized;
                float ropeAngleDeg = Mathf.Atan2(ropeDirection.y, ropeDirection.x) * Mathf.Rad2Deg;

                visual.rotation = Quaternion.Euler(0, 0, ropeAngleDeg + 90); // Apply rotation to visual so it faces along the rope

                if (swingKeyR)
                {
                    rb.velocity = new Vector2(rb.velocity.x, jspd);
                    rb.gravityScale = 1;

                    MovementState mstate;
                    mstate = MovementState.endswing;
                    anim.SetInteger("mstate", (int)mstate);

                    coll.size = new Vector2(0.8397379f, 1.615343f);
                    coll.offset = new Vector2(-0.03511286f, -0.03012538f);

                    audioSrc.PlayOneShot(sndWebRelease);

                    pState = PlayerState.normal;

                    swingEnd = true;

                    ReturnAllRopeSegmentsToPool(); // Destroy old rope segments
                }

                bool onGround = Grounded();

                if (onGround)
                {
                    visual.rotation = Quaternion.Euler(0, 0, 0);
                    coll.size = new Vector2(0.8397379f, 1.615343f);
                    coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                    audioSrc.PlayOneShot(sndWebSnap);
                    pState = PlayerState.normal;
                    ReturnAllRopeSegmentsToPool();
                    rb.gravityScale = 1;
                }
            }
            break;
        }

        if (pState == PlayerState.swing)
            DrawRope(new Vector2(grappleX, grappleY), new Vector2(ropeX, ropeY));

        UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        if (dirX > 0f)
            sprite.flipX = false;
        else if (dirX < 0f)
            sprite.flipX = true;

        if (swingEnd) return; // Let Animator handle transitioning after animation finishes
        if (pState == PlayerState.dashenemy) return;
        if (pState == PlayerState.hurt) return;

        MovementState mstate = MovementState.idle;

        if (pState == PlayerState.normal)
        {
            anim.speed = 1f; // Normal animation speed

            if (dirX > 0f)                  // Controlling running animation by controlling boolean variable responsible for triggering running animation based on horizontal speed
                mstate = MovementState.running;
            else if (dirX < 0f)
                mstate = MovementState.running;
            else
                mstate = MovementState.idle;

            if (rb.velocity.y > 0.1f)
                mstate = MovementState.jumping;
            else if (rb.velocity.y < -0.1f)
                mstate = MovementState.falling;
        }
        else if (pState == PlayerState.swing)
        {
            mstate = MovementState.swinging;
            anim.speed = 1f; // Normal animation speed
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        float normalizedTime = stateInfo.normalizedTime % 1f;

        if (mstate == MovementState.running)
        {
            if (normalizedTime >= 0.35f && normalizedTime <= 0.38f)
                audioSrc.PlayOneShot(sndStep2);

            if (normalizedTime >= 0.83f && normalizedTime <= 0.86f)
                audioSrc.PlayOneShot(sndStep);
        }

        anim.SetInteger("mstate", (int)mstate);
    }

    public bool Grounded()
    {
        return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, 0.1f, jumpableGround);
    }

    void DrawRope(Vector2 start, Vector2 end)
    {
        ReturnAllRopeSegmentsToPool();

        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);
        int segmentCount = Mathf.CeilToInt(distance / ropeSegmentLength);

        for (int i = 0; i < segmentCount; i++)
        {
            Vector2 position = start + direction * ropeSegmentLength * i;
            GameObject seg = GetRopeSegment(position);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            seg.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    GameObject GetRopeSegment(Vector2 position)
    {
        GameObject segment;

        if (ropeSegmentPool.Count > 0)
        {
            segment = ropeSegmentPool.Dequeue();
            segment.SetActive(true);
        }
        else
        {
            segment = Instantiate(ropeSegmentPrefab);
        }

        segment.transform.position = position;
        ropeSegments.Add(segment);
        return segment;
    }

    void ReturnAllRopeSegmentsToPool()
    {
        foreach (var seg in ropeSegments)
        {
            if (ropeSegmentPool.Count < maxPoolSize)
            {
                seg.SetActive(false);
                ropeSegmentPool.Enqueue(seg);
            }
            else
            {
                Destroy(seg); // If we exceed the pool, just clean up
            }
        }

        ropeSegments.Clear();
    }
}