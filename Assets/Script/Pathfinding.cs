using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Pathfinding Settings")]
    public List<Node> allNodes = new List<Node>();

    [Header("Line Renderer")]
    public LineRenderer line;

    [Header("Traffic System")]
    public int maxNpc = 10;
    public float roadWidth = 3f;
    public LayerMask obstacleLayer;
    public float blockedCost = 999999f;

    private Node startNode;
    private List<Node> path = new List<Node>();
    private List<Node> fixedPath = new List<Node>();
    private Node fixedTarget;
    private int lastPassedNodeIndex = -1;
    private bool needsRecalculation = false;
    private float checkInterval = 0.5f;
    private float lastCheckTime = 0f;

    void Start()
    {
        if (player == null) player = transform;
        ClearLine();
        lastPassedNodeIndex = -1;
    }

    void Update()
    {
        CheckWrongNode();
        UpdateLastPassedNode();
        CheckPathForObstacles();
        DrawPath();
    }

    void CheckPathForObstacles()
    {
        if (fixedPath == null || fixedPath.Count < 2) return;
        if (Time.time - lastCheckTime < checkInterval) return;
        
        lastCheckTime = Time.time;

        int startIndex = Mathf.Max(0, lastPassedNodeIndex + 1);
        
        for (int i = startIndex; i < fixedPath.Count - 1; i++)
        {
            if (fixedPath[i] == null || fixedPath[i + 1] == null) continue;
            
            if (IsEdgeBlocked(fixedPath[i], fixedPath[i + 1]))
            {
                if (!needsRecalculation)
                {
                    needsRecalculation = true;
                    RecalculatePathFromCurrentPosition();
                }
                return;
            }
        }
    }

    public bool IsEdgeBlocked(Node from, Node to)
    {
        if (from == null || to == null) return false;
        
        Collider[] hits = Physics.OverlapCapsule(
            from.transform.position,
            to.transform.position,
            roadWidth
        );
        
        foreach (Collider hit in hits)
        {
            if (((1 << hit.gameObject.layer) & obstacleLayer) != 0)
                return true;
        }
        return false;
    }

    void CheckWrongNode()
    {
        if (fixedPath == null || fixedPath.Count == 0) return;
        
        float radius = 8f;
        var delivery = FindFirstObjectByType<DeliveryManualAI>();
        if (delivery != null) radius = delivery.nodeRadius;
        
        Node nearestNode = GetNearestNode(player.position);
        if (nearestNode == null) return;
        
        float distToNearest = Vector3.Distance(player.position, nearestNode.transform.position);
        
        if (distToNearest < radius)
        {
            int nodeIndex = fixedPath.IndexOf(nearestNode);
            
            if (nodeIndex == -1 || nodeIndex <= lastPassedNodeIndex)
            {
                if (!needsRecalculation)
                {
                    needsRecalculation = true;
                    RecalculatePathFromCurrentPosition();
                }
            }
        }
    }

    void RecalculatePathFromCurrentPosition()
    {
        if (fixedTarget == null) return;
        
        Node newStartNode = GetNearestValidNode(player.position);
        
        if (newStartNode != null)
        {
            startNode = newStartNode;
            FindPath(startNode, fixedTarget);
            fixedPath = new List<Node>(path);
            lastPassedNodeIndex = -1;
            needsRecalculation = false;
        }
    }

    Node GetNearestValidNode(Vector3 position)
    {
        Node bestNode = null;
        float bestDistance = Mathf.Infinity;
        
        foreach (Node node in allNodes)
        {
            if (node == null) continue;
            if (node.neighbors == null || node.neighbors.Count == 0) continue;
            
            float dist = Vector3.Distance(position, node.transform.position);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestNode = node;
            }
        }
        return bestNode;
    }

    void UpdateLastPassedNode()
    {
        if (fixedPath == null || fixedPath.Count == 0) return;
        
        float radius = 8f;
        var delivery = FindFirstObjectByType<DeliveryManualAI>();
        if (delivery != null) radius = delivery.nodeRadius;
        
        for (int i = lastPassedNodeIndex + 1; i < fixedPath.Count; i++)
        {
            if (fixedPath[i] == null) continue;
            
            float distToPlayer = Vector3.Distance(player.position, fixedPath[i].transform.position);
            if (distToPlayer < radius)
            {
                lastPassedNodeIndex = i;
            }
        }
    }

    void DrawPath()
    {
        if (line == null) return;
        if (player == null) return;
        
        if (fixedPath == null || fixedPath.Count < 2)
        {
            ClearLine();
            return;
        }
        
        int startIndex = lastPassedNodeIndex + 1;
        
        if (startIndex >= fixedPath.Count)
        {
            ClearLine();
            return;
        }
        
        // Validasi jalur sebelum menggambar
        List<Node> remainingPath = new List<Node>();
        for (int i = startIndex; i < fixedPath.Count; i++)
        {
            remainingPath.Add(fixedPath[i]);
        }
        
        if (remainingPath.Count == 0)
        {
            ClearLine();
            return;
        }
        
        // Cek apakah ada obstacle di jalur yang tersisa
        bool hasObstacle = false;
        for (int i = 0; i < remainingPath.Count - 1; i++)
        {
            if (IsEdgeBlocked(remainingPath[i], remainingPath[i + 1]))
            {
                hasObstacle = true;
                break;
            }
        }
        
        if (hasObstacle)
        {
            ClearLine();
            if (!needsRecalculation)
            {
                needsRecalculation = true;
                RecalculatePathFromCurrentPosition();
            }
            return;
        }
        
        line.positionCount = remainingPath.Count + 1;
        line.SetPosition(0, player.position + Vector3.up * 0.5f);
        
        for (int i = 0; i < remainingPath.Count; i++)
        {
            line.SetPosition(i + 1, remainingPath[i].transform.position + Vector3.up * 0.5f);
        }
    }

    void ClearLine()
    {
        if (line != null) line.positionCount = 0;
    }

    void UpdateStartNode()
    {
        if (player == null) return;

        Node bestNode = null;
        float bestDistance = Mathf.Infinity;

        foreach (Node node in allNodes)
        {
            if (node == null) continue;
            if (node.neighbors == null || node.neighbors.Count == 0) continue;

            float distance = Vector3.Distance(player.position, node.transform.position);
            if (distance < 5f) continue;
            
            Vector3 dirToNode = (node.transform.position - player.position).normalized;
            float dot = Vector3.Dot(player.forward, dirToNode);
            if (dot < 0.3f) continue;
            
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestNode = node;
            }
        }

        if (bestNode != null) 
        {
            startNode = bestNode;
        }
        else
        {
            foreach (Node node in allNodes)
            {
                if (node == null) continue;
                if (node.neighbors == null || node.neighbors.Count == 0) continue;
                float dist = Vector3.Distance(player.position, node.transform.position);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestNode = node;
                }
            }
            startNode = bestNode;
        }
    }

    Node GetValidStartNode()
    {
        Node bestNode = null;
        float bestDistance = Mathf.Infinity;
        
        foreach (Node node in allNodes)
        {
            if (node == null) continue;
            if (node.neighbors == null || node.neighbors.Count == 0) continue;
            if (Vector3.Distance(player.position, node.transform.position) < 3f) continue;
            
            float dist = Vector3.Distance(player.position, node.transform.position);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestNode = node;
            }
        }
        return bestNode;
    }

    void ResetNodes()
    {
        foreach (Node node in allNodes)
        {
            if (node != null)
            {
                node.gCost = Mathf.Infinity;
                node.hCost = 0f;
                node.parent = null;
            }
        }
    }

    void FindPath(Node start, Node target)
    {
        if (start == null || target == null)
        {
            path.Clear();
            return;
        }

        if (start.neighbors == null || start.neighbors.Count == 0)
        {
            start = GetValidStartNode();
            if (start == null)
            {
                path.Clear();
                return;
            }
        }

        if (target.neighbors == null || target.neighbors.Count == 0)
        {
            path.Clear();
            return;
        }

        if (start == target)
        {
            path.Clear();
            path.Add(start);
            return;
        }

        ResetNodes();
        path.Clear();

        List<Node> openList = new List<Node>();
        HashSet<Node> closedList = new HashSet<Node>();

        start.gCost = 0f;
        openList.Add(start);

        while (openList.Count > 0)
        {
            Node currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fCost < currentNode.fCost ||
                    (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost))
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            if (currentNode == target)
            {
                RetracePath(start, target);
                return;
            }

            if (currentNode.neighbors == null || currentNode.neighbors.Count == 0) continue;

            foreach (Node neighbor in currentNode.neighbors)
            {
                if (neighbor == null) continue;
                if (closedList.Contains(neighbor)) continue;

                // CEK OBSTACLE - SKIP LANGSUNG JIKA TERBLOKIR
                if (IsEdgeBlocked(currentNode, neighbor))
                {
                    continue; // Skip neighbor ini, tidak bisa dilewati
                }

                float distanceCost = Vector3.Distance(currentNode.transform.position, neighbor.transform.position);
                float totalCost = currentNode.gCost + distanceCost;

                // CEK NPC COUNT (opsional)
                Collider[] hits = Physics.OverlapCapsule(
                    currentNode.transform.position,
                    neighbor.transform.position,
                    roadWidth
                );

                int npcCount = 0;
                foreach (Collider hit in hits)
                {
                    if (hit.CompareTag("NPC")) npcCount++;
                }

                if (npcCount >= maxNpc) totalCost += blockedCost;

                if (totalCost < neighbor.gCost || !openList.Contains(neighbor))
                {
                    neighbor.gCost = totalCost;
                    neighbor.hCost = Vector3.Distance(neighbor.transform.position, target.transform.position);
                    neighbor.parent = currentNode;

                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }
        }

        // Jika tidak ada path yang ditemukan, coba cari alternatif
        if (path.Count == 0)
        {
            // Coba cari path dengan mengabaikan obstacle (fallback)
            FindPathWithFallback(start, target);
        }
    }

    void FindPathWithFallback(Node start, Node target)
    {
        // Reset nodes
        foreach (Node node in allNodes)
        {
            if (node != null)
            {
                node.gCost = Mathf.Infinity;
                node.hCost = 0f;
                node.parent = null;
            }
        }

        List<Node> openList = new List<Node>();
        HashSet<Node> closedList = new HashSet<Node>();

        start.gCost = 0f;
        openList.Add(start);

        while (openList.Count > 0)
        {
            Node currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fCost < currentNode.fCost ||
                    (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost))
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            if (currentNode == target)
            {
                RetracePath(start, target);
                return;
            }

            if (currentNode.neighbors == null || currentNode.neighbors.Count == 0) continue;

            foreach (Node neighbor in currentNode.neighbors)
            {
                if (neighbor == null) continue;
                if (closedList.Contains(neighbor)) continue;

                // Fallback: tetap bisa lewat obstacle tapi dengan cost tinggi
                float distanceCost = Vector3.Distance(currentNode.transform.position, neighbor.transform.position);
                float totalCost = currentNode.gCost + distanceCost;

                if (IsEdgeBlocked(currentNode, neighbor))
                {
                    totalCost += blockedCost * 2; // Cost lebih tinggi
                }

                if (totalCost < neighbor.gCost || !openList.Contains(neighbor))
                {
                    neighbor.gCost = totalCost;
                    neighbor.hCost = Vector3.Distance(neighbor.transform.position, target.transform.position);
                    neighbor.parent = currentNode;

                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }
        }

        path.Clear();
    }

    void RetracePath(Node start, Node target)
    {
        path.Clear();
        Node currentNode = target;

        while (currentNode != start)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
            if (currentNode == null)
            {
                path.Clear();
                return;
            }
        }

        path.Add(start);
        path.Reverse();
    }

    public void SetTargetNode(Node newTarget)
    {
        if (newTarget == null) return;
        if (newTarget == fixedTarget) return;
        
        fixedTarget = newTarget;
        lastPassedNodeIndex = -1;
        needsRecalculation = false;
        
        UpdateStartNode();
        if (startNode == null) startNode = GetValidStartNode();
        
        if (startNode != null && fixedTarget != null)
        {
            FindPath(startNode, fixedTarget);
            fixedPath = new List<Node>(path);
        }
    }

    public void ClearTarget()
    {
        fixedTarget = null;
        fixedPath.Clear();
        path.Clear();
        lastPassedNodeIndex = -1;
        needsRecalculation = false;
        ClearLine();
    }
    
    public void ResetTarget()
    {
        path.Clear();
        if (line != null)
        {
            line.positionCount = 0;
        }
    }   
    

    public float GetDistanceBetweenNodes(Node from, Node to)
    {
        if (from == null || to == null) return -1f;
        
        FindPath(from, to);
        if (path == null || path.Count == 0) return -1f;
        
        float total = 0f;
        Vector3 prevPos = from.transform.position;
        foreach (Node node in path)
        {
            if (node == null) continue;
            total += Vector3.Distance(prevPos, node.transform.position);
            prevPos = node.transform.position;
        }
        return total;
    }

    public List<Node> GetCurrentPath() => new List<Node>(fixedPath);
    public bool HasValidTarget() => fixedTarget != null && fixedPath.Count > 0;
    
    public Node GetNearestNode(Vector3 position)
    {
        Node closest = null;
        float closestDist = Mathf.Infinity;
        foreach (Node node in allNodes)
        {
            if (node == null) continue;
            float dist = Vector3.Distance(position, node.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = node;
            }
        }
        return closest;
    }

    // Method untuk debugging - visualisasi obstacle
    void OnDrawGizmos()
    {
        if (fixedPath == null || fixedPath.Count < 2) return;
        
        Gizmos.color = Color.yellow;
        for (int i = 0; i < fixedPath.Count - 1; i++)
        {
            if (fixedPath[i] == null || fixedPath[i + 1] == null) continue;
            
            if (IsEdgeBlocked(fixedPath[i], fixedPath[i + 1]))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(fixedPath[i].transform.position, fixedPath[i + 1].transform.position);
                Gizmos.color = Color.yellow;
            }
        }
    }
}