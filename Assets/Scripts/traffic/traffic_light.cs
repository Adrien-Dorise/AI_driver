using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class traffic_light : MonoBehaviour
{
    private Material material;
    private enum traffic_states {red, orange, green};
    private traffic_states current_state;
    private IDictionary<string, Color> traffic_colors;

    private float switch_time, last_time;

    // Start is called before the first frame update
    void Start()
    {
        switch_time = 2f;

        traffic_colors = new Dictionary<string, Color>(){
            {"green", Color.green},
            {"orange", new Color(255,127,0,255)},
            {"red", Color.red},
        };
        material = this.GetComponent<Material>();
        current_state = traffic_states.green;
        material.color = traffic_colors["green"];
        last_time = Time.time;
    }

    void state_machine()
    {
        switch(current_state)
        {
            case traffic_states.green:
                material.color = traffic_colors["orange"];
                current_state = traffic_states.orange;
                break;

            case traffic_states.orange:
                material.color = traffic_colors["red"];
                current_state = traffic_states.red;
                break;

            case traffic_states.red:
                material.color = traffic_colors["green"];
                current_state = traffic_states.green;
                break;

            default:
                material.color = traffic_colors["green"];
                current_state = traffic_states.green;
                break;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if(Time.time - last_time >= switch_time)
        {
            state_machine();
        } 
    }
}
