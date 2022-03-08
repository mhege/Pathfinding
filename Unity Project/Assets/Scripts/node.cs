using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class node : MonoBehaviour
{
    // Adopt linked list structure
    // Store heuristic value, total estimated cost, and the current cost
    private float valheur;
    private float valtotal;
    private float currentcost;
    [HideInInspector] public node[] neighbours;
    [HideInInspector] public node prev;

    public node()
    {
        valheur = 0.0f;
        valtotal = 0.0f;
        currentcost = 0.0f;
        neighbours = new node[8];
        prev = null;
    }

    public float getValHeur()
    {
        return valheur;
    }

    public void setValHeur(float valheur)
    {
        this.valheur = valheur;
    }

    public float getValTotal()
    {
        return valtotal;
    }

    public void setValTotal(float valtotal)
    {
        this.valtotal = valtotal;
    }

    public float getCurrentCost()
    {
        return currentcost;
    }

    public void setCurrentCost(float currentcost)
    {
        this.currentcost = currentcost;
    }

}
