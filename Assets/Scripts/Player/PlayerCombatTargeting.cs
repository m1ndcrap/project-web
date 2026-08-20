using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// Owns who the player is allowed to attack or counter, resolving the best melee
/// target among nearby enemies, tracking the scene's boss/shocker singletons, and exposing
/// convenient Transform/Rigidbody2D/Animator accessors for whichever target is currently selected,
/// instead of switching on the target's concrete type (RobotStep, GoblinStep, or ShockerStep) at
/// every call site that needs it.
/// </summary>


public class PlayerCombatTargeting
{
    /// <summary>The deadzone within which the player is considered to be facing a target.</summary>
    private const float FacingDeadzone = 0.15f;


    /// <summary>The scene's single GoblinStep boss instance, if one exists. Found automatically when this class is constructed.</summary>
    public GoblinStep Boss { get; }


    /// <summary>The scene's single ShockerStep instance, if one exists. Found automatically when this class is constructed.</summary>
    public ShockerStep Shocker { get; }


    /// <summary>The enemy currently selected as a melee attack target. A RobotStep, GoblinStep, or ShockerStep, or null if there isn't one.</summary>
    public Component CurrentTarget { get; set; }


    /// <summary>The enemy currently selected as a counter-attack target. A RobotStep, GoblinStep, or ShockerStep, or null if there isn't one.</summary>
    public Component CurrentCounterTarget { get; set; }


    /// <summary>True while a counter target is currently selected.</summary>
    public bool HasCounterTarget => CurrentCounterTarget != null;


    /// <summary>The Transform of <see cref="CurrentTarget"/>, or null if there is none.</summary>
    public Transform TargetTransform => CurrentTarget switch
    {
        RobotStep r => r.transform,
        GoblinStep g => g.transform,
        ShockerStep s => s.transform,
        _ => null
    };


    /// <summary>The Rigidbody2D of <see cref="CurrentTarget"/>, or null if there is none.</summary>
    public Rigidbody2D TargetRB => CurrentTarget switch
    {
        RobotStep r => r.rb,
        GoblinStep g => g.rb,
        ShockerStep s => s.rb,
        _ => null
    };


    /// <summary>The Animator of <see cref="CurrentTarget"/>, or null if there is none.</summary>
    public Animator TargetAnim => CurrentTarget switch
    {
        RobotStep r => r.anim,
        GoblinStep g => g.anim,
        ShockerStep s => s.anim,
        _ => null
    };


    /// <summary>The Transform of <see cref="CurrentCounterTarget"/>, or null if there is none.</summary>
    public Transform CounterTargetTransform => CurrentCounterTarget switch
    {
        RobotStep r => r.transform,
        GoblinStep g => g.transform,
        ShockerStep s => s.transform,
        _ => null
    };


    /// <summary>The Animator of <see cref="CurrentCounterTarget"/>, or null if there is none.</summary>
    public Animator CounterTargetAnim => CurrentCounterTarget switch
    {
        RobotStep r => r.anim,
        GoblinStep g => g.anim,
        ShockerStep s => s.anim,
        _ => null
    };


    /// <summary>The Rigidbody2D of <see cref="CurrentCounterTarget"/>, or null if there is none.</summary>
    public Rigidbody2D CounterTargetRB => CurrentCounterTarget switch
    {
        RobotStep r => r.rb,
        GoblinStep g => g.rb,
        ShockerStep s => s.rb,
        _ => null
    };


    /// <summary>Finds the scene's Boss and Shocker, if present. Safe to call even if the scene has neither.</summary>
    public PlayerCombatTargeting()
    {
        Boss = Object.FindObjectOfType<GoblinStep>();
        Shocker = Object.FindObjectOfType<ShockerStep>();
    }


    /// <summary>True while <see cref="CurrentTarget"/> is in an "engaged" combat state for its particular type.</summary>
    public bool TargetIsEngaged()
    {
        return CurrentTarget switch
        {
            GoblinStep g => g.gState == GoblinStep.GoblinState.engaged,
            ShockerStep s => s.sState == ShockerStep.ShockerState.engaged,
            RobotStep r => r.isEngaged,
            _ => false
        };
    }


    /// <summary>True while <see cref="CurrentTarget"/> is standing on the ground.</summary>
    public bool TargetGrounded()
    {
        return CurrentTarget switch
        {
            RobotStep r => r.Grounded(),
            GoblinStep g => g.Grounded(),
            ShockerStep s => s.Grounded(),
            _ => false
        };
    }


    /// <summary>True while <see cref="CurrentTarget"/> is dead, or if there is no target at all.</summary>
    public bool TargetIsDead()
    {
        return CurrentTarget switch
        {
            RobotStep r => r.eState == RobotStep.EnemyState.death,
            GoblinStep g => g.gState == GoblinStep.GoblinState.death,
            ShockerStep s => s.sState == ShockerStep.ShockerState.death,
            _ => true
        };
    }




