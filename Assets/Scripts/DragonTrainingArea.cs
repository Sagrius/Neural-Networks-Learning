using UnityEngine;

public class DragonTrainingArea : MonoBehaviour
{
    //Public output consumed by the agent
    [HideInInspector] public Vector3 AgentSpawnPosition;
    [HideInInspector] public Vector3 TargetSpawnPosition;

    //Spawn bounds (local space) 
    [Header("Spawn Bounds  (local space)")]
    [SerializeField] private Vector3 agentSpawnMinBounds = new Vector3(0,0,0);
    [SerializeField] private Vector3 agentSpawnMaxBounds = new Vector3(0,0,0);

    //  Target bounds (local space) 
    [Header("Target Bounds  (local space)")]
    [SerializeField] private Vector3 targetMinSpawnBounds = new Vector3(0, 0, 0);
    [SerializeField] private Vector3 targetMaxSpawnBounds = new Vector3(0, 0, 0);


    //  Target visual marker 
    [Header("Target Marker")]
    [Tooltip("Assign a sphere or particle GameObject to visualise the target.")]
    [SerializeField] private Transform targetMarker;

    public void ResetRunArea()
    {
        RandomizeLocation(ref AgentSpawnPosition,agentSpawnMinBounds,agentSpawnMaxBounds);
        RandomizeLocation(ref TargetSpawnPosition,targetMinSpawnBounds,targetMaxSpawnBounds);

        targetMarker.position = TargetSpawnPosition;
    }

    private void RandomizeLocation(ref Vector3 Reference, Vector3 minBounds, Vector3 maxBounds)
    {
        Vector3 localPos = RandomInBounds(minBounds, maxBounds);

        Reference = transform.TransformPoint(localPos);

    }

    private static Vector3 RandomInBounds(Vector3 min, Vector3 max)
    {
        float x = Random.Range(min.x, max.x);
        float y = Random.Range(min.y, max.y);
        float z = Random.Range(min.z, max.z);

        return new Vector3(x,y,z);
    }

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        VisualizeBounds(agentSpawnMinBounds, agentSpawnMaxBounds);

        Gizmos.color = Color.cyan;
        VisualizeBounds(targetMinSpawnBounds, targetMaxSpawnBounds);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(AgentSpawnPosition, 35);

    }

    private void VisualizeBounds(Vector3 min, Vector3 max)
    {
        Vector3 position = transform.TransformPoint((min + max) * 0.5f);
        Vector3 size = max - min;
        Gizmos.DrawWireCube(position, size);
    }

    #endregion
}