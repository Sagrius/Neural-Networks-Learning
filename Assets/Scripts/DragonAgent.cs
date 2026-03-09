using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(Rigidbody))]
public class DragonAgent : Agent
{
    [Header("References")]
    [SerializeField] private DragonController dragonController;
    [SerializeField] private Transform targetPoint;

    [Header("Flight Settings")]
    [SerializeField] private float moveForceMultiplier = 10f;
    [SerializeField] private float maxSpeed = 15f;

    [Header("Raycasts for Obstacle Detection")]
    [SerializeField] private int rayCount = 8;
    [SerializeField] private float rayLength = 10f;
    [SerializeField] private LayerMask obstacleLayer;

    private Rigidbody rb;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 previousPosition;

    // How close to target counts as success
    private const float TargetReachedDistance = 2f;

    // -------------------------------------------------------

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        MaxStep = 5000;  
    }

    public override void OnEpisodeBegin()
    {
        // Reset dragon to its original scene position
        previousPosition = startPosition;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(startPosition, startRotation);
    }

    // -------------------------------------------------------
    // OBSERVATIONS  (inputs to the neural network)
    // -------------------------------------------------------
    public override void CollectObservations(VectorSensor sensor)
    {
        // Dragon position (normalized relative to target)
        Vector3 toTarget = targetPoint.position - transform.position;
        sensor.AddObservation(toTarget.normalized);         // 3 floats
        sensor.AddObservation(toTarget.magnitude / 100f);  // 1 float (distance, normalized)

        // Current velocity
        sensor.AddObservation(rb.linearVelocity / maxSpeed);  // 3 floats

        // Raycasts — obstacle detection (one per direction around the dragon)
        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * (360f / rayCount);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            float hit = Physics.Raycast(transform.position, dir, out RaycastHit hitInfo, rayLength, obstacleLayer)
                        ? hitInfo.distance / rayLength   // normalized 0–1
                        : 1f;                            // no obstacle = 1
            sensor.AddObservation(hit);                  // rayCount floats
        }

        // Total observation size: 3 + 1 + 3 + rayCount = 15 (with 8 rays)
    }

    // -------------------------------------------------------
    // ACTIONS  (outputs from the neural network)
    // -------------------------------------------------------
    public override void OnActionReceived(ActionBuffers actions)
    {
        // 3 continuous actions in range [-1, 1]
        float leftWing = actions.ContinuousActions[0];   // -1 to 1
        float rightWing = actions.ContinuousActions[1];   // -1 to 1
        float tail = actions.ContinuousActions[2];   // -1 to 1

        // --- Drive the animator sliders via DragonController ---
        // Remap from [-1,1] to [0,1] for your sliders
        dragonController.SetLeftWingValue((leftWing + 1f) / 2f);
        dragonController.SetRightWingValue((rightWing + 1f) / 2f);
        dragonController.SetTailValue((tail + 1f) / 2f);

        // --- Drive flap animation speed based on average wing output ---
        float avgWing = (Mathf.Abs(leftWing) + Mathf.Abs(rightWing)) / 2f;
        dragonController.SetFlapSpeed(Mathf.Lerp(0.5f, 2f, avgWing));

        // --- Apply physical forces to the Rigidbody ---
        // Equal wings = lift/descend, unequal = turn, tail = forward thrust
        float lift = (leftWing + rightWing) * 0.5f;
        float yaw = (rightWing - leftWing);           // differential = turn
        float thrust = tail;

        Vector3 force = transform.up * lift * moveForceMultiplier
                      + transform.forward * thrust * moveForceMultiplier;
        rb.AddForce(force);

        // Torque for turning
        rb.AddTorque(transform.up * yaw * moveForceMultiplier * 0.5f);

        // Clamp speed
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        // --- Rewards ---
        float distanceToTarget = Vector3.Distance(transform.position, targetPoint.position);

        // Reward for getting closer since last step (dense reward)
        float previousDistance = Vector3.Distance(previousPosition, targetPoint.position);
        AddReward((previousDistance - distanceToTarget) * 0.1f);

        // Penalty for being upside down
        AddReward(Vector3.Dot(transform.up, Vector3.up) * 0.001f);

        // Penalty per step
        AddReward(-0.0005f);

        // Big reward for reaching target
        if (distanceToTarget < TargetReachedDistance)
        {
            AddReward(+10f);
            EndEpisode();
        }

        // Fell too far
        if (transform.position.y < -5f)
        {
            AddReward(-2f);
            EndEpisode();
        }

        previousPosition = transform.position;
    }

    // -------------------------------------------------------
    // Heuristic — lets you manually test with keyboard
    // -------------------------------------------------------
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        ca[0] = Input.GetAxis("Horizontal");   // left wing
        ca[1] = -Input.GetAxis("Horizontal");  // right wing (opposite = turn)
        ca[2] = Input.GetAxis("Vertical");     // tail / thrust
    }

    // -------------------------------------------------------
    // Obstacle collision penalty
    // -------------------------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
        {
            AddReward(-1f);
            EndEpisode();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * (360f / rayCount);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, dir * rayLength);
        }
        if (targetPoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPoint.position, TargetReachedDistance);
        }
    }
}