using UnityEngine;

public class CameraFollowDragon : MonoBehaviour
{
   [SerializeField] private Transform target;
   [SerializeField] private Quaternion rotation = Quaternion.Euler(30, 0, 0);
   [SerializeField] private Vector3 offset  = new Vector3(0, 15, -35);

    private void Update()
    {
        transform.SetPositionAndRotation(target.position + offset, rotation);
    }
}
