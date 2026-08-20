using System.Collections;
using UnityEngine;


/// <summary>
/// Handles boss scene specific behavior for the player, including triggering quickzip actions and playing certain voice lines.
/// </summary>


public class PlayerBossCode : MonoBehaviour
{
    [SerializeField] private PlayerStep myself;
    Vector2 corner;
    private bool bossVoicePlaying = false;
    private bool bossZipTriggered = false;




    void Update()
    {
        bool belowThreshold = myself.transform.position.y <= -1.72f;

        // Once the player has climbed back above the threshold, allow this to fire again next time they fall
        if (!belowThreshold)
        {
            bossZipTriggered = false;
        }

        if (belowThreshold && myself.pState != PlayerStep.PlayerState.quickzip && myself.pState != PlayerStep.PlayerState.death && !bossZipTriggered)
        {
            bossZipTriggered = true;

            if (myself.pState == PlayerStep.PlayerState.swing)
            {
                myself.rb.velocity = new Vector2(myself.rb.velocity.x, myself.jspd);
                myself.rb.gravityScale = 1;
                myself.AnimationDriver.SetMovementState((int)PlayerStep.MovementState.endswing);
                myself.AudioController.Play(myself.sndWebRelease);
                myself.ExitSwing();
                myself.pState = PlayerStep.PlayerState.normal;
                myself.swingEnd = true;
            }

            myself.RopeRenderer.ReturnAllToPool();

            if (myself.transform.position.x < -5.474f)
            {
                corner = new Vector2(-10.3744f, 3.5459f);
            }
            else
            {
                corner = new Vector2(-1.0767f, 3.5459f);
            }

            if (!bossVoicePlaying)
            {
                AudioClip[] bossClips = { myself.sndGoblinBoss, myself.sndBoss };
                AudioClip randomBossClip = bossClips[Random.Range(0, bossClips.Length)];
                if (randomBossClip != null) StartCoroutine(PlayBossVoice(randomBossClip));
            }

            myself.moveTarget = corner;
            myself.coll.size = new Vector2(0.7719507f, 1.863027f);
            myself.coll.offset = new Vector2(-0.3766563f, -0.968719f);
            myself.AudioController.PlayRandom(myself.sndSwing, myself.sndSwing2, myself.sndSwing3);

            // Nudge toward the target before entering quickzip, same as every other zip entry point
            myself.BeginForcedQuickZip(corner);
        }
    }




    /// <summary>
    /// Plays the specified voice audio clip intended for Boss scene and waits for it to finish before continuing execution.
    /// </summary>
    /// <param name="clip">The audio clip to play as the boss voice. Must not be null.</param>
    IEnumerator PlayBossVoice(AudioClip clip)
    {
        bossVoicePlaying = true;
        myself.AudioController.Play(clip);
        yield return new WaitForSeconds(clip.length);
        bossVoicePlaying = false;
    }
}