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
    private Transform target;
    private car_controller car_script;

    private float lastDistanceToTarget;
    void Start () 
    {
        rBody = GetComponent<Rigidbody>();
        target = GameObject.Find("Target").transform;
        car_script = this.gameObject.GetComponent<car_controller>();
        car_script.isAgent = true;
        lastDistanceToTarget = 10000f;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;
        
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = -Input.GetAxis("Vertical");
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
        //Move car to initial position
        this.rBody.angularVelocity = Vector3.zero;
        this.rBody.velocity = Vector3.zero;
        this.transform.position = new Vector3(0.28f, 0.5f, -16.13f);
        this.transform.rotation = Quaternion.Euler(0f,-90f,0f);

        //Move target to initial spot
        target.localPosition = new Vector3(5f, 0.5f, -15.5f);

        lastDistanceToTarget = 10000f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation(target.localPosition); // 3 observations
        sensor.AddObservation(this.transform.localPosition); // 3 observations


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
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, target.localPosition);

        // Reached target
        if (distanceToTarget < 1.42f)
        {
            SetReward(1.0f);
            EndEpisode();
        }
        
        if(distanceToTarget < lastDistanceToTarget)
        {
            SetReward(01f);
        }
        else
        {
            SetReward(-0.1f);
        }

        lastDistanceToTarget = distanceToTarget;

        // Fell off platform
        if (this.transform.localPosition.y < 0)
        {
            EndEpisode();
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Death")
        {
            EndEpisode();
        }
    }

    int maxStep = 3500;
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
