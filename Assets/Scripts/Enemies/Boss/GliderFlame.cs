using UnityEngine;


/// <summary>
/// Controls the visual representation and positioning of the flame effect attached to a glider object in the scene.
/// </summary>


public class GliderFlame : MonoBehaviour
{
    [SerializeField] private GameObject glider;
    private SpriteRenderer sprite;




    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
    }




    void Update()
    {
        SpriteRenderer gliderSprite = glider.GetComponent<SpriteRenderer>();

        if (gliderSprite.flipX)
        {
            sprite.flipX = false;
            transform.position = new Vector3(glider.transform.position.x + 0.477f, glider.transform.position.y + 0.0477f, glider.transform.position.z);
        }
        else
        {
            sprite.flipX = true;
            transform.position = new Vector3(glider.transform.position.x - 0.477f, glider.transform.position.y + 0.0477f, glider.transform.position.z);
        }
    }
}