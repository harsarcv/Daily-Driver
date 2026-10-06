using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Node : MonoBehaviour
{
    [Header("Connection")]
    public List<Node> neighbors = new List<Node>();

    [HideInInspector]
    public Node parent;

    [HideInInspector]
    public float gCost;

    [HideInInspector]
    public float hCost;

    public float fCost
    {
        get { return gCost + hCost; }
    }

    // ==================================================
    // GIZMOS
    // ==================================================

    void OnDrawGizmos()
    {
        // NODE
        Gizmos.color = Color.red;

        Gizmos.DrawSphere(
            transform.position,
            0.5f
        );

#if UNITY_EDITOR

        // CONNECTION
        Handles.color = Color.yellow;

        foreach (Node neighbor in neighbors)
        {
            if (neighbor != null)
            {
                Handles.DrawAAPolyLine(
                    6f, // ketebalan garis
                    transform.position,
                    neighbor.transform.position
                );
            }
        }

#endif
    }
}