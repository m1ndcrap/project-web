using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ShockerSpriteScript : MonoBehaviour
{
    [SerializeField] private ShockerStep enemy;
    public UnityEvent<PlayerStep> OnAttack;

    public void AttackEvent()
    {
        enemy.AttackEvent();
    }
}