using System;
using System.Collections.Generic;
using UnityEngine;

public class pathfinding : MonoBehaviour
{
    // Setup access to player character
    GameObject player;
    private const string tagP1 = "Player";

    // Variables used for modes:
    // ClickStartEnd is for custom path setting with the mouse
    // manhatten_T_cluster_F is which heuristic algorithm to use. True for manhatten, false for cluster
    public bool clickStartEnd = true;
    private bool begin;
    public bool manhatten_T_cluster_F = true;

    // All variables and data structures used for graph and pathfinding
    // First list block is for A* and smoothing
    // Second is to store the small rooms
    // Third to store the clusters and evaluate the heuristic
    int numNodesVisited = 1;
    private Vector3 nodeDistance = new Vector3(1.0f, 0.0f, 1.0f);
    private const float rs = .25f;

    private node start = null;
    private node target = null;
    private List<node> nodes = new List<node>();
    private List<node> nodesOpen = new List<node>();
    private List<node> nodesClosed = new List<node>();
    private List<node> nodesPath = new List<node>();
    private List<node> nodesPathSmooth = new List<node>();

    private int startChamber;
    private int targetChamber;
    private List<node> chamber1 = new List<node>();
    private List<node> chamber2 = new List<node>();
    private List<node> chamber3 = new List<node>();
    private List<node> chamber4 = new List<node>();

    private List<List<float>> nodeClusters = new List<List<float>>();
    private const int numClusters = 4;
    private const int layerClusterOffset = 6;
    private List<node> cluster1 = new List<node>();
    private List<node> cluster2 = new List<node>();
    private List<node> cluster3 = new List<node>();
    private List<node> cluster4 = new List<node>();
    private List<Vector3> clusterCents = new List<Vector3>();

    void Start()
    {
        // Initialize the graph based on the mode selected
        // Click to start
        // Manhatten or Cluster

        begin = !clickStartEnd;

        player = GameObject.FindGameObjectWithTag(tagP1);
        map();
        clusterCentroid();
        clusterTable();

        //color();

        if (begin)
        {
            randomStart();
            randomTarget();

            if (manhatten_T_cluster_F)
                manhattenPath();
            else
                clusterPath();

            smooth();
        }
        
    }

    void Update()
    {

        if (!begin)
            selectStartEnd();

        if (begin)
        {   
            // Color the open, closed, grid path, smoothed grid path, start, and target nodes.
            foreach (node node in nodesOpen)
            {
                node.GetComponent<Renderer>().material.color = Color.magenta;
            }

            foreach (node node in nodesClosed)
            {
                node.GetComponent<Renderer>().material.color = Color.white;
            }

            foreach (node node in nodesPath)
            {
                node.GetComponent<Renderer>().material.color = Color.green;
            }

            foreach (node node in nodesPathSmooth)
            {
                node.GetComponent<Renderer>().material.color = Color.yellow;
            }

            start.GetComponent<Renderer>().material.color = Color.red;
            target.GetComponent<Renderer>().material.color = Color.red;

            // Move the player based on path following and alignment
            if (numNodesVisited < nodesPathSmooth.Count)
            {
                if (Vector3.Angle(player.transform.forward, nodesPathSmooth[numNodesVisited].transform.position - player.transform.position) > 50.0f)
                {
                    player.GetComponent<motion>().arrive();
                    player.GetComponent<motion>().align(nodesPathSmooth[numNodesVisited]);
                }
                else
                {
                    if (numNodesVisited == nodesPathSmooth.Count - 1)
                        player.GetComponent<motion>().moveToTarget(nodesPathSmooth[numNodesVisited]);
                    else
                        player.GetComponent<motion>().moveToNode(nodesPathSmooth[numNodesVisited]);
                }

                if ((player.transform.position - nodesPathSmooth[numNodesVisited].transform.position).magnitude < rs)
                    numNodesVisited += 1;

            }
            else
                player.GetComponent<motion>().arrive();
        }

    }

