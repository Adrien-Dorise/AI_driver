using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.ObjectModel;
using UnityEditor;

public class car_agent_discrete_navigation : Agent
{
    private Rigidbody rBody;
    [SerializeField] private Transform target;
    private car_controller car_script;
    private int position_step;
    private enum situationals {none, traffic_light};
    private situationals current_situational;
    private GameObject situational_object;
    private navigation nav_script; 

    private float lastDistanceToTarget;
    void Start () 
    {
        current_situational = situationals.none;
        situational_object = null;
        rBody = GetComponent<Rigidbody>();
        car_script = this.gameObject.GetComponent<car_controller>();
        nav_script = this.transform.parent.GetComponentInChildren<navigation>();
        car_script.isAgent = true;
        lastDistanceToTarget = 10000f;
        position_step = 0;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;
        
        //continuousActionsOut[0] = Input.GetAxis("Horizontal");
        //continuousActionsOut[1] = Input.GetAxis("Vertical");

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

    public override void OnEpisodeBegin()
    {
        nav_script.set_random_trip();
        if(nav_script.activate_navigation(nav_script.start_point, nav_script.end_point) == -1)
        {
            EndEpisode();
        }
        Transform startPosition = nav_script.active_target.transform.GetChild(0); 
        
        if(nav_script.activate_next_node() == 0) //No nodes left
        {
            EndEpisode();
        }
        target = nav_script.active_target.transform.GetChild(1);

        //Move car to initial position
        this.rBody.angularVelocity = Vector3.zero;
        this.rBody.velocity = Vector3.zero;
        this.transform.position = startPosition.position;
        this.transform.position += new Vector3(0f,-0.5f,0f);
        this.transform.rotation = startPosition.rotation;
        
        if(Vector3.Dot(distanceVector3(target,this.transform).normalized, this.transform.forward) < 0)
        {
            this.transform.Rotate(new Vector3(0f,180f,0f));
        }

        //Move target to initial spot
        //target.position = nav_script.active_target.transform.GetChild(position_step).GetChild(1).position;

        
        lastDistanceToTarget = 10000f;
    }


    private Vector3 distanceVector3(Transform object1, Transform object2)
    {
        float x, y, z;

        x = object1.transform.position.x - object2.transform.position.x;
        y = object1.transform.position.y - object2.transform.position.y;
        z = object1.transform.position.z - object2.transform.position.z;
        return new Vector3(x, y, z);
    }

    private Vector2 distanceVector2(Transform object1, Transform object2)
    {
        float x, z;

        x = object1.transform.position.x - object2.transform.position.x;
        z = object1.transform.position.z - object2.transform.position.z;
        return new Vector2(x, z);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        try{
            sensor.AddObservation(distanceVector3(this.transform, target)); //3 observations
        }
        catch
        {
            sensor.AddObservation(distanceVector3(this.transform, this.transform)); //3 observations
            Debug.LogWarning("No target position found. Replaced with null vector3");            
        }
        //sensor.AddObservation(target.position); // 3 observations
        //sensor.AddObservation(this.transform.position); // 3 observations


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

    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 2
        //car_script.horizontalInput = actionBuffers.ContinuousActions[0];
        //car_script.verticalInput = actionBuffers.ContinuousActions[1];

        if(actionBuffers.DiscreteActions[0] == 1)
        {
            car_script.isBreaking = true;
        }
        else if(actionBuffers.DiscreteActions[0] == 0)
        {
            car_script.isBreaking = false;
        }

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


        // Rewards
        float distanceToTarget = Vector3.Distance(this.transform.position, target.position);
              
        if(distanceToTarget < lastDistanceToTarget)
        {
            AddReward(0.00001f);
        }
        else
        {
            AddReward(-0.00005f);
        }


        float current_speed = Mathf.Abs(rBody.velocity.x) + Mathf.Abs(rBody.velocity.z);
        switch(current_situational)
        {
            case situationals.traffic_light:
                switch(situational_object.GetComponent<traffic_light>().current_state)
                {
                    case traffic_light.traffic_states.green:
                        if(current_speed <= 5e-2)
                        {
                            SetReward(-0.2f);
                        }
                        
                        break;
                    case traffic_light.traffic_states.orange:
                    case traffic_light.traffic_states.red:
                        if(current_speed <= 5e-2)
                        {
                            SetReward(0.1f);
                        }
                        else
                        {
                            SetReward(-0.2f);
                        }
                        break;
                }
                break;
        }

        lastDistanceToTarget = distanceToTarget;
        // Fell off platform
        if (this.transform.position.y < 0)
        {
            EndEpisode();
        }

    }
    private void OnTriggerStay(Collider other)
    {
        
        if(other.tag == "Target")
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
        else if(other.tag == "Death")
        {
            SetReward(-1f);
            EndEpisode();
        }
        else if(other.tag == "Traffic_light")
        {
            current_situational = situationals.traffic_light;
            situational_object = other.transform.parent.gameObject;
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if(other.tag == "Traffic_light")
        {
            current_situational = situationals.none;
            situational_object = null;
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
    ReadOnlyCollection<float> observations;
    [SerializeField] List<float> obs = new List<float>();
    private void FixedUpdate()
    {
        observations = this.GetObservations();
        if(observations.Count > 0)
        {
            obs.Clear();
            foreach(float o in observations)
            {
                obs.Add(o);
            }
        }

        if(this.StepCount >= maxStep)
        {
            EndEpisode();
        }
    }
}
