using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Keys : MonoBehaviour
{
    private PlayerStep player;
    [SerializeField] public Animator animator;
    public int keyIndex = 0;
    public string keyColor = "nothing";
    private Vector2 posToGo = Vector2.zero;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float snapDistance = 0.13f;
    private bool hasReachedTarget = false;

    void Start()
    {
        player = FindObjectOfType<PlayerStep>();
    }

    void Update()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (player.keys == 0)
        {
            Destroy(gameObject);
        }

        // Calculate target position
        if (player.keys == 1)
        {
            posToGo = new Vector2(player.transform.position.x, player.transform.position.y + 0.51f);
            player.keyColor1 = keyColor;

            if (keyIndex > 1)
            {
                Destroy(gameObject);
            }
        }
        else if (player.keys == 2)
        {
            if (keyIndex == 1)
            {
                posToGo = new Vector2(player.transform.position.x - 0.078125f, player.transform.position.y + 0.51f);
                player.keyColor1 = keyColor;
            }
            else if (keyIndex == 2)
            {
                posToGo = new Vector2(player.transform.position.x + 0.078125f, player.transform.position.y + 0.51f);
                player.keyColor2 = keyColor;
            }
            if (keyIndex == 3)
            {
                Destroy(gameObject);
            }
        }
        else if (player.keys == 3)
        {
            if (keyIndex == 1)
            {
                posToGo = new Vector2(player.transform.position.x - 0.15625f, player.transform.position.y + 0.51f);
                player.keyColor1 = keyColor;
            }
            else if (keyIndex == 2)
            {
                posToGo = new Vector2(player.transform.position.x, player.transform.position.y + 0.51f);
                player.keyColor2 = keyColor;
            }
            else if (keyIndex == 3)
            {
                posToGo = new Vector2(player.transform.position.x + 0.15625f, player.transform.position.y + 0.51f);
                player.keyColor3 = keyColor;
            }
        }

        // Move towards or snap to player position
        if (posToGo != Vector2.zero)
        {
            if (!hasReachedTarget)
            {
                transform.position = Vector2.Lerp(transform.position, posToGo, moveSpeed * Time.deltaTime);

                if (Vector2.Distance(transform.position, posToGo) < snapDistance)
                {
                    hasReachedTarget = true;
                    transform.position = posToGo;
                }
            }
            else
            {
                transform.position = posToGo;
            }
        }

        // Handle animation
        if (keyColor == "red")
            animator.Play("RedKey");
        else if (keyColor == "blue")
            animator.Play("BlueKey");
        else if (keyColor == "yellow")
            animator.Play("YellowKey");
        else if (keyColor == "Gray")
            animator.Play("GrayKey");
    }
}