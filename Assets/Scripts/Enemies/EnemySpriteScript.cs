using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// Provides functionality for handling enemy sprite actions and attack events within a Unity scene.
/// </summary>


public class EnemySpriteScript : MonoBehaviour
{
    [SerializeField] private RobotStep enemy;
    public UnityEvent<PlayerStep> OnAttack;


    public void AttackEvent()
    {
        enemy.AttackEvent();
    }
}