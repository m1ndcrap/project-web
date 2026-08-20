using UnityEngine;


/// <summary>
/// A collected key that floats up and attaches to the player's HUD once picked up (granted by
/// whatever enemy or pickup logic sets <see cref="PlayerStep.keys"/> and assigns this key's
/// <see cref="keyIndex"/>/<see cref="keyColor"/>). Positions itself based on how many keys the
/// player currently holds, so a 2nd or 3rd key slots in next to the first instead of overlapping it.
/// </summary>


public class Keys : MonoBehaviour
{
    private PlayerStep player;


    [Tooltip("The Animator that shows this key's colored sprite.")]
    [SerializeField] public Animator animator;


    [Tooltip("Which HUD slot this key occupies once the player has multiple keys (1st, 2nd, or 3rd).")]
    public int keyIndex = 0;


    [Tooltip("This key's color. Case-insensitive, so \"Red\", \"red\", and \"RED\" all work. Recognized colors: red, blue, yellow, gray.")]
    public string keyColor = "nothing";


    [Header("Movement")]
    [Tooltip("How quickly the key eases toward its HUD position.")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("Once within this distance of its target position, the key snaps exactly into place instead of continuing to ease in.")]
    [SerializeField] private float snapDistance = 0.13f;


    private Vector2 posToGo = Vector2.zero;
    private bool hasReachedTarget = false;




    private void Start()
    {
        player = FindObjectOfType<PlayerStep>();
    }




    private void Update()
    {
        // The player used this key (or all keys were consumed), nothing left for it to do
        if (player.keys == 0)
        {
            Destroy(gameObject);
            return;
        }

        UpdateTargetPosition();
        MoveTowardTarget();
        PlayColorAnimation();
    }




    /// <summary>
    /// Figures out where this key should sit in the HUD based on how many keys the player currently 
    /// holds and this key's own <see cref="keyIndex"/>, and reports its color to the player for the 
    /// matching door check.
    /// </summary>
    private void UpdateTargetPosition()
    {
        if (player.keys == 1)
        {
            posToGo = new Vector2(player.transform.position.x, player.transform.position.y + 0.51f);
            player.keyColor1 = keyColor;

            if (keyIndex > 1)
                Destroy(gameObject);
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
                Destroy(gameObject);
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
    }




    /// <summary>
    /// Eases toward <see cref="posToGo"/>, snapping exactly into place once close enough.
    /// </summary>
    private void MoveTowardTarget()
    {
        if (posToGo == Vector2.zero)
            return;

        if (hasReachedTarget)
        {
            transform.position = posToGo;
            return;
        }

        transform.position = Vector2.Lerp(transform.position, posToGo, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, posToGo) < snapDistance)
        {
            hasReachedTarget = true;
            transform.position = posToGo;
        }
    }




    /// <summary>
    /// Plays the Animator clip matching this key's color.
    /// </summary>
    private void PlayColorAnimation()
    {
        if (string.Equals(keyColor, "red", System.StringComparison.OrdinalIgnoreCase))
            animator.Play("RedKey");
        else if (string.Equals(keyColor, "blue", System.StringComparison.OrdinalIgnoreCase))
            animator.Play("BlueKey");
        else if (string.Equals(keyColor, "yellow", System.StringComparison.OrdinalIgnoreCase))
            animator.Play("YellowKey");
        else if (string.Equals(keyColor, "gray", System.StringComparison.OrdinalIgnoreCase))
            animator.Play("GrayKey");
    }
}