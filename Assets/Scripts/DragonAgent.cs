using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Xml.Schema;

[RequireComponent(typeof(Rigidbody))]
public class DragonAgent : Agent
{
    #region Fields
    [Header("References")]
    [SerializeField] private DragonTrainingArea trainingArea;
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    [SerializeField] private DragonOutputsVisuallizer dragonController;
    [SerializeField] private Transform targetTransform;

    [Header("Rewards")]
    [SerializeField] private float approachRewardScale = 0.01f;
    [SerializeField] private float headingRewardScale = 0.02f;
    [SerializeField] private float minSpeedForHeading = 2f;
    [SerializeField] private float reachTargetBonus = 5f;
    [SerializeField] private float targetRadius = 15f;

    [Header("Penalties")]
    [SerializeField] private float stepPenalty = -0.0005f;
    [SerializeField] private float smoothnessPenalty = -0.001f;
    [SerializeField] private float nearMissPenalty = -0.01f;
    [SerializeField] private float obstacleHitPenalty = -2f;
    [SerializeField] private float minFlightAltitude = 15f;
    [SerializeField] private float groundHitPenalty = -2f;
    [SerializeField] private float OvershootPenalty = -2f;

    [Header("Obstacle detection")]
    [SerializeField] private float rayLength = 30f;
    [SerializeField] private int raysInRing = 8;
    [SerializeField] private float coneAngle = 30f;
    [SerializeField] private LayerMask obstacleMask;

    private Rigidbody _rb;

    private float liftForce = 24f;
    private float thrustForce = 40f;
    private float turnTorque = 16f;
    private float rollTorque = 3f;
    private float pitchTorque = 12f;
    private float dragCoefficient = 0.06f;
    private float maxSpeed = 80f;

    private float _prevDistance;
    private bool _obstacleInRange;

    #endregion

