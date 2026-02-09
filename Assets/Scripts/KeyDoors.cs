using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyDoors : MonoBehaviour
{
    public int phase = 0;
    private bool open = false;
    private Animator anim;
    [SerializeField] private GameObject doorEmpty;
    [SerializeField] private GameObject doorWall;
    private int alarm1 = 0;
    [SerializeField] private string doorColor = "nothing";
    [SerializeField] private GameObject doorPair;
    private bool matchedPair = false;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (doorPair.GetComponent<KeyDoors>().phase != 0 && !matchedPair) { phase = 1; matchedPair = true; }

        if (phase == 0)
        {
            switch (doorColor)
            {
                case "red":
                    anim.Play("RedDoorClosed");
                    break;
                case "blue":
                    anim.Play("BlueDoorClosed");
                    break;
                case "yellow":
                    anim.Play("YellowDoorClosed");
                    break;
                case "gray":
                    anim.Play("GrayDoorClosed");
                    break;
            }
        }

        if (phase == 1 && !open)
        {
            alarm1 = 10;
            open = true;
        }

        if (alarm1 > 0)
            alarm1 -= 1;
        else
        {
            if (phase == 1)
            {
                switch (doorColor)
                {
                    case "red":
                        anim.Play("RedDoorOpening");
                        break;
                    case "blue":
                        anim.Play("BlueDoorOpening");
                        break;
                    case "yellow":
                        anim.Play("YellowDoorOpening");
                        break;
                    case "gray":
                        anim.Play("GrayDoorOpening");
                        break;
                }

                phase = 2;
            }
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        if (phase == 2 && (stateInfo.IsName("RedDoorOpening") || stateInfo.IsName("BlueDoorOpening") || stateInfo.IsName("YellowDoorOpening") || stateInfo.IsName("GrayDoorOpening")) && stateInfo.normalizedTime >= 1f) { phase = 3; }

        if (phase == 3)
        {
            Destroy(doorWall);

            switch (doorColor)
            {
                case "red":
                    anim.Play("RedDoorOpen");
                    break;
                case "blue":
                    anim.Play("BlueDoorOpen");
                    break;
                case "yellow":
                    anim.Play("YellowDoorOpen");
                    break;
                case "gray":
                    anim.Play("GrayDoorOpen");
                    break;
            }
        }
    }
}