    /// <summary>
    /// True if <paramref name="dx"/>, a horizontal distance from the player to a candidate target,
    /// is in front of the player given which way the player is currently facing. A small deadzone
    /// around zero always passes, so a target almost directly on top of the player isn't rejected by
    /// facing direction alone.
    /// </summary>
    /// <param name="dx">Horizontal distance from the player to the candidate: <c>target.x, player.x</c>.</param>
    /// <param name="facingLeft">True if the player is currently facing left.</param>
    public bool PassesFacingCheck(float dx, bool facingLeft)
    {
        if (Mathf.Abs(dx) < FacingDeadzone) return true;
        return !((facingLeft && dx > 0) || (!facingLeft && dx < 0));
    }




    /// <summary>
    /// Decides which enemy should become the melee target: a nearby robot, the boss, or the shocker.
    /// target, depending on the active scene and which candidates are currently valid. This method
    /// does not set <see cref="CurrentTarget"/> itself; assign the returned value to it at the call site.
    /// </summary>
    /// <param name="origin">The player's current position.</param>
    /// <param name="facingLeft">True if the player is currently facing left.</param>
    /// <param name="playerTransform">The player's own Transform, used for line-of-sight raycasts.</param>
    /// <param name="jumpableGround">Layer mask used for line-of-sight raycasts against level geometry.</param>
    /// <param name="closestRobot">The nearest valid RobotStep candidate found by the caller's own enemy scan, or null.</param>
    /// <param name="closestRobotDist">The distance from the player to <paramref name="closestRobot"/>.</param>
    /// <returns>The resolved target, or null if nothing is currently targetable.</returns>
    public Component ResolveTarget(Vector2 origin, bool facingLeft, Transform playerTransform, LayerMask jumpableGround, RobotStep closestRobot, float closestRobotDist)
    {
        if (SceneManager.GetActiveScene().name == "Boss")
        {
            if (!IsBossMeleeResolvable())
                return null;

            return IsValidMeleeCandidate(origin, facingLeft, playerTransform, jumpableGround, Boss.transform) ? Boss : null;
        }

        // Mission scene compares the shocker against the closest robot
        if (SceneManager.GetActiveScene().name == "Mission" && Shocker != null && Shocker.sState != ShockerStep.ShockerState.death)
        {
            float shockerDx = Shocker.transform.position.x - origin.x;
            bool inFront = !((facingLeft && shockerDx > 0) || (!facingLeft && shockerDx < 0));
            float shockerDist = Mathf.Abs(shockerDx);

            if (inFront && shockerDist <= 5.2f && Shocker.sState != ShockerStep.ShockerState.chase)
            {
                // Prefer whichever is closer
                if (closestRobot == null || shockerDist < closestRobotDist)
                    return Shocker;
            }
        }

        return closestRobot;
    }




    /// <summary>
    /// True if the boss is currently in a state that allows it to be resolved as a melee target. The
    /// boss is only targetable when it's engaged, getting hit, or in the air fighting on its glider
    /// </summary>
    /// <returns>True if the boss is currently in a state that allows it to be resolved as a melee target.</returns>
    private bool IsBossMeleeResolvable()
    {
        if (Boss == null) return false;
        return Boss.gState == GoblinStep.GoblinState.engaged || Boss.gState == GoblinStep.GoblinState.getting_hit || (Boss.gState == GoblinStep.GoblinState.on_glider && Boss.glider.state == GliderScript.GState.AirFight);
    }




    /// <summary>
    /// True if the given target Transform is a valid candidate for melee targeting.
    /// </summary>
    /// <returns>True if the given target Transform is a valid candidate for melee targeting.</returns>
    private bool IsValidMeleeCandidate(Vector2 origin, bool facingLeft, Transform playerTransform, LayerMask jumpableGround, Transform targetTf)
    {
        if (targetTf == null) return false;

        float dx = targetTf.position.x - origin.x;
        if (!PassesFacingCheck(dx, facingLeft)) return false;

        if (Mathf.Abs(dx) > 5.2f) return false;

        RaycastHit2D hit = Physics2D.Linecast(playerTransform.position, targetTf.position, jumpableGround);
        if (hit.collider != null && (Vector2)hit.point != (Vector2)targetTf.position) return false;

        foreach (var hl in Physics2D.LinecastAll(playerTransform.position, targetTf.position))
        {
            LightningScript ls = hl.collider?.GetComponent<LightningScript>();
            if (ls != null && ls.phase == 0) return false;
        }

        return true;
    }




    /// <summary>
    /// Clears <see cref="CurrentTarget"/> if it currently matches the given component. Called when an
    /// enemy becomes untargetable (for example, it dies) so the player doesn't keep referencing it.
    /// </summary>
    /// <param name="target">The component that just became untargetable.</param>
    public void ReleaseIfCurrent(Component target)
    {
        if (CurrentTarget == target)
            CurrentTarget = null;
    }
}