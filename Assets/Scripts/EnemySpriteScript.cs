using UnityEngine;
using UnityEngine.Events;

public class EnemySpriteScript : MonoBehaviour
{
    [SerializeField] private RobotStep enemy;
    public UnityEvent<PlayerStep> OnAttack;

    public void AttackEvent()
    {
        enemy.AttackEvent();
    }
}