    #region Agent
    public override void Initialize()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.linearDamping = 0.5f;
        _rb.angularDamping = 1f;
    }

    public override void OnEpisodeBegin()
    {
        obstacleSpawner.RespawnObstacles();
        trainingArea.ResetRunArea();

        transform.position = trainingArea.AgentSpawnPosition;
        transform.rotation = Quaternion.Euler(0, 0, 0);
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        targetTransform.position = trainingArea.TargetSpawnPosition;

        _prevDistance = Vector3.Distance(transform.position, targetTransform.position);
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 directionToTarget = (targetTransform.position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, targetTransform.position);
        float altitudeDiff = targetTransform.position.y - transform.position.y;

        //3 Observations
        sensor.AddObservation(directionToTarget);           
        //1 Observation -- 1 is far / 0 is close
        sensor.AddObservation(Mathf.Clamp01(distanceToTarget / 1800f));
        //1 Observation -- is target above or below -- positive above target --equal same height -- negative below target
        sensor.AddObservation(altitudeDiff / 100f);

        Vector3 localVelocity = transform.InverseTransformDirection(_rb.linearVelocity);
        Vector3 localAngularVelocity = transform.InverseTransformDirection(_rb.angularVelocity);
        //3 Observations --  how fast and in which local direction
        sensor.AddObservation(localVelocity / maxSpeed);
        //3 Observations -- how fast is it spinning
        sensor.AddObservation(localAngularVelocity / 10f);
        //3 Observations -- which way the dragon faces (world space)
        sensor.AddObservation(transform.forward);                       
        //3 Observations -- which way is up for the dragon (world space)
        sensor.AddObservation(transform.up);                            

        //17 Total ^

        // did it hit (0 or 1)
        // normalised distance (1=clear, 0=right on top)
        _obstacleInRange = false;
        foreach (Vector3 dir in BuildRayDirections())
        {
            bool hit = Physics.Raycast(transform.position, dir, out RaycastHit hitInfo, rayLength, obstacleMask);
            float normDistance = hit ? hitInfo.distance / rayLength : 1f;

            //1 Observation
            sensor.AddObservation(hit ? 1f : 0f);
            //1 Observation
            sensor.AddObservation(normDistance);

            if (hit) _obstacleInRange = true;
        }

        //1 Center Ray x2  = 2
        //8 Cone Rays x 2 = 16

        //Total 17+18 = 35
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        float leftWing = (actions.ContinuousActions[0] + 1f) * 0.5f;
        float rightWing = (actions.ContinuousActions[1] + 1f) * 0.5f;
        float tail = actions.ContinuousActions[2];

        ApplyFlightForces(leftWing, rightWing, tail);
        UpdateVisuals(leftWing, rightWing, tail);

        float distance = Vector3.Distance(transform.position, targetTransform.position);
        float deltadist = _prevDistance - distance;
        _prevDistance = distance;

        // Proximity Reward
        AddReward(deltadist * approachRewardScale);

        // Direction Reward
        if (_rb.linearVelocity.magnitude > minSpeedForHeading)
        {
            Vector3 directionToTarget = (targetTransform.position - transform.position).normalized;
            float headingAlignment = Vector3.Dot(_rb.linearVelocity.normalized, directionToTarget);
            AddReward(headingAlignment * headingRewardScale);
        }

        // Step Penalty
        AddReward(stepPenalty);

        // Smoothness Penalty
        AddReward(smoothnessPenalty * _rb.angularVelocity.magnitude);

        // Penalty when an obstacle is dangerously close ahead
        if (_obstacleInRange) AddReward(nearMissPenalty);

        if (transform.position.z > targetTransform.position.z + 5)
        {
            AddReward(OvershootPenalty);
            Debug.Log("Ended episode due to overshoot!");
            EndEpisode();
            return;
        } 

        // Altiture Penalty
        if (transform.position.y < minFlightAltitude)
        {
            AddReward(groundHitPenalty);
            Debug.Log("Ended episode due to height!");
            EndEpisode();
            return;
        }

        // Target reward
        if (distance < targetRadius)
        {
            AddReward(reachTargetBonus);
            Debug.Log("Reached target!");
            EndEpisode();
        }
    }

    #endregion

    #region Physics
    private void ApplyFlightForces(float leftWing, float rightWing, float tail)
    {

        // Average wing strength = lift (both wings up = climb)
        float liftInput = (leftWing + rightWing) * 0.5f;
        _rb.AddForce(transform.up * liftInput * liftForce, ForceMode.Force);

        // Tail positive = forward thrust, tail negative = braking
        if (tail > 0f)
            _rb.AddForce(transform.forward * tail * thrustForce, ForceMode.Force);
        else
            _rb.AddForce(-_rb.linearVelocity * Mathf.Abs(tail) * dragCoefficient * 10f, ForceMode.Force);

        // Wing difference = turn (left stronger = turn right, right stronger = turn left)
        float wingDifference = leftWing - rightWing;
        _rb.AddTorque(transform.up * wingDifference * turnTorque, ForceMode.Force);
        _rb.AddTorque(transform.forward * wingDifference * rollTorque, ForceMode.Force);
        _rb.AddTorque(transform.right * tail * pitchTorque, ForceMode.Force);


        // Speed limit
        if (_rb.linearVelocity.magnitude > maxSpeed) _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;

    }

    #endregion

    #region Visuals
    private void UpdateVisuals(float leftWing, float rightWing, float tail)
    {
        if (dragonController == null) return;

        float flapSpeed = Mathf.Lerp(0f, 1f, (leftWing + rightWing) * 0.5f);
        dragonController.SetFlapSpeed(flapSpeed);
        dragonController.SetLeftWingValue(leftWing);
        dragonController.SetRightWingValue(rightWing);
        dragonController.SetTailValue(tail);
    }

    private void OnDrawGizmos()
    {

        foreach (Vector3 dir in BuildRayDirections())
        {
            bool hit = Physics.Raycast(transform.position, dir, rayLength, obstacleMask);
            Gizmos.color = hit ? Color.red : Color.yellow;
            Gizmos.DrawRay(transform.position, dir * rayLength);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetTransform.position, targetRadius);
    }

    #endregion

    #region Collisions/Rays
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            AddReward(obstacleHitPenalty);
            EndEpisode();
        }

        if (other.gameObject.CompareTag("Ground"))
        {
            AddReward(groundHitPenalty);
            EndEpisode();
        }
    }
    private Vector3[] BuildRayDirections()
    {
        var dirs = new Vector3[1 + raysInRing];

        // Center ray 
        dirs[0] = transform.forward;

        // Ring rays 
        for (int i = 0; i < raysInRing; i++)
        {
            float rollAngle = 360f * i / raysInRing;
            Vector3 tilted = Quaternion.AngleAxis(coneAngle, transform.right) * transform.forward;
            Vector3 rolled = Quaternion.AngleAxis(rollAngle, transform.forward) * tilted;
            dirs[1 + i] = rolled;
        }

        return dirs;
    }
    #endregion
}