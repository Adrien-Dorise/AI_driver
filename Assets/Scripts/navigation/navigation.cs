using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
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

    private class Node
    {
        public GameObject road;
        public List<GameObject> connections;
        public bool is_start;
        public bool is_target;
        public bool already_visited;

        public Node(GameObject assigned_road, GameObject start_point, GameObject end_point)
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

        public Node(GameObject assigned_road)
        {
            road = assigned_road;
            road_node road_script = road.GetComponentInChildren<road_node>();
            connections = new List<GameObject>();
            foreach(GameObject connect in road_script.connected_road)
            {
                connections.Add(connect);
            }
            is_start = false;
            is_target = false;
            already_visited = false;
        }
    }

    private class NavigationGraph
    {
        private List<Node> nodes;
        private List<Node> path;
        private Node current_node;
        private GameObject start_point, end_point;

        public NavigationGraph(GameObject start_point, GameObject end_point)
        {
            nodes = new List<Node>();
            path = new List<Node>();
            this.start_point = start_point;
            this.end_point = end_point;

            current_node = get_node_from_road(start_point);
            nodes.Add(new Node(start_point, start_point, end_point));
            path.Add(new Node(start_point, start_point, end_point));
        }

        private road_node get_nodescript_from_road(GameObject road)
        {
            return road.GetComponentInChildren<road_node>();
        }

        private GameObject get_road_from_node(Node n)
        {
            return n.road.gameObject.transform.parent.gameObject;
        }

        private Node get_node_from_road(GameObject road)
        {
            return new Node(road);
        }

        public void findPath()
        {
            int safeguard = 0;
            while(!Object.ReferenceEquals(current_node, end_point))
            {
                if(safeguard > 1000){break;}
                safeguard++;
                GameObject next_road = current_node.connections[Random.Range(0,current_node.connections.Count)];
                current_node = get_node_from_road(next_road);

            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
