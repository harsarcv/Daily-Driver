using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class NPCType
{
    public GameObject prefab;
    [Range(0f, 1f)]
    public float probability = 0.33f;
    public string typeName = "Mobil";
}

public class NPCSpawnerDespawner : MonoBehaviour
{
    [Header("Prefab NPC (Dengan Presentase)")]
    public List<NPCType> npcTypes = new List<NPCType>();

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Settings")]
    public int initialSpawnCount = 20;
    public float spawnDelay = 0.1f;

    [Header("Deteksi")]
    public float minDistanceToNPC = 5f;  // Jarak minimal dengan NPC lain

    [Header("Reference")]
    public Pathfinding pathfinding;

    private List<GameObject> activeNPCs = new List<GameObject>();
    private Transform npcParent;

    void Start()
    {
        if (pathfinding == null)
            pathfinding = FindFirstObjectByType<Pathfinding>();
        
        ValidateProbabilities();
        
        npcParent = new GameObject("All_NPCs").transform;
        npcParent.SetParent(transform);
        
        StartCoroutine(SpawnInitialNPCsCoroutine());
    }

    void ValidateProbabilities()
    {
        float total = 0f;
        foreach (NPCType type in npcTypes)
        {
            total += type.probability;
        }
        
        if (Mathf.Abs(total - 1f) > 0.01f)
        {
            foreach (NPCType type in npcTypes)
            {
                type.probability /= total;
            }
        }
    }

    GameObject GetRandomPrefab()
    {
        if (npcTypes.Count == 0) return null;
        
        float randomValue = Random.value;
        float cumulative = 0f;
        
        foreach (NPCType type in npcTypes)
        {
            cumulative += type.probability;
            if (randomValue <= cumulative)
            {
                return type.prefab;
            }
        }
        
        return npcTypes[0].prefab;
    }

    // ========== CEK APAKAH SPAWN POINT AMAN ==========
    bool IsSpawnPointSafe(Transform spawnPoint)
    {
        if (spawnPoint == null) return false;
        
        foreach (GameObject npc in activeNPCs)
        {
            if (npc == null) continue;
            
            float distance = Vector3.Distance(npc.transform.position, spawnPoint.position);
            if (distance < minDistanceToNPC)
            {
                return false; // Ada NPC terlalu dekat
            }
        }
        
        return true; // Aman
    }

    // ========== DAPATKAN SPAWN POINT YANG AMAN ==========
    Transform GetSafeSpawnPoint()
    {
        List<Transform> safePoints = new List<Transform>();
        
        foreach (Transform sp in spawnPoints)
        {
            if (sp == null) continue;
            if (IsSpawnPointSafe(sp))
            {
                safePoints.Add(sp);
            }
        }
        
        if (safePoints.Count > 0)
        {
            return safePoints[Random.Range(0, safePoints.Count)];
        }
        
        return null; // Tidak ada spawn point yang aman
    }

    IEnumerator SpawnInitialNPCsCoroutine()
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("Tidak ada Spawn Point!");
            yield break;
        }

        if (npcTypes.Count == 0)
        {
            Debug.LogError("Tidak ada Prefab NPC!");
            yield break;
        }

        int spawned = 0;
        int maxAttempts = initialSpawnCount * 3;

        for (int attempt = 0; attempt < maxAttempts && spawned < initialSpawnCount; attempt++)
        {
            Transform safePoint = GetSafeSpawnPoint();
            
            if (safePoint == null)
            {
                // Tidak ada spawn point aman, tunggu sebentar
                yield return new WaitForSeconds(1f);
                continue;
            }

            SpawnAtPoint(safePoint);
            spawned++;
            yield return new WaitForSeconds(spawnDelay);
        }

        Debug.Log($"Selesai spawn {spawned} NPC!");
    }

    void SpawnAtPoint(Transform spawnPoint)
    {
        if (spawnPoint == null) return;
        
        GameObject selectedPrefab = GetRandomPrefab();
        if (selectedPrefab == null) return;
        
        // Raycast ke tanah biar nempel
        Vector3 spawnPos = spawnPoint.position;
        RaycastHit hit;
        if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out hit, 10f))
        {
            spawnPos.y = hit.point.y;
        }
        else
        {
            spawnPos.y += 0.1f;
        }

        GameObject newNPC = Instantiate(selectedPrefab, spawnPos, spawnPoint.rotation);
        newNPC.transform.SetParent(npcParent);
        
        NPCNodeMovement movement = newNPC.GetComponent<NPCNodeMovement>();
        if (movement != null && movement.pathfinding == null)
            movement.pathfinding = pathfinding;
        
        activeNPCs.Add(newNPC);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (spawnPoints != null)
        {
            foreach (Transform sp in spawnPoints)
            {
                if (sp != null)
                {
                    Gizmos.DrawWireSphere(sp.position, 1f);
                    Gizmos.DrawRay(sp.position, sp.forward * 2f);
                    
                    // Gambar radius deteksi
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(sp.position, minDistanceToNPC);
                    Gizmos.color = Color.green;
                }
            }
        }
    }
}