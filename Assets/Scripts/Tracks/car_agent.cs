using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.VisualScripting;

public class car_agent : Agent
{
    private Rigidbody rBody;
    [SerializeField] private Transform target;
    private car_controller car_script;
    [SerializeField] private GameObject trainingPositions;
    [SerializeField] private int positionStep;

    private float lastDistanceToTarget;
    void Start () 
    {
        rBody = GetComponent<Rigidbody>();
        //trainingPositions = GameObject.Find("Training Positions");
        target = trainingPositions.transform.GetChild(0).GetChild(1);
        car_script = this.gameObject.GetComponent<car_controller>();
        car_script.isAgent = true;
        lastDistanceToTarget = 10000f;
        positionStep = 0;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;
        
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
        if(Input.GetKey(KeyCode.Space))
        {
            discreteActionsOut[0] = 1;
        }
        else
        {
            discreteActionsOut[0] = 0;
        }
    }

    public override void OnEpisodeBegin()
    {
        Transform startPosition = trainingPositions.transform.GetChild(positionStep).GetChild(0); 

        //Move car to initial position
        this.rBody.angularVelocity = Vector3.zero;
        this.rBody.velocity = Vector3.zero;
        this.transform.position = startPosition.position;
        this.transform.rotation = startPosition.localRotation;

        //Move target to initial spot
        //target.position = trainingPositions.transform.GetChild(positionStep).GetChild(1).position;

        lastDistanceToTarget = 10000f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation(target.position); // 3 observations
        sensor.AddObservation(this.transform.position); // 3 observations


        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);
    }

    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 2
        car_script.horizontalInput = actionBuffers.ContinuousActions[0];
        car_script.verticalInput = actionBuffers.ContinuousActions[1];

        if(actionBuffers.DiscreteActions[0] == 1)
        {
            car_script.isBreaking = true;
        }
        else
        {
            car_script.isBreaking = false;
        }

        // Rewards
        float distanceToTarget = Vector3.Distance(this.transform.position, target.position);

        // Reached target
        if (distanceToTarget < 1.42f)
        {
            SetReward(1.0f);
            trainingPositions.transform.GetChild(positionStep).gameObject.SetActive(false);
            
            if(positionStep + 1 >= trainingPositions.transform.childCount)
            {
                positionStep = 0;
            }
            else
            {
                positionStep++;
            }
            
            trainingPositions.transform.GetChild(positionStep).gameObject.SetActive(true);
            target = trainingPositions.transform.GetChild(positionStep).GetChild(1);
            target.position = trainingPositions.transform.GetChild(positionStep).GetChild(1).position;
        }
        
        if(distanceToTarget < lastDistanceToTarget)
        {
            SetReward(0.1f);
        }
        else
        {
            SetReward(-0.1f);
        }

        lastDistanceToTarget = distanceToTarget;

        // Fell off platform
        if (this.transform.position.y < 0)
        {
            EndEpisode();
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Death")
        {
            SetReward(-0.5f);
            EndEpisode();
        }
    }

    int maxStep = 15000;
    int step = 0;
    private void FixedUpdate()
    {
        step+=1;
        if(step >= maxStep)
        {
            step = 0;
            EndEpisode();
        }
    }
}
