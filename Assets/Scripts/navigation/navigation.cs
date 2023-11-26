using System.Collections;
using System.Collections.Generic;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;

public class navigation : MonoBehaviour
{
    [SerializeField] GameObject start_point, end_point;
    [SerializeField] List<road_node> paths;
    [SerializeField] bool debug_calculate_navigation;

    // Start is called before the first frame update
    void Start()
    {
        debug_calculate_navigation = false;
    }

    private class node
    {
        public GameObject road;
        public List<GameObject> connections;
        public bool is_start;
        public bool is_target;
        public bool already_visited;

        public node(GameObject assigned_road, GameObject start_point, GameObject end_point)
        {
            road = assigned_road;
            road_node road_script = road.GetComponentInChildren<road_node>();
            connections = new List<GameObject>();
            foreach(GameObject connect in road_script.connected_road)
            {
                connections.Add(connect);
            }
            is_start = Object.ReferenceEquals(road, start_point);
            is_target = Object.ReferenceEquals(road, end_point);
            already_visited = false;
        }
    }

    private class nav_graph
    {
        private List<node> nodes;
        private GameObject start_point, end_point;

        public nav_graph(GameObject start_point, GameObject end_point)
        {
            nodes = new List<node>();
            this.start_point = start_point;
            this.end_point = end_point;

            nodes.Add(new node(start_point, start_point, end_point));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
