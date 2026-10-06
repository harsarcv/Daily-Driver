using UnityEngine;
using System.Collections.Generic;

public class GameRulerChecker : MonoBehaviour
{
    [Header("=== REFERENSI ===")]
    public Transform player;
    public List<Node> allNodes;
    public DeliveryManualAI gameManager;
    public CarController carController;
    
    [Header("=== LAMPU MERAH SETTINGS ===")]
    public LayerMask trafficLightLayer;
    public float detectionRadius = 4f;
    public float checkLampuInterval = 0.3f;
    
    [Header("=== NODE NEIGHBORS SETTINGS ===")]
    public float checkNodeInterval = 0.2f;
    public float nodeReachRadius = 15f;
    
    [Header("=== DEBUG ===")]
    public bool enableLog = false;
    
    // ===== INTERNAL =====
    private float timerLampu = 0f;
    private float timerNode = 0f;
    
    private bool isGameOver = false;
    private bool isInitialized = false;
    
    // ===== UNTUK NODE NEIGHBORS =====
    private List<Node> visitedNodes = new List<Node>();
    private Node lastVisitedNode = null;
    
    // ===== UNTUK LAMPU MERAH =====
    private Vector3 lastPlayerPosition;
    private bool hasStoppedAtRedLight = false;
    private TrafficLight currentRedLight = null;
    private float speedThreshold = 0.5f;
    
    void Start()
    {
        InitializeReferences();
        if (player != null)
            lastPlayerPosition = player.position;
    }
    
    void InitializeReferences()
    {
        if (player == null)
            player = GetComponent<Transform>();
        
        if (gameManager == null)
            gameManager = FindFirstObjectByType<DeliveryManualAI>();
        
        if (carController == null && player != null)
            carController = player.GetComponent<CarController>();
        
        if (player != null && allNodes != null && allNodes.Count > 0)
        {
            Node firstNode = GetNearestNode(player.position);
            if (firstNode != null)
            {
                lastVisitedNode = firstNode;
                visitedNodes.Add(firstNode);
            }
        }
        
        isInitialized = true;
    }
    
    void Update()
    {
        if (!isInitialized) return;
        if (gameManager != null && !gameManager.gameAktif) return;
        if (isGameOver) return;
        if (player == null) return;
        
        timerLampu += Time.unscaledDeltaTime;
        if (timerLampu >= checkLampuInterval)
        {
            timerLampu = 0f;
            CheckTrafficLight();
        }
        
        CheckPassingTrafficLight();
        
        if (allNodes != null && allNodes.Count > 0)
        {
            timerNode += Time.unscaledDeltaTime;
            if (timerNode >= checkNodeInterval)
            {
                timerNode = 0f;
                CheckNodeNeighbors();
            }
        }
        
        if (player != null)
            lastPlayerPosition = player.position;
    }
    
    // ============================================================
    // 1. CEK LAMPU MERAH - POSISI
    // ============================================================
    
    void CheckTrafficLight()
    {
        if (trafficLightLayer.value == 0) return;
        
        Collider[] hitColliders = Physics.OverlapSphere(player.position, detectionRadius, trafficLightLayer);
        
        bool foundRed = false;
        
        foreach (Collider col in hitColliders)
        {
            TrafficLight trafficLight = col.GetComponent<TrafficLight>();
            if (trafficLight != null)
            {
                if (IsPlayerFacingTrafficLight(trafficLight))
                {
                    if (trafficLight.IsRed())
                    {
                        foundRed = true;
                        if (!hasStoppedAtRedLight)
                        {
                            hasStoppedAtRedLight = true;
                            currentRedLight = trafficLight;
                        }
                    }
                    else if (trafficLight.IsGreen())
                    {
                        if (currentRedLight == trafficLight)
                        {
                            ResetLightStatus();
                        }
                    }
                }
            }
        }
        
        if (!foundRed && hasStoppedAtRedLight && currentRedLight != null)
        {
            float distToLight = Vector3.Distance(player.position, currentRedLight.transform.position);
            if (distToLight > detectionRadius * 2f)
            {
                ResetLightStatus();
            }
        }
    }
    
    // ============================================================
    // 2. CEK LAMPU MERAH - MELEWATI (SETIAP FRAME)
    // ============================================================
    
    void CheckPassingTrafficLight()
    {
        if (trafficLightLayer.value == 0) return;
        if (player == null) return;
        if (lastPlayerPosition == Vector3.zero) return;
        
        float speed = Vector3.Distance(player.position, lastPlayerPosition) / Time.unscaledDeltaTime;
        if (speed < speedThreshold) return;
        
        float passingRadius = detectionRadius * 1.5f;
        Collider[] hitColliders = Physics.OverlapSphere(player.position, passingRadius, trafficLightLayer);
        
        foreach (Collider col in hitColliders)
        {
            TrafficLight trafficLight = col.GetComponent<TrafficLight>();
            if (trafficLight != null)
            {
                if (IsPlayerBehindLight(trafficLight))
                {
                    if (trafficLight.IsRed() && !hasStoppedAtRedLight)
                    {
                        TriggerGameOver("ANDA MELANGGAR LAMPU MERAH!");
                        return;
                    }
                    else if (trafficLight.IsRed() && hasStoppedAtRedLight && currentRedLight == trafficLight)
                    {
                        ResetLightStatus();
                    }
                    else if (trafficLight.IsGreen())
                    {
                        ResetLightStatus();
                    }
                }
            }
        }
    }
    
