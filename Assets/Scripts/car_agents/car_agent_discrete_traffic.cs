using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class car_agent_discrete_traffic : car_agent
{
    private enum situationals {none, traffic_light};
    private situationals current_situational;
    private GameObject situational_object;
    private Transform training_positions;

    protected override void _start()
    {
        current_situational = situationals.none;
        situational_object = null;
    }

    protected override void _onEpisodeBegin()
    {
        for(int i=0; i<training_positions.transform.childCount; i++)
        {
            training_positions.transform.GetChild(i).gameObject.SetActive(false);
        }
        positionStep = Random.Range(0, training_positions.transform.childCount);
        training_positions.transform.GetChild(positionStep).gameObject.SetActive(true);
    }

    protected override Transform _getTarget()
    {
        return training_positions.transform.GetChild(positionStep).GetChild(1);
    }

    protected override Transform _getStart()
    {
        return training_positions.transform.GetChild(positionStep).GetChild(0);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation(distanceVector(this.transform, target)); //3 observations

        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);

        //Situational
        switch(current_situational)
        {
            case situationals.traffic_light:
                switch(situational_object.GetComponent<traffic_light>().current_state)
                {
                    case traffic_light.traffic_states.green:
                        sensor.AddObservation(1);
                        break;
                    case traffic_light.traffic_states.orange:
                        sensor.AddObservation(2);
                        break;
                    case traffic_light.traffic_states.red:
                        sensor.AddObservation(3);
                        break;
                }
                break;
            
            default:
                sensor.AddObservation(0);
                break;
        }
    }

    protected override void _fixRewards()
    {
        // Approached target
        float distanceToTarget = Vector3.Distance(this.transform.position, target.position);
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

    protected override void _triggerRewards(string tag, bool is_inside)
    {
        if(is_inside)
        {
            if(tag == "Target")
            {
                SetReward(1.0f);
                training_positions.transform.GetChild(positionStep).gameObject.SetActive(false);
                
                if(positionStep + 1 >= training_positions.transform.childCount)
                {
                    positionStep = 0;
                }
                else
                {
                    positionStep++;
                }
                
                training_positions.transform.GetChild(positionStep).gameObject.SetActive(true);
                target = training_positions.transform.GetChild(positionStep).GetChild(1);
                target.position = training_positions.transform.GetChild(positionStep).GetChild(1).position;
            }
        }
        else
        {
            if(tag == "Traffic_light")
            {
                current_situational = situationals.none;
                situational_object = null;
            }
        }
    }

    protected override void _collisionRewards(string tag)
    {
        if(tag == "Death")
        {
            SetReward(-1f);
            EndEpisode();
        }
    }
    
}
