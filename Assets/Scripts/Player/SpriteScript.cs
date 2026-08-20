using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// Controls animation event for the player sprite. This script is attached to the player sprite and is
/// responsible for triggering events when certain animation events occur, such as hitting an enemy or 
/// performing a swing kick. It communicates with the PlayerStep component to handle these events appropriately.
/// </summary>


public class SpriteScript : MonoBehaviour
{
    [SerializeField] private PlayerStep player;
    public UnityEvent<RobotStep> OnHit;


    public void HitEvent()
    {
        player.HitEvent();
    }


    public void SwingKickHitEvent()
    {
        player.SwingKickHitEvent();
    }


    public void PauseBeforeHit()
    {
        player.PauseBeforeHit();
    }
}