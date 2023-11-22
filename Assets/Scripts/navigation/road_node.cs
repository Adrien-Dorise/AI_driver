using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class road_node : MonoBehaviour
{
    //connected_road variable is to set in the editor
    [SerializeField] private List<GameObject> connected_road;

    // Start is called before the first frame update
    void Start()
    {
        connected_road = new List<GameObject>();
    }

    private void OnTriggerStay(Collider other)
    {
        connected_road.Add(other.gameObject);
    }

     private void OnCollisionEnter(Collision other)
    {
        connected_road.Add(other.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
