using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBossCode : MonoBehaviour
{
    [SerializeField] private PlayerStep myself;
    Vector2 corner;

    void Update()
    {
        if (myself.transform.position.y <= -1.72f && myself.pState != PlayerStep.PlayerState.quickzip && myself.pState != PlayerStep.PlayerState.death)
        {
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
            //  if (!audio_is_playing(snd_goblin_boss) && !audio_is_playing(snd_boss) && !audio_is_playing(snd_blank))
            //       audio_play_sound(choose(snd_goblin_boss, snd_boss, snd_blank), 1, false);

            myself.moveTarget = corner; // <- trigger movement
            myself.coll.size = new Vector2(0.7719507f, 1.863027f);
            myself.coll.offset = new Vector2(-0.3766563f, -0.968719f);
            AudioClip[] clips = { myself.sndSwing, myself.sndSwing2, myself.sndSwing3 };
            AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
            myself.audioSrc.PlayOneShot(randomClip);
            myself.pState = PlayerStep.PlayerState.quickzip;
            myself.rb.gravityScale = 0;
        }
    }
}