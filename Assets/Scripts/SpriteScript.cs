using UnityEngine;
using UnityEngine.Events;

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