using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{

    [Header("Prefab")]
    [Tooltip("Prefab must have Tag = 'Obstacle' and Layer = 'Obstacle'.")]
    [SerializeField] private GameObject obstaclePrefab;

    [Header("Spawn Count")]
    [SerializeField] private int obstacleCount = 20;

    [Header("Spawn Bounds  (world space)")]
    [SerializeField] private Vector3 minBounds = new Vector3(0,0,0);
    [SerializeField] private Vector3 maxBounds = new Vector3(0,0,0);


    private readonly List<GameObject> _obstacleList = new List<GameObject>();

    public void RespawnObstacles()
    {
        DestroyAll();

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = obstacleCount * 10;   // avoid infinite loop

        while (spawned < obstacleCount && attempts < maxAttempts)
        {
            attempts++;

            float x = Random.Range(minBounds.x, maxBounds.x);
            float y = Random.Range(minBounds.y, maxBounds.y);
            float z = Random.Range(minBounds.z, maxBounds.z);

            Vector3 pos = new Vector3(x, y, z);

            GameObject obstacle = Instantiate(obstaclePrefab, pos ,Quaternion.Euler(0,0,0), transform);

            //Just to be sure
            obstacle.tag = "Obstacle";
            obstacle.layer = LayerMask.NameToLayer("Obstacle");

            _obstacleList.Add(obstacle);
            spawned++;
        }

    }


    private void DestroyAll()
    {
        foreach (var obstacle in _obstacleList)
        {
            if (obstacle != null) Destroy(obstacle);
        }
            
        _obstacleList.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        // Spawn volume (orange)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);

        Vector3 centerLocation = (minBounds + maxBounds) * 0.5f;

        Vector3 size = maxBounds - minBounds;

        Gizmos.DrawWireCube(centerLocation, size);

    }
}