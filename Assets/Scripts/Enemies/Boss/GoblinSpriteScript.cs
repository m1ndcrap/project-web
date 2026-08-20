using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// Provides functionality for controlling a goblin enemy sprite and raising attack events.
/// </summary>


public class GoblinSpriteScript : MonoBehaviour
{
    [SerializeField] private GoblinStep enemy;
    public UnityEvent<PlayerStep> OnAttack;


    public void AttackEvent()
    {
        enemy.AttackEvent();
    }
}