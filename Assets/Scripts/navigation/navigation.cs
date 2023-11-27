using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class navigation : MonoBehaviour
{
    [SerializeField] public GameObject start_point, end_point;
    [SerializeField] List<GameObject> paths;
    [SerializeField] bool debug_calculate_navigation;
    [SerializeField] NavigationGraph game_graph;
    private bool is_init_done;
    public GameObject active_target;
 
    // Start is called before the first frame update
    void Start()
    {
        debug_calculate_navigation = false;
        is_init_done = false;
        active_target = null;
    }

    private class Node
    {
        public GameObject road;
        public Node previous_node, next_node;
        public List<GameObject> connections;
        public bool is_start;
        public bool is_target;
        public bool already_visited;

        public Node(Node previous_node, GameObject road, GameObject start_point, GameObject end_point)
        {
            this.road = road;
            this.previous_node = previous_node;
            this.next_node = null;
            this.is_start = ReferenceEquals(this.road, start_point);
            this.is_target = ReferenceEquals(this.road, end_point);
            this.already_visited = false;
            this.connections = new List<GameObject>();
            
            road_node road_script = road.GetComponentInChildren<road_node>();
            foreach(GameObject connect in road_script.connected_road)
            {
                connections.Add(connect.transform.parent.gameObject);
            }

        }

        
        public void set_visited()
        {
            this.already_visited = true;
        }
    }

    private class NavigationGraph
    {
        public List<Node> nodes, path;
        public Node current_node;
        private GameObject start_point, end_point;
        private int max_safeguard = 500, safeguard;
        private bool found_path;

        public NavigationGraph(GameObject start_point, GameObject end_point)
        {
            nodes = new List<Node>();
            path = new List<Node>();
            this.start_point = start_point;
            this.end_point = end_point;
            found_path = false;
            safeguard = 0;

            this.current_node = new Node(null, start_point, start_point, end_point);
            this.current_node.set_visited();
            nodes.Add(this.current_node);
        }

        private road_node get_nodescript_from_road(GameObject road)
        {
            return road.GetComponentInChildren<road_node>();
        }

        public GameObject get_road_from_node(Node n)
        {
            return n.road.gameObject;
        }

        private bool already_visited(GameObject road)
        {
            foreach(Node n in this.nodes)
            {
                if(ReferenceEquals(n.road, road))
                {
                    return true;
                }
            }
            return false;
        }
        
        public bool findPath()
        {
            int safeguard = 0;
            safeguard++;

            //Debug.Log(current_node.road.gameObject.name + ": count=" + current_node.connections.Count.ToString() + " -> " + current_node.connections[0].transform.parent.name + "/" + current_node.connections[1].transform.parent);
            explore_nodes(current_node);
            while(current_node.previous_node != null)
            {
                if(this.safeguard > this.max_safeguard){break;}
                safeguard++;

                path.Add(current_node);
                current_node.previous_node.next_node = current_node; 
                current_node = current_node.previous_node;
            }
            path.Add(current_node);
            path.Reverse();

            return this.found_path;
        }
        
        public Node explore_nodes(Node node)
        {
            Node next_noad = null;
            foreach(GameObject r in node.connections)
            {
                if(this.safeguard > this.max_safeguard){break;}
                safeguard++;
                
                if(found_path){break;}

                if(!already_visited(r))
                {
                    next_noad = new Node(node, r, this.start_point, this.end_point);
                    next_noad.set_visited();
                    this.nodes.Add(next_noad);
                    if(next_noad.is_target)
                    {
                        this.current_node = next_noad;
                        found_path = true;
                    }
                    else
                    {
                        explore_nodes(next_noad);
                    }
                }
            }
            return next_noad;
        }

    }

    public int activate_navigation(GameObject start_point, GameObject end_point)
    {
        if(!is_init_done)
        {return -1;}
        
        game_graph = new NavigationGraph(start_point,end_point);
        if(game_graph.findPath())
        {
            game_graph.current_node.road.transform.Find("Target Road").gameObject.SetActive(true);
            active_target = game_graph.current_node.road.transform.Find("Target Road").gameObject;
            return 0;
        }
        return -1;
    }

    public void activate_next_node()
    {
        game_graph.current_node.road.transform.Find("Target Road").gameObject.SetActive(false);
        if(game_graph.current_node.next_node != null)
        {
            game_graph.current_node = game_graph.current_node.next_node;
            game_graph.current_node.road.transform.Find("Target Road").gameObject.SetActive(true);
            active_target = game_graph.current_node.road.transform.Find("Target Road").gameObject;
        }
    }

    IEnumerator path_animation(List<GameObject> path)
    {
        foreach(GameObject road in path)
        {
            road.transform.Find("Target Road").gameObject.SetActive(true);  
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(1f);

        foreach(GameObject road in path)
        {
            road.transform.Find("Target Road").gameObject.SetActive(false);
        }

    }

    // Update is called once per frame
    void Update()
    {
        if(!is_init_done && start_point.GetComponentInChildren<road_node>().connected_road.Count>0)
        {
            //game_graph = new NavigationGraph(start_point,end_point);
            is_init_done = true;
        }

        if(debug_calculate_navigation && is_init_done)
        {
            game_graph = new NavigationGraph(start_point,end_point);
            if(game_graph.findPath())
            {
                debug_calculate_navigation = false;
                paths = new List<GameObject>();
                
                foreach(Node n in game_graph.path)
                {
                    paths.Add(game_graph.get_road_from_node(n));
                }
                StartCoroutine(path_animation(paths));
            }
        }
    }
}
