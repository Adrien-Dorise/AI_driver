using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class car_agent_template : car_agent
{
    protected override void _onEpisodeBegin()
    {
        
    }

    protected override Transform _getTarget()
    {
        return null;
    }

    protected override Transform _getStart()
    {
        return null;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        
    }

    protected override void _fixRewards()
    {
        
    }

    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {

    }

    private void OnCollisionEnter(Collision collision)
    {
        _collisionRewards(collision.transform.tag);
    }
    

    int maxStep = 5000;
    private void FixedUpdate()
    {

    }
    
}
