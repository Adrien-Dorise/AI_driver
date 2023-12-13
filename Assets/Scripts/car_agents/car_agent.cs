using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class car_agent : Agent
{
    private Rigidbody rBody;
    private Transform target;
    private car_controller car_script;
    [SerializeField] private GameObject trainingPositions;
    private float lastDistanceToTarget;    
    [SerializeField] private int positionStep;

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

    protected virtual void _onEpisodeBegin()
    {
        for(int i=0; i<trainingPositions.transform.childCount; i++)
        {
            trainingPositions.transform.GetChild(i).gameObject.SetActive(false);
        }
        positionStep = Random.Range(0, trainingPositions.transform.childCount);
        trainingPositions.transform.GetChild(positionStep).gameObject.SetActive(true);
    }

    protected virtual Transform _getTarget()
    {
        return trainingPositions.transform.GetChild(positionStep).GetChild(1);
    }

    protected virtual Transform _getStart()
    {
        return trainingPositions.transform.GetChild(positionStep).GetChild(0); 
    }


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


    private Vector3 distanceVector(Transform object1, Transform object2)
    {
        float x, y, z;

        x = object1.transform.position.x - object2.transform.position.x;
        y = object1.transform.position.y - object2.transform.position.y;
        z = object1.transform.position.z - object2.transform.position.z;
        return new Vector3(x, y, z);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation(distanceVector(this.transform, target)); //3 observations

        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);
    }

    protected virtual void _rewards()
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
        // Actions, size = 2
        //car_script.horizontalInput = actionBuffers.ContinuousActions[0];
        //car_script.verticalInput = actionBuffers.ContinuousActions[1];

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


       _rewards();

    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Death")
        {
            SetReward(-1f);
            EndEpisode();
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.transform.tag == "Death")
        {
            SetReward(-1f);
            EndEpisode();
        }
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
