using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class car_agent : Agent
{
    protected Rigidbody rBody;
    protected Transform target;
    protected car_controller car_script;
    [SerializeField] protected GameObject trainingPositions;
    protected float lastDistanceToTarget;    
    [SerializeField] protected int positionStep;

    enum RewardType {}

    void Start () 
    {
        rBody = GetComponent<Rigidbody>();
        car_script = this.gameObject.GetComponent<car_controller>();
        target = trainingPositions.transform.GetChild(0).GetChild(1);
        car_script.isAgent = true;
        lastDistanceToTarget = 10000f;
        positionStep = 0;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;
        
        if(Input.GetKey(KeyCode.Space))
        {
            discreteActionsOut[0] = 1;
        }
        else
        {
            discreteActionsOut[0] = 0;
        }

        if(Input.GetAxis("Horizontal") == 0)
        {
            discreteActionsOut[1] = 0;
        }
        else if(Input.GetAxis("Horizontal") > 0)
        {
            discreteActionsOut[1] = 1;
        }
        else if(Input.GetAxis("Horizontal") < 0)
        {
            discreteActionsOut[1] = 2;
        }
         
        if(Input.GetAxis("Vertical") == 0)
        {
            discreteActionsOut[2] = 0;
        }
        else if(Input.GetAxis("Vertical") > 0)
        {
            discreteActionsOut[2] = 1;
        }
        else if(Input.GetAxis("Vertical") < 0)
        {
            discreteActionsOut[2] = 2;
        }
    }

    protected virtual void _onEpisodeBegin(){}

    protected virtual Transform _getTarget(){return null;}

    protected virtual Transform _getStart(){return null;}


    public override void OnEpisodeBegin()
    {
        _onEpisodeBegin();
        Transform startPosition = _getStart();
        target = _getTarget();

        //Move car to initial position
        this.rBody.angularVelocity = Vector3.zero;
        this.rBody.velocity = Vector3.zero;
        this.transform.position = startPosition.position;
        this.transform.rotation = startPosition.parent.localRotation;
        this.transform.Rotate(new Vector3(0f,90f,0f));

        //Move target to initial spot
        //target.position = trainingPositions.transform.GetChild(positionStep).GetChild(1).position;

        lastDistanceToTarget = 10000f;
    }


    protected Vector3 distanceVector(Transform object1, Transform object2)
    {
        float x, y, z;

        x = object1.transform.position.x - object2.transform.position.x;
        y = object1.transform.position.y - object2.transform.position.y;
        z = object1.transform.position.z - object2.transform.position.z;
        return new Vector3(x, y, z);
    }

    public override void CollectObservations(VectorSensor sensor){}
    

    protected virtual void _fixRewards(){} 

    protected virtual void _collisionRewards(string tag){}


    public override void OnActionReceived(ActionBuffers actionBuffers){
        _fixRewards();
    }


    private void OnCollisionEnter(Collision collision)
    {
        _collisionRewards(collision.transform.tag);
    }


}