    //Colors rooms and clusters for display purposes
    private void color()
    {
        /*
        foreach (node node in chamber1)
        {
            node.GetComponent<Renderer>().material.color = Color.magenta;
        }

        foreach (node node in chamber2)
        {
            node.GetComponent<Renderer>().material.color = Color.red;
        }

        foreach (node node in chamber3)
        {
            node.GetComponent<Renderer>().material.color = Color.cyan;
        }

        foreach (node node in chamber4)
        {
            node.GetComponent<Renderer>().material.color = Color.white;
        }
        */

        foreach (node node in cluster1)
        {
            node.GetComponent<Renderer>().material.color = Color.magenta;
        }

        foreach (node node in cluster2)
        {
            node.GetComponent<Renderer>().material.color = Color.red;
        }

        foreach (node node in cluster3)
        {
            node.GetComponent<Renderer>().material.color = Color.cyan;
        }

        foreach (node node in cluster4)
        {
            node.GetComponent<Renderer>().material.color = Color.white;
        }
    }

    // Find all nodes on the graph and fill rooms
    private void map()
    {
        GameObject[] nodesGO = GameObject.FindGameObjectsWithTag("node");
        
        foreach(GameObject GO in nodesGO)
        {
            nodes.Add(GO.GetComponent<node>());
        }
        
        foreach(node n in nodes)
        {
            nodeNeighbours(n);

            if (n.transform.position.x < -7.0f && n.transform.position.z < -6.0f)
                chamber1.Add(n);
            else if (n.transform.position.x > 8.0f && n.transform.position.z < -3.0f)
                chamber2.Add(n);
            else if (n.transform.position.x > 7.0f && n.transform.position.z < 7.0f && n.transform.position.z > 2.0f)
                chamber3.Add(n);
            else if (n.transform.position.x < -8.0f && n.transform.position.z > 8.0)
                chamber4.Add(n);
        }
        
    }

