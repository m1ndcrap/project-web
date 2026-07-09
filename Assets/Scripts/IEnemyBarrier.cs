using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemyBarrier
{
    bool IsSolidToPlayer { get; }
    Collider2D BarrierCollider { get; }
    void NudgeAway(float dir); // dir: -1 = push left, 1 = push right
}