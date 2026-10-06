using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NPCNodeMovement : MonoBehaviour
{
    [Header("Pathfinding")]
    public Pathfinding pathfinding;

    private Node currentNode;
    private Node targetNode;
    private Node previousNode;
    private Rigidbody rb;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float rotateSpeed = 5f;
    public float reachDistance = 2f;

    [Header("Random Behavior")]
    [Range(0f, 1f)]
    public float chanceToGoStraight = 0.4f;
    [Range(0f, 1f)]
    public float chanceToTurnRight = 0.3f;
    [Range(0f, 1f)]
    public float chanceToTurnLeft = 0.3f;

    [Header("Distance Preference")]
    [Range(0f, 1f)]
    public float preferLongPath = 0.3f;

    [Header("Deteksi Kemacetan")]
    public float nodeCheckRadius = 4f;
    public int maxNPCBeforeAvoid = 3;
    public int lookAheadNodes = 2;

    // ========== OBSTACLE AVOIDANCE ==========
    [Header("Obstacle Avoidance")]
    public int obstacleLookAhead = 2; // Berapa node ke depan dicek

    // ========== ANTI MUTER ==========
    private Queue<Node> nodeHistory = new Queue<Node>();
    public int historySize = 3;

    // ========== CEGAH BELOK KANAN TERLALU SERING ==========
    private int rightTurnCount = 0;
    public int maxRightTurnInRow = 2;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        if (pathfinding == null)
            pathfinding = FindFirstObjectByType<Pathfinding>();
        FindNearestNode();
    }

    void FixedUpdate()
    {
        if (targetNode == null)
        {
            FindNearestNode();
            return;
        }

        Vector3 targetPos = targetNode.transform.position;
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0f;
        float distance = Vector3.Distance(transform.position, targetPos);

        if (distance < reachDistance)
        {
            UpdateRightTurnCount();
            
            if (currentNode != null)
            {
                nodeHistory.Enqueue(currentNode);
                if (nodeHistory.Count > historySize)
                    nodeHistory.Dequeue();
            }
            
            previousNode = currentNode;
            currentNode = targetNode;
            ChooseNextNode();
            return;
        }

        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime);
            rb.linearVelocity = transform.forward * moveSpeed;
        }
    }

    // =============================================
    // OBSTACLE DETECTION via Pathfinding
    // =============================================

    bool IsPathHasObstacleAhead(Node startNode)
    {
        if (startNode == null || pathfinding == null) return false;
        
        HashSet<Node> visited = new HashSet<Node>();
        Queue<Node> toCheck = new Queue<Node>();
        toCheck.Enqueue(startNode);
        
        for (int depth = 0; depth < obstacleLookAhead && toCheck.Count > 0; depth++)
        {
            int levelSize = toCheck.Count;
            for (int i = 0; i < levelSize; i++)
            {
                Node node = toCheck.Dequeue();
                if (visited.Contains(node)) continue;
                visited.Add(node);
                
                // Cek edge dari node ke neighbor-nya
                foreach (Node neighbor in node.neighbors)
                {
                    if (neighbor != null && pathfinding.IsEdgeBlocked(node, neighbor))
                    {
                        return true;
                    }
                }
                
                foreach (Node neighbor in node.neighbors)
                {
                    if (!visited.Contains(neighbor))
                        toCheck.Enqueue(neighbor);
                }
            }
        }
        
        return false;
    }

    List<Node> GetSafeNeighbors(Node node)
    {
        List<Node> safeNeighbors = new List<Node>();
        
        if (node == null || pathfinding == null) return safeNeighbors;
        
        foreach (Node neighbor in node.neighbors)
        {
            if (neighbor == null) continue;
            if (neighbor == previousNode) continue;
            if (nodeHistory.Contains(neighbor)) continue;
            
            // Cek apakah edge ke neighbor ini aman
            if (!pathfinding.IsEdgeBlocked(node, neighbor))
            {
                // Cek 1 node ke depan dari neighbor
                bool hasBlockedAhead = false;
                foreach (Node nextNode in neighbor.neighbors)
                {
                    if (nextNode != null && pathfinding.IsEdgeBlocked(neighbor, nextNode))
                    {
                        hasBlockedAhead = true;
                        break;
                    }
                }
                
                if (!hasBlockedAhead)
                    safeNeighbors.Add(neighbor);
            }
        }
        
        return safeNeighbors;
    }

    Node FindSafeFarNode()
    {
        if (pathfinding == null || pathfinding.allNodes.Count == 0) return null;

        Node bestNode = null;
        float bestDist = -1f;

        foreach (Node node in pathfinding.allNodes)
        {
            if (node == null || node == currentNode) continue;
            
            // Cek apakah ada jalur aman ke node ini
            bool pathSafe = true;
            List<Node> path = GetSimplePath(currentNode, node);
            
            if (path != null && path.Count > 1)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    if (pathfinding.IsEdgeBlocked(path[i], path[i + 1]))
                    {
                        pathSafe = false;
                        break;
                    }
                }
            }

            if (pathSafe)
            {
                float dist = Vector3.Distance(transform.position, node.transform.position);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    bestNode = node;
                }
            }
        }

        return bestNode;
    }

    List<Node> GetSimplePath(Node start, Node target)
    {
        if (start == null || target == null) return new List<Node>();

        Queue<Node> queue = new Queue<Node>();
        Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();
        HashSet<Node> visited = new HashSet<Node>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Node current = queue.Dequeue();
            
            if (current == target)
            {
                List<Node> path = new List<Node>();
                Node node = target;
                while (node != start)
                {
                    path.Add(node);
                    if (!cameFrom.ContainsKey(node)) break;
                    node = cameFrom[node];
                }
                path.Add(start);
                path.Reverse();
                return path;
            }

            foreach (Node neighbor in current.neighbors)
            {
                if (neighbor != null && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return new List<Node>();
    }

    // =============================================
    // ORIGINAL METHODS (dengan modifikasi)
    // =============================================

    int CountNPCAtNode(Node node)
    {
        if (node == null) return 0;
        
        Collider[] hits = Physics.OverlapSphere(node.transform.position, nodeCheckRadius);
        int count = 0;
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("NPC"))
                count++;
        }
        return count;
    }

    bool IsPathCongested(Node startNode, out int totalNPC)
    {
        totalNPC = 0;
        if (startNode == null) return false;
        
        HashSet<Node> visited = new HashSet<Node>();
        Queue<Node> toCheck = new Queue<Node>();
        toCheck.Enqueue(startNode);
        
        for (int depth = 0; depth < lookAheadNodes && toCheck.Count > 0; depth++)
        {
            int levelSize = toCheck.Count;
            for (int i = 0; i < levelSize; i++)
            {
                Node node = toCheck.Dequeue();
                if (visited.Contains(node)) continue;
                visited.Add(node);
                
                totalNPC += CountNPCAtNode(node);
                
                foreach (Node neighbor in node.neighbors)
                {
                    if (!visited.Contains(neighbor))
                        toCheck.Enqueue(neighbor);
                }
            }
        }
        
        return totalNPC >= maxNPCBeforeAvoid;
    }

    void UpdateRightTurnCount()
    {
        if (previousNode == null || currentNode == null) return;
        if (targetNode == null) return;
        
        Vector3 oldDir = (currentNode.transform.position - previousNode.transform.position).normalized;
        Vector3 newDir = (targetNode.transform.position - currentNode.transform.position).normalized;
        oldDir.y = 0f;
        newDir.y = 0f;
        
        float cross = Vector3.Cross(oldDir, newDir).y;
        
        if (cross < 0)
        {
            rightTurnCount++;
        }
        else
        {
            if (rightTurnCount > 0)
                rightTurnCount = 0;
        }
    }

    void FindNearestNode()
    {
        if (pathfinding == null || pathfinding.allNodes.Count == 0) return;

        Node nearest = null;
        float nearestDist = Mathf.Infinity;
        foreach (Node node in pathfinding.allNodes)
        {
            float dist = Vector3.Distance(transform.position, node.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = node;
            }
        }

        if (nearest != null)
        {
            currentNode = nearest;
            previousNode = null;
            nodeHistory.Clear();
            rightTurnCount = 0;
            ChooseFarthestNode();
        }
    }

    void ChooseFarthestNode()
    {
        if (currentNode == null || currentNode.neighbors.Count == 0)
        {
            FindNearestNode();
            return;
        }

        // Cari node terjauh yang aman
        Node farthestNode = null;
        float farthestDist = -1f;

        foreach (Node n in currentNode.neighbors)
        {
            // Skip jika edge terblokir
            if (pathfinding != null && pathfinding.IsEdgeBlocked(currentNode, n))
                continue;
                
            float dist = Vector3.Distance(transform.position, n.transform.position);
            if (dist > farthestDist)
            {
                farthestDist = dist;
                farthestNode = n;
            }
        }

        if (farthestNode != null)
            targetNode = farthestNode;
        else
        {
            // Jika semua terblokir, cari node jauh yang aman
            Node safeNode = FindSafeFarNode();
            if (safeNode != null)
                targetNode = safeNode;
            else
                targetNode = currentNode.neighbors[0];
        }
    }

    void ChooseNextNode()
    {
        if (currentNode == null || currentNode.neighbors.Count == 0)
        {
            FindNearestNode();
            return;
        }

        // =============================================
        // CEK OBSTACLE DI DEPAN MENGGUNAKAN PATHFINDING
        // =============================================
        
        if (pathfinding != null && IsPathHasObstacleAhead(currentNode))
        {
            // Cari neighbor yang aman
            List<Node> safeNeighbors = GetSafeNeighbors(currentNode);
            
            if (safeNeighbors.Count > 0)
            {
                // Pilih yang paling searah dengan arah NPC
                Node bestNode = safeNeighbors[0];
                float bestDot = -1f;
                
                foreach (Node node in safeNeighbors)
                {
                    Vector3 toNode = (node.transform.position - transform.position).normalized;
                    float dot = Vector3.Dot(transform.forward, toNode);
                    
                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        bestNode = node;
                    }
                }
                
                targetNode = bestNode;
                return;
            }
            else
            {
                // Jika tidak ada neighbor aman, cari node jauh yang aman
                Node safeNode = FindSafeFarNode();
                if (safeNode != null)
                {
                    targetNode = safeNode;
                    return;
                }
            }
        }

        // =============================================
        // ORIGINAL LOGIC (jika tidak ada obstacle)
        // =============================================

        List<Node> candidates = new List<Node>();
        
        Vector3 datangArah = Vector3.zero;
        if (previousNode != null && currentNode != null)
        {
            datangArah = (currentNode.transform.position - previousNode.transform.position).normalized;
            datangArah.y = 0f;
        }
        
        foreach (Node n in currentNode.neighbors)
        {
            if (nodeHistory.Contains(n)) continue;
            if (previousNode != null && n == previousNode) continue;
            
            // Skip jika edge terblokir
            if (pathfinding != null && pathfinding.IsEdgeBlocked(currentNode, n))
                continue;
            
            if (previousNode != null && datangArah != Vector3.zero)
            {
                Vector3 arahKeNode = (n.transform.position - currentNode.transform.position).normalized;
                arahKeNode.y = 0f;
                float dot = Vector3.Dot(datangArah, arahKeNode);
                if (dot < -0.5f) continue;
            }
            
            candidates.Add(n);
        }

        if (candidates.Count == 0)
        {
            foreach (Node n in currentNode.neighbors)
            {
                if (previousNode != null && n == previousNode) continue;
                if (pathfinding != null && pathfinding.IsEdgeBlocked(currentNode, n))
                    continue;
                candidates.Add(n);
            }
        }

        if (candidates.Count == 0)
        {
            // Jika semua terblokir, cari node jauh yang aman
            Node safeNode = FindSafeFarNode();
            if (safeNode != null)
            {
                targetNode = safeNode;
                return;
            }
            candidates = new List<Node>(currentNode.neighbors);
        }

        if (candidates.Count == 1)
        {
            targetNode = candidates[0];
            return;
        }

        Dictionary<Node, float> congestionScore = new Dictionary<Node, float>();
        
        foreach (Node candidate in candidates)
        {
            int npcCount;
            bool isCongested = IsPathCongested(candidate, out npcCount);
            
            float score = isCongested ? -100f : 100f - (npcCount * 10f);
            congestionScore[candidate] = score;
        }

        Vector3 currentForward = transform.forward;
        currentForward.y = 0f;
        if (currentForward == Vector3.zero) currentForward = Vector3.forward;

        List<Node> straightNodes = new List<Node>();
        List<Node> rightNodes = new List<Node>();
        List<Node> leftNodes = new List<Node>();

        foreach (Node node in candidates)
        {
            Vector3 toNode = (node.transform.position - transform.position).normalized;
            toNode.y = 0f;
            float dot = Vector3.Dot(currentForward, toNode);
            
            if (dot > 0.7f)
                straightNodes.Add(node);
            else
            {
                Vector3 cross = Vector3.Cross(currentForward, toNode);
                if (cross.y > 0)
                    leftNodes.Add(node);
                else
                    rightNodes.Add(node);
            }
        }

        if (rightTurnCount >= maxRightTurnInRow)
            rightNodes.Clear();

        List<Node> selectedDirection = null;
        
        List<Node> bestNodes = candidates.OrderByDescending(n => congestionScore[n]).ToList();
        
        if (bestNodes.Count > 0 && congestionScore[bestNodes[0]] > -50)
        {
            targetNode = bestNodes[0];
            return;
        }
        
        float randomRoll = Random.value;
        float actualStraightChance = chanceToGoStraight;
        float actualLeftChance = chanceToTurnLeft;
        float actualRightChance = (rightNodes.Count > 0) ? chanceToTurnRight : 0;
        float actualTotal = actualStraightChance + actualLeftChance + actualRightChance;
        
        if (actualTotal < 0.01f) actualTotal = 1f;
        
        float straightChanceNorm = actualStraightChance / actualTotal;
        float rightChanceNorm = actualRightChance / actualTotal;
        
        if (randomRoll < straightChanceNorm && straightNodes.Count > 0)
            selectedDirection = straightNodes;
        else if (randomRoll < straightChanceNorm + rightChanceNorm && rightNodes.Count > 0)
            selectedDirection = rightNodes;
        else if (leftNodes.Count > 0)
            selectedDirection = leftNodes;
        else if (straightNodes.Count > 0)
            selectedDirection = straightNodes;
        else if (rightNodes.Count > 0)
            selectedDirection = rightNodes;
        else
            selectedDirection = candidates;

        if (selectedDirection.Count == 1)
        {
            targetNode = selectedDirection[0];
        }
        else
        {
            Dictionary<Node, float> distances = new Dictionary<Node, float>();
            foreach (Node node in selectedDirection)
            {
                float dist = Vector3.Distance(transform.position, node.transform.position);
                distances[node] = dist;
            }
            
            var sortedByDistance = distances.OrderBy(x => x.Value).ToList();
            
            if (Random.value < preferLongPath)
                targetNode = sortedByDistance[sortedByDistance.Count - 1].Key;
            else
                targetNode = sortedByDistance[0].Key;
        }
    }

    public Vector3 GetDirection()
    {
        if (targetNode == null) return transform.forward;
        Vector3 dir = (targetNode.transform.position - transform.position).normalized;
        dir.y = 0f;
        return dir;
    }

    void OnDrawGizmosSelected()
    {
        if (targetNode != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetNode.transform.position);
            Gizmos.DrawWireSphere(targetNode.transform.position, 0.5f);
        }
        if (previousNode != null)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(previousNode.transform.position, 0.5f);
        }
        
        Gizmos.color = Color.magenta;
        foreach (Node h in nodeHistory)
        {
            if (h != null)
                Gizmos.DrawWireSphere(h.transform.position, 0.7f);
        }
        
        if (rightTurnCount >= maxRightTurnInRow)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
        }

        // ========== GIZMOS OBSTACLE ==========
        if (currentNode != null && pathfinding != null)
        {
            HashSet<Node> visited = new HashSet<Node>();
            Queue<Node> toCheck = new Queue<Node>();
            toCheck.Enqueue(currentNode);
            
            for (int depth = 0; depth < obstacleLookAhead && toCheck.Count > 0; depth++)
            {
                int levelSize = toCheck.Count;
                for (int i = 0; i < levelSize; i++)
                {
                    Node node = toCheck.Dequeue();
                    if (visited.Contains(node)) continue;
                    visited.Add(node);
                    
                    bool hasBlockedEdge = false;
                    foreach (Node neighbor in node.neighbors)
                    {
                        if (neighbor != null && pathfinding.IsEdgeBlocked(node, neighbor))
                        {
                            hasBlockedEdge = true;
                            break;
                        }
                    }
                    
                    if (hasBlockedEdge && depth > 0)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(node.transform.position, 1f);
                    }
                    else if (depth > 0)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(node.transform.position, 0.3f);
                    }
                    
                    foreach (Node neighbor in node.neighbors)
                    {
                        if (!visited.Contains(neighbor))
                            toCheck.Enqueue(neighbor);
                    }
                }
            }
        }
    }
}