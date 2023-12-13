using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;


public class car_agent_discrete1 : car_agent
{
    
    protected override void _onEpisodeBegin()
    {
        for(int i=0; i<trainingPositions.transform.childCount; i++)
        {
            trainingPositions.transform.GetChild(i).gameObject.SetActive(false);
        }
        positionStep = Random.Range(0, trainingPositions.transform.childCount);
        trainingPositions.transform.GetChild(positionStep).gameObject.SetActive(true);
    }

    protected override Transform _getTarget()
    {
        return trainingPositions.transform.GetChild(positionStep).GetChild(1);
    }

    protected override Transform _getStart()
    {
        return trainingPositions.transform.GetChild(positionStep).GetChild(0); 
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation(distanceVector(this.transform, target)); //3 observations

        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);
    }

    protected override void _fixRewards()
    {
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
            AddReward(0.00001f);
        }
        else
        {
            AddReward(-0.00005f);
        }

        lastDistanceToTarget = distanceToTarget;

        // Fell off platform
        if (this.transform.position.y < 0)
        {
            EndEpisode();
        }
    }

    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {

        //Break
        if(actionBuffers.DiscreteActions[0] == 1)
        {
            car_script.isBreaking = true;
        }
        else if(actionBuffers.DiscreteActions[0] == 0)
        {
            car_script.isBreaking = false;
        }

        //Horizontal
        if(actionBuffers.DiscreteActions[1] == 0)
        {
            car_script.horizontalInput = 0;
        }
        else if(actionBuffers.DiscreteActions[1] == 1)
        {
            car_script.horizontalInput = 1;
        }
        else if(actionBuffers.DiscreteActions[1] == 2)
        {
            car_script.horizontalInput = -1;
        }

        //Vertical
        if(actionBuffers.DiscreteActions[2] == 0)
        {
            car_script.verticalInput = 0;
        }
        else if(actionBuffers.DiscreteActions[2] == 1)
        {
            car_script.verticalInput = 1;
        }
        else if(actionBuffers.DiscreteActions[2] == 2)
        {
            car_script.verticalInput = -1;
        }

       _fixRewards();

    }

    int maxStep = 5000;
    private void FixedUpdate()
    {
        if(this.StepCount >= maxStep)
        {
            EndEpisode();
        }
    }
}

