using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
                PlayerStep.MovementState mstate;
                mstate = PlayerStep.MovementState.endswing;
                myself.anim.SetInteger("mstate", (int)mstate);
                myself.coll.size = new Vector2(0.8397379f, 1.615343f);
                myself.coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                myself.audioSrc.PlayOneShot(myself.sndWebRelease);
                myself.pState = PlayerStep.PlayerState.normal;
                myself.swingEnd = true;
            }

            myself.ReturnAllRopeSegmentsToPool();

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
            AudioClip[] clips = { myself.sndSwing, myself.sndSwing2, myself.sndSwing3 };
            AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
            myself.audioSrc.PlayOneShot(randomClip);

            // Nudge toward the target before entering quickzip, same as every other zip entry point
            myself.BeginForcedQuickZip(corner);
        }
    }
    IEnumerator PlayBossVoice(AudioClip clip)
    {
        bossVoicePlaying = true;
        myself.audioSrc.PlayOneShot(clip);
        yield return new WaitForSeconds(clip.length);
        bossVoicePlaying = false;
    }
}