using UnityEngine;


/// <summary>
/// Defines an interface to handle collisions between player and enemy to prevent either from walking/running through the other...
/// </summary>


public interface IEnemyBarrier
{
    bool IsSolidToPlayer { get; }


    Collider2D BarrierCollider { get; }


    void NudgeAway(float dir); // dir: -1 = push left, 1 = push right
}