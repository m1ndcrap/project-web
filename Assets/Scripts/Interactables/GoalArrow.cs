using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// Controls the behavior of the goal arrow, including its oscillating movement and level completion logic.
/// </summary>


public class GoalArrow : MonoBehaviour
{
    private int phase = 0;
    private float x_off = 0;
    private Vector2 pos;




    void Start()
    {
        pos = transform.position;
    }




    void Update()
    {
        if (phase == 0)
        {
            if (x_off < 10)
            {
                x_off += 1;
            }
            else
            {
                x_off = 10;
                phase = 1;
            }
        }

        if (phase == 1)
        {
            if (x_off > -10)
            {
                x_off -= 1;
            }
            else
            {
                x_off = -10;
                phase = 0;
            }
        }

        transform.position = new Vector2(pos.x + (x_off / 30), pos.y);
    }




    /// <summary>
    /// Handles the logic for completing the level, including loading the title screen.
    /// </summary>
    public void levelComplete()
    {
        SceneManager.LoadScene("Title Screen");
    }
}