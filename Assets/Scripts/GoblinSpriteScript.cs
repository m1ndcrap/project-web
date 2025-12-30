using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GoblinSpriteScript : MonoBehaviour
{
    [SerializeField] private GoblinStep enemy;
    public UnityEvent<PlayerStep> OnAttack;

    public void AttackEvent()
    {
        enemy.AttackEvent();
    }
}