    bool IsPlayerFacingTrafficLight(TrafficLight trafficLight)
    {
        if (player == null || trafficLight == null) return false;
        
        Vector3 toLight = (trafficLight.transform.position - player.position).normalized;
        toLight.y = 0f;
        
        Vector3 playerForward = player.forward;
        playerForward.y = 0f;
        
        float dot = Vector3.Dot(playerForward, toLight);
        float distance = Vector3.Distance(player.position, trafficLight.transform.position);
        
        return dot > 0.3f && distance < detectionRadius;
    }
    
    bool IsPlayerBehindLight(TrafficLight trafficLight)
    {
        if (player == null || trafficLight == null) return false;
        
        Vector3 lightPos = trafficLight.transform.position;
        lightPos.y = 0f;
        
        Vector3 playerPos = player.position;
        playerPos.y = 0f;
        
        Vector3 fromLightToPlayer = (playerPos - lightPos).normalized;
        Vector3 lightForward = -trafficLight.transform.forward;
        lightForward.y = 0f;
        
        float dot = Vector3.Dot(fromLightToPlayer, lightForward);
        float distance = Vector3.Distance(playerPos, lightPos);
        
        return dot < -0.2f && distance < detectionRadius * 1.5f;
    }
    
    void ResetLightStatus()
    {
        hasStoppedAtRedLight = false;
        currentRedLight = null;
    }
    
    // ============================================================
    // 3. CEK NODE NEIGHBORS (DIPERBAIKI)
    // ============================================================
    
    void CheckNodeNeighbors()
    {
        Node nearestNode = GetNearestNode(player.position);
        if (nearestNode == null) return;
        
        float distanceToNode = Vector3.Distance(player.position, nearestNode.transform.position);
        
        if (distanceToNode <= nodeReachRadius)
        {
            // ===== PERBAIKAN: IZINKAN LEWAT NODE YANG SUDAH DIKUNJUNGI =====
            if (visitedNodes.Contains(nearestNode))
            {
                // Update last visited agar sistem tetap tahu posisi terakhir
                lastVisitedNode = nearestNode;
                return;
            }
            
            // Cek apakah node terhubung dengan node terakhir
            bool isNeighbor = IsNodeNeighbor(lastVisitedNode, nearestNode);
            
            if (!isNeighbor)
            {
                TriggerGameOver($"ANDA MELAWAN ARAH!");
                return;
            }
            
            // Node baru valid, tambahkan ke visited
            visitedNodes.Add(nearestNode);
            lastVisitedNode = nearestNode;
            
            if (enableLog)
                Debug.Log($"Node terhubung: {lastVisitedNode.name} -> {nearestNode.name}");
        }
    }
    
    bool IsNodeNeighbor(Node fromNode, Node toNode)
    {
        if (fromNode == null || toNode == null) return false;
        if (fromNode == toNode) return true;
        
        if (fromNode.neighbors != null)
        {
            foreach (Node neighbor in fromNode.neighbors)
            {
                if (neighbor == toNode)
                    return true;
            }
        }
        
        return false;
    }
    
    Node GetNearestNode(Vector3 position)
    {
        if (allNodes == null || allNodes.Count == 0)
            return null;
        
        Node nearest = null;
        float nearestDist = Mathf.Infinity;
        
        foreach (Node node in allNodes)
        {
            if (node == null) continue;
            float dist = Vector3.Distance(position, node.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = node;
            }
        }
        return nearest;
    }
    
    // ============================================================
    // GAME OVER
    // ============================================================
    
    void TriggerGameOver(string reason)
    {
        if (isGameOver) return;
        isGameOver = true;
        
        if (carController != null)
            carController.enabled = false;
        
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        if (gameManager != null)
        {
            gameManager.GameOver(reason);
        }
        else
        {
            Debug.LogError($"GAME OVER: {reason}");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
    
    // ============================================================
    // RESET
    // ============================================================
    
    public void ResetChecker()
    {
        isGameOver = false;
        visitedNodes.Clear();
        lastVisitedNode = null;
        timerLampu = 0f;
        timerNode = 0f;
        ResetLightStatus();
        
        if (player != null)
            lastPlayerPosition = player.position;
        
        if (player != null && allNodes != null && allNodes.Count > 0)
        {
            Node firstNode = GetNearestNode(player.position);
            if (firstNode != null)
            {
                lastVisitedNode = firstNode;
                visitedNodes.Add(firstNode);
            }
        }
    }
    
    // ============================================================
    // DEBUG VISUAL
    // ============================================================
    
    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, detectionRadius);
        
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(player.position, detectionRadius * 1.5f);
        
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(player.position, nodeReachRadius);
        
        Gizmos.color = new Color(0.8f, 0f, 0.8f, 0.3f);
        foreach (Node node in visitedNodes)
        {
            if (node != null)
            {
                Gizmos.DrawWireSphere(node.transform.position, 1f);
            }
        }
        
        if (lastVisitedNode != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(lastVisitedNode.transform.position, 1.5f);
            Gizmos.DrawLine(player.position, lastVisitedNode.transform.position);
        }
        
        if (currentRedLight != null)
        {
            Gizmos.color = hasStoppedAtRedLight ? Color.green : Color.red;
            Gizmos.DrawWireSphere(currentRedLight.transform.position, 2f);
            
            Gizmos.color = Color.blue;
            Vector3 lightForward = -currentRedLight.transform.forward;
            Gizmos.DrawRay(currentRedLight.transform.position, lightForward * 3f);
        }
    }
}