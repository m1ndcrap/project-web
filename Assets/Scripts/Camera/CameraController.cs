using UnityEngine;


/// <summary>
/// Keeps the camera locked to a single target's X/Y position every frame, preserving the camera's
/// own Z position (so it stays at whatever distance it was set up at). Toggle <see cref="followPlayer"/>
/// off to freeze the camera in place during a cutscene or a scripted objective sequence.
/// </summary>


public class CameraController : MonoBehaviour
{
    [Header("Follow Target")]
    [Tooltip("The Transform this camera follows. Usually the player.")]
    [SerializeField] private Transform player;




    [Tooltip("While true, the camera snaps to the target's X/Y every frame. Set to false to freeze the camera in place (e.g. during a cutscene), and back to true to resume following.")]
    public bool followPlayer = true;




    private void Start()
    {
        if (player == null)
        {
            Debug.LogWarning($"[CameraController] No target assigned on '{gameObject.name}'. Assign one in the Inspector, or this camera won't move.", this);
        }
    }




    private void LateUpdate()
    {
        if (followPlayer && player != null)
        {
            transform.position = new Vector3(player.position.x, player.position.y, transform.position.z);
        }
    }
}