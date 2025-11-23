using Race.Utility.PoolingSystem;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform mapRoot;

    [Tooltip("List of possible obstacle prefabs. One is picked at random per spawn.")]
    public List<GameObject> obstaclePrefabs = new List<GameObject>();

    [Header("Spawn Settings")]
    public int minPerSegment = 1;
    public int maxPerSegment = 3;
    public float minSpacing = 4f;
    public float lateralPadding = 0.2f;
    public bool usePooling = true;

    [Tooltip("Optional layer index for spawned obstacles.")]
    public int obstacleLayer = -1;

    private HashSet<GameObject> processedSegments = new HashSet<GameObject>();

    private void Start()
    {
        if (mapRoot == null)
            mapRoot = transform;

        if (obstaclePrefabs.Count == 0)
            Debug.LogWarning("ObstacleSpawner: No obstacles assigned!");
    }

    private void Update()
    {
        if (mapRoot == null || obstaclePrefabs.Count == 0) return;

        for (int i = 0; i < mapRoot.childCount; i++)
        {
            GameObject child = mapRoot.GetChild(i).gameObject;

            if (processedSegments.Contains(child)) continue;
            if (!child.name.ToLower().Contains("roadsegment")) continue;

            TrySpawnOnSegment(child);
            processedSegments.Add(child);
        }

        CleanProcessedList();
    }

    private void CleanProcessedList()
    {
        List<GameObject> toRemove = null;

        foreach (var go in processedSegments)
        {
            if (go == null)
            {
                if (toRemove == null) toRemove = new List<GameObject>();
                toRemove.Add(go);
            }
        }

        if (toRemove != null)
            foreach (var r in toRemove) processedSegments.Remove(r);
    }

    private void TrySpawnOnSegment(GameObject segment)
    {
        MeshFilter mf = segment.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        Mesh mesh = mf.sharedMesh;
        Vector3[] verts = mesh.vertices;

        int pairCount = verts.Length / 2;
        if (pairCount < 2) return;

        int targetCount = Random.Range(minPerSegment, maxPerSegment + 1);
        List<Vector3> placed = new List<Vector3>();

        int attemptsLimit = targetCount * 6;
        int attempts = 0;
        int placedCount = 0;

        while (placedCount < targetCount && attempts < attemptsLimit)
        {
            attempts++;

            int idx = Random.Range(0, pairCount);

            Vector3 leftLocal = verts[idx * 2 + 0];
            Vector3 rightLocal = verts[idx * 2 + 1];
            Vector3 midLocal = (leftLocal + rightLocal) * 0.5f;

            float halfWidthLocal = Vector3.Distance(leftLocal, midLocal);
            float maxLateral = Mathf.Max(0.001f, halfWidthLocal - lateralPadding);

            float lateral = Random.Range(-maxLateral, maxLateral);

            Vector3 rightDirLocal = (rightLocal - leftLocal).normalized;
            Vector3 finalLocal = midLocal + rightDirLocal * lateral;

            Vector3 worldPos = segment.transform.TransformPoint(finalLocal);

            bool tooClose = false;
            foreach (var p in placed)
            {
                if (Vector3.Distance(worldPos, p) < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // rotation follows tangent
            Vector3 midNext = (idx + 1 < pairCount)
                ? (verts[(idx + 1) * 2] + verts[(idx + 1) * 2 + 1]) * 0.5f
                : midLocal;

            Vector3 midPrev = (idx - 1 >= 0)
                ? (verts[(idx - 1) * 2] + verts[(idx - 1) * 2 + 1]) * 0.5f
                : midLocal;

            Vector3 tangentLocal = (midNext - midPrev).normalized;
            Vector3 tangentWorld = segment.transform.TransformDirection(tangentLocal);

            Quaternion rot = Quaternion.LookRotation(Vector3.forward, tangentWorld);

            // PICK RANDOM OBSTACLE
            GameObject pickedPrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
            GameObject spawned = SpawnObstacle(pickedPrefab, worldPos, rot);

            if (spawned != null)
            {
                if (obstacleLayer >= 0)
                    spawned.layer = obstacleLayer;

                placed.Add(worldPos);
                placedCount++;
            }
        }
    }

    private GameObject SpawnObstacle(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (usePooling && PoolingManagerExists())
        {
            // pooling version
            try
            {
                return PoolingManager.Instance.Spawn(prefab, pos, rot, null, true);
            }
            catch
            {
                return Instantiate(prefab, pos, rot);
            }
        }

        // normal instantiate
        return Instantiate(prefab, pos, rot);
    }

    private bool PoolingManagerExists()
    {
        try { return PoolingManager.Instance != null; }
        catch { return false; }
    }
}
