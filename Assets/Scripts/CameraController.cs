using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    public bool followPlayer = true;
    
    private void LateUpdate()
    {
        if (followPlayer)
        {
            transform.position = new Vector3(player.position.x, player.position.y, transform.position.z);
        }
    }
}