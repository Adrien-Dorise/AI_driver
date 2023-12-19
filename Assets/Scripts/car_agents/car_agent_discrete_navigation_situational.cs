using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.ObjectModel;
using Unity.VisualScripting.Dependencies.Sqlite;

public class car_agent_discrete_navigation_situational : car_agent
{

    private enum situationals {none, traffic_light};
    private situationals current_situational;
    private GameObject situational_object;
    [SerializeField] navigation nav_script;

    protected override void _start()
    {
        current_situational = situationals.none;
        situational_object = null;
        nav_script = this.transform.parent.GetComponentInChildren<navigation>();
    }
    protected override void _onEpisodeBegin()
    {
        nav_script.set_random_trip();
        bool is_navigation_initialised = nav_script.activate_navigation(nav_script.start_point, nav_script.end_point) != -1;
        if(!is_navigation_initialised)
        {
            EndEpisode();
        }
    }

    protected override Transform _getTarget()
    {
        return nav_script.active_target.transform.GetChild(1); 
    }

    protected override Transform _getStart()
    {
        Transform startPos =  nav_script.active_target.transform.GetChild(0);
        bool is_next_path_null = nav_script.activate_next_node() == 0;
        if(is_next_path_null)
        {
            EndEpisode();
        }
        return startPos;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Target position
        try{
            sensor.AddObservation(distanceVector(this.transform, target)); //3 observations
        }
        catch
        {
            sensor.AddObservation(distanceVector(this.transform, this.transform)); //3 observations
            Debug.LogWarning("No target position found. Replaced with null vector3");            
        }

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
                if(nav_script.activate_next_node() == 0) //No nodes left
                {
                    EndEpisode();
                }
                try{
                target = nav_script.active_target.transform.GetChild(1);
                }
                catch{
                    Debug.LogWarning("No target available");
                }
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
