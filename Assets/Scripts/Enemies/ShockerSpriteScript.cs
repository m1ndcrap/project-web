using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// Provides animation event logic for shocker's sprite, including attack event handling and integration with Unity's event
/// system.
/// </summary>


public class ShockerSpriteScript : MonoBehaviour
{
    [SerializeField] private ShockerStep enemy;
    public UnityEvent<PlayerStep> OnAttack;


    public void AttackEvent()
    {
        enemy.AttackEvent();
    }
}