    // Choose the start and end points of the pathfinding algorithm with mouse clicks
    private void selectStartEnd()
    {
        bool hitNode = false;
        bool found = false;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100.0f))
            {
                
                foreach (node node in nodes){
                    if (node.transform == hit.transform)
                        hitNode = true;
                }

                if (hitNode)
                {
                    if (start == null)
                    {
                        
                        foreach (node node in chamber1)
                        {
                            if (node.transform == hit.transform)
                            {
                                start = node;
                                startChamber = 1;
                                Debug.Log("Start node registered in chamber " + startChamber);
                            }  
                        }

                        foreach (node node in chamber2)
                        {
                            if (node.transform == hit.transform)
                            {
                                start = node;
                                startChamber = 2;
                                Debug.Log("Start node registered in chamber " + startChamber);
                            }
                        }

                        foreach (node node in chamber3)
                        {
                            if (node.transform == hit.transform)
                            {
                                start = node;
                                startChamber = 3;
                                Debug.Log("Start node registered in chamber " + startChamber);
                            }
                        }

                        foreach (node node in chamber4)
                        {
                            if (node.transform == hit.transform)
                            {
                                start = node;
                                startChamber = 4;
                                Debug.Log("Start node registered in chamber " + startChamber);
                            }
                        }


                    }
                    else if (target == null)
                    {
                        foreach (node node in chamber1)
                        {
                            if (node.transform == hit.transform && startChamber != 1)
                            {
                                target = node;
                                found = true;
                                Debug.Log("Target node registered in chamber " + 1);
                            }
                                
                        }

                        foreach (node node in chamber2)
                        {
                            if (node.transform == hit.transform && startChamber != 2)
                            {
                                target = node;
                                found = true;
                                Debug.Log("Start node registered in chamber " + 2);
                            }
                        }

                        foreach (node node in chamber3)
                        {
                            if (node.transform == hit.transform && startChamber != 3)
                            {
                                target = node;
                                found = true;
                                Debug.Log("Start node registered in chamber " + 3);
                            }
                        }

                        foreach (node node in chamber4)
                        {
                            if (node.transform == hit.transform && startChamber != 4)
                            {
                                target = node;
                                found = true;
                                Debug.Log("Start node registered in chamber " + 4);
                            }
                        }

                        if (found)
                        {
                            player.transform.position = start.transform.position;

                            if (manhatten_T_cluster_F)
                                manhattenPath();
                            else
                                clusterPath();

                            smooth();
                            begin = true;
                        }
                        
                    }

                }

            }

        }

    }

    // Heuristic cost based on manhatten distance
    private float manhatten(node n, node othern)
    {
        Vector3 diff = n.transform.position - othern.transform.position;
        float cost = Math.Abs(diff.x) + Math.Abs(diff.z);
        return cost;
    }

    // Randomly pick a starting location within a room
    private void randomStart()
    {
        startChamber = UnityEngine.Random.Range(0, 4);

        switch (startChamber)
        {
            case 0:
                start = chamber1[UnityEngine.Random.Range(0, chamber1.Count)];
                break;
            case 1:
                start = chamber2[UnityEngine.Random.Range(0, chamber2.Count)];
                break;
            case 2:
                start = chamber3[UnityEngine.Random.Range(0, chamber3.Count)];
                break;
            case 3:
                start = chamber4[UnityEngine.Random.Range(0, chamber4.Count)];
                break;
        }

        player.transform.position = new Vector3(start.transform.position.x, player.transform.position.y, start.transform.position.z);

    }

    // Randomly pick a target in a room that isnt the same as the start
    private void randomTarget()
    {
        do
        {
            targetChamber = UnityEngine.Random.Range(0, 4);
        } while (targetChamber == startChamber);

        switch (targetChamber)
        {
            case 0:
                target = chamber1[UnityEngine.Random.Range(0, chamber1.Count)];
                break;
            case 1:
                target = chamber2[UnityEngine.Random.Range(0, chamber2.Count)];
                break;
            case 2:
                target = chamber3[UnityEngine.Random.Range(0, chamber3.Count)];
                break;
            case 3:
                target = chamber4[UnityEngine.Random.Range(0, chamber4.Count)];
                break;
        }

    }

    // Find all the neighbours of a select node
    private void nodeNeighbours(node node)
    {
        node.neighbours[0] = nodes.Find(othernode => othernode.transform.position - node.transform.position == new Vector3(1.0f * nodeDistance.x, 1.0f * nodeDistance.y, 0.0f * nodeDistance.z));
        node.neighbours[1] = nodes.Find(othernode => othernode.transform.position - node.transform.position == new Vector3(-1.0f * nodeDistance.x, 1.0f * nodeDistance.y, 0.0f * nodeDistance.z));
        node.neighbours[2] = nodes.Find(othernode => othernode.transform.position - node.transform.position == new Vector3(0.0f * nodeDistance.x, 1.0f * nodeDistance.y, 1.0f * nodeDistance.z));
        node.neighbours[3] = nodes.Find(othernode => othernode.transform.position - node.transform.position == new Vector3(0.0f * nodeDistance.x, 1.0f * nodeDistance.y, -1.0f * nodeDistance.z));
        node.neighbours[4] = nodes.Find(othernode => othernode.transform.position - node.transform.position == new Vector3(1.0f * nodeDistance.x, 1.0f * nodeDistance.y, 1.0f * nodeDistance.z));
        node.neighbours[5] = nodes.Find(othernode => othernode.transform.position - node.transform.position == new Vector3(1.0f * nodeDistance.x, 1.0f * nodeDistance.y, -1.0f * nodeDistance.z));
        node.neighbours[6] = nodes.Find(othernode => othernode.transform.position - node.transform.position == new Vector3(-1.0f * nodeDistance.x, 1.0f * nodeDistance.y, 1.0f * nodeDistance.z));
        node.neighbours[7] = nodes.Find(othernode => othernode.transform.position - node.transform.position == new Vector3(-1.0f * nodeDistance.x, 1.0f * nodeDistance.y, -1.0f * nodeDistance.z));
    }

    // Solve for the shortest path using A* based on the manhatten heuristic
    private void manhattenPath()
    {

        nodesOpen.Add(start);
        start.setValHeur(manhatten(start, target));
        start.setValTotal(start.getValHeur() + start.getCurrentCost());

        while (nodesOpen.Count != 0)
        {
            node eval = nodesOpen[0];

            foreach(node n in nodesOpen)
            {
                if (eval.getValTotal() > n.getValTotal())
                    eval = n;
            }

            if (target == eval)
                break;

            nodesClosed.Add(eval);
            nodesOpen.Remove(eval);

            foreach(node othern in eval.neighbours)
            {
                bool inOpen = false;
                bool inClosed = false;

                if (othern == null)
                    continue;

                if (nodesOpen.Contains(othern))
                    inOpen = true;

                if (nodesClosed.Contains(othern))
                    inClosed = true;

                float totalCost = manhatten(eval, othern) + eval.getCurrentCost();
                othern.setValHeur(manhatten(eval, othern));

                if(inClosed && totalCost < othern.getCurrentCost())
                {
                    othern.setCurrentCost(totalCost);
                    othern.setValTotal(othern.getCurrentCost() + othern.getValHeur());
                    othern.prev = eval;
                    nodesClosed.Remove(othern);
                    nodesOpen.Add(othern);
                }else if(inOpen && totalCost < othern.getCurrentCost())
                {
                    othern.setCurrentCost(totalCost);
                    othern.setValTotal(othern.getCurrentCost() + othern.getValHeur());
                    othern.prev = eval;
                }
                else if(!inOpen && !inClosed)
                {
                    othern.setCurrentCost(totalCost);
                    othern.setValTotal(othern.getCurrentCost() + othern.getValHeur());
                    othern.prev = eval;
                    nodesOpen.Add(othern);
                }

            }

        }


        nodesPath.Add(target);
        formPath();

    }

    // Solve for the shortest path using A* based on the cluster and manhatten heuristics
    private void clusterPath()
    {

        nodesOpen.Add(start);
        start.setValHeur(manhatten(start, target));
        start.setValTotal(start.getValHeur() + start.getCurrentCost());

        while (nodesOpen.Count != 0)
        {
            node eval = nodesOpen[0];

            foreach (node n in nodesOpen)
            {
                if (eval.getValTotal() > n.getValTotal())
                    eval = n;
            }

            if (target == eval)
                break;

            nodesClosed.Add(eval);
            nodesOpen.Remove(eval);

            foreach (node othern in eval.neighbours)
            {
                bool inOpen = false;
                bool inClosed = false;

                if (othern == null)
                    continue;

                if (nodesOpen.Contains(othern))
                    inOpen = true;

                if (nodesClosed.Contains(othern))
                    inClosed = true;

                float totalCost = manhatten(eval, othern) + eval.getCurrentCost();
                othern.setValHeur(manhatten(eval, othern) + cluster(othern.gameObject.layer, target.gameObject.layer));

                if (inClosed && totalCost < othern.getCurrentCost())
                {
                    othern.setCurrentCost(totalCost);
                    othern.setValTotal(othern.getCurrentCost() + othern.getValHeur());
                    othern.prev = eval;
                    nodesClosed.Remove(othern);
                    nodesOpen.Add(othern);
                }
                else if (inOpen && totalCost < othern.getCurrentCost())
                {
                    othern.setCurrentCost(totalCost);
                    othern.setValTotal(othern.getCurrentCost() + othern.getValHeur());
                    othern.prev = eval;
                }
                else if (!inOpen && !inClosed)
                {
                    othern.setCurrentCost(totalCost);
                    othern.setValTotal(othern.getCurrentCost() + othern.getValHeur());
                    othern.prev = eval;
                    nodesOpen.Add(othern);
                }

            }

        }


        nodesPath.Add(target);
        formPath();

    }

    // Recursively flip the path from A*
    private void formPath()
    {
        if(start == nodesPath[nodesPath.Count - 1].prev)
        {
            nodesPath.Add(nodesPath[nodesPath.Count - 1].prev);
            nodesPath.Reverse();
            return;
        }
        else
        {
            nodesPath.Add(nodesPath[nodesPath.Count - 1].prev);
            formPath();
        }

    } 

    // Find the centroid of each cluster
    private void clusterCentroid()
    {

        Vector3 Vcent1 = new Vector3(0, 0, 0);
        Vector3 Vcent2 = new Vector3(0, 0, 0);
        Vector3 Vcent3 = new Vector3(0, 0, 0);
        Vector3 Vcent4 = new Vector3(0, 0, 0);

        foreach (node node in nodes)
        {
            if (node.gameObject.layer == 6)
                cluster1.Add(node);
            if (node.gameObject.layer == 7)
                cluster2.Add(node);
            if (node.gameObject.layer == 8)
                cluster3.Add(node);
            if (node.gameObject.layer == 9)
                cluster4.Add(node);
        }

        foreach (node node in cluster1)
            Vcent1 += node.transform.position;

        foreach (node node in cluster2)
            Vcent2 += node.transform.position;

        foreach (node node in cluster3)
            Vcent3 += node.transform.position;

        foreach (node node in cluster4)
            Vcent4 += node.transform.position;

        clusterCents.Add(new Vector3(Vcent1.x / cluster1.Count, Vcent1.y / cluster1.Count, Vcent1.z / cluster1.Count));
        clusterCents.Add(new Vector3(Vcent2.x / cluster2.Count, Vcent2.y / cluster2.Count, Vcent2.z / cluster2.Count));
        clusterCents.Add(new Vector3(Vcent3.x / cluster3.Count, Vcent3.y / cluster3.Count, Vcent3.z / cluster3.Count));
        clusterCents.Add(new Vector3(Vcent4.x / cluster4.Count, Vcent4.y / cluster4.Count, Vcent4.z / cluster4.Count));
    }

    // Generate the look up table using the cluster centroids
    private void clusterTable()
    {

        for(int i = 0; i < numClusters; i++)
        {
            nodeClusters.Add(new List<float>());

            for(int j = 0; j < numClusters; j++)
            {
                if (j == i)
                {
                    nodeClusters[i].Add(0.0f);
                    //Debug.Log(0.0f);
                }
                else
                {
                    nodeClusters[i].Add(Math.Abs((clusterCents[i] - clusterCents[j]).x) + Math.Abs((clusterCents[i] - clusterCents[j]).z));
                    //Debug.Log(Math.Abs((clusterCents[i] - clusterCents[j]).x) + Math.Abs((clusterCents[i] - clusterCents[j]).z));
                }
                    
            }

        }

    }

    // Selects the correct heuristic value from the lookup table
    private float cluster(int othern, int target)
    {

        int othernIndex = othern - layerClusterOffset;
        int targetIndex = target - layerClusterOffset;


        return nodeClusters[othernIndex][targetIndex];
    }

    // Smoothes out the grid tile path using raycasting
    private void smooth()
    {

        foreach (node node in nodesPath){
            node.gameObject.layer = 3;
        }

        nodesPathSmooth.Add(nodesPath[0]);

        int rayNode = 0;
        int targetNode = 0;

        RaycastHit hit;
        int layerMask = 1 << 3;

        while (targetNode != nodesPath.Count - 1)
        {
            targetNode += 1;

            if(Physics.Raycast(nodesPath[rayNode].transform.position, (nodesPath[targetNode].transform.position - nodesPath[rayNode].transform.position), out hit, 24.0f, layerMask) && (hit.transform == nodesPath[targetNode].transform))
            {
                nodesPath[targetNode].gameObject.layer = 2;
            }
            else
            {
                targetNode -= 1;
                nodesPathSmooth.Add(nodesPath[targetNode]);
                rayNode = targetNode;
            }
            
        }

        nodesPathSmooth.Add(nodesPath[nodesPath.Count - 1]);
      
    }

}
