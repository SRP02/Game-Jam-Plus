using System.Collections.Generic;
using UnityEngine;
using Race.Utility.PoolingSystem;

public class Ground : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform ground;
    [Tooltip("The tree prefab to spawn.")]
    [SerializeField] private GameObject treePrefab;

    [Header("Spawn volume (X ranges)")]
    [SerializeField] private Vector2 leftXRange = new Vector2(-10f, -4f);
    [SerializeField] private Vector2 rightXRange = new Vector2(4f, 10f);

    [Header("Density Settings")] 
    [SerializeField] private float spawnAheadDistance = 100f; 
    
    [Tooltip("Vertical distance between rows.")]
    [SerializeField] private float spawnYStep = 1.0f; // Keep around 1.0 to 2.0

    [Tooltip("How many trees to try and spawn per row on EACH side. Increase this for forests.")]
    [SerializeField] private int treesPerSidePerRow = 2; // NEW: Spawns multiple trees at same Y height

    [Tooltip("Distance BEHIND the player to remove trees.")]
    [SerializeField] private float despawnDistance = 20f; // Reduced: We only care about what is behind us
    
    [Tooltip("Hard limit on objects.")]
    [SerializeField] private int maxActiveTrees = 1000; 

    [Tooltip("An empty GameObject at (0,0,0) to hold the trees so they don't move with the ground.")]
    [SerializeField] private Transform treeContainer;

    [Header("Misc")]
    [Range(0f, 1f)]
    [Tooltip("Chance for a tree to appear.")]
    [SerializeField] private float spawnChance = 0.85f; 

    [Tooltip("If true, uses the PoolingManager. If false, uses Instantiate/Destroy.")]
    [SerializeField] private bool usePooling = true;

    // Internal tracking
    private readonly List<GameObject> activeTrees = new List<GameObject>();
    private float nextSpawnY;

    private void Start()
    {
        if (player == null || treePrefab == null)
        {
            Debug.LogError("Ground: Player or TreePrefab not assigned!");
            enabled = false;
            return;
        }

        nextSpawnY = player.position.y;

        if (usePooling && PoolingManager.Instance != null)
        {
            // FIX: Initialize pool with the MAX amount, not just 100.
            PoolingManager.Instance.CreatePool(treePrefab, maxActiveTrees); 
        }
    }

    private void Update()
    {
        if (player == null) return;

        transform.rotation = Quaternion.identity;
        if (ground != null)
        {
            ground.position = new Vector3(player.position.x, player.position.y, 10f);
        }

        HandleSpawning();
        HandleDespawning();
    }

    private void HandleSpawning()
    {
        float spawnLimitY = player.position.y + spawnAheadDistance;
        if (spawnYStep <= 0.1f) spawnYStep = 0.1f; 

        while (nextSpawnY < spawnLimitY && activeTrees.Count < maxActiveTrees)
        {
            // 1. Loop for Density (Spawn multiple trees per Y step)
            for (int i = 0; i < treesPerSidePerRow; i++)
            {
                // Try Left Side
                if (Random.value <= spawnChance)
                {
                    float x = Random.Range(leftXRange.x, leftXRange.y);
                    // Add slight Y variation so they aren't in a perfect straight line
                    float yVariation = Random.Range(0, spawnYStep / 2); 
                    SpawnAndRegister(new Vector3(x, nextSpawnY + yVariation, 0f));
                }

                // Try Right Side
                if (Random.value <= spawnChance)
                {
                    float x = Random.Range(rightXRange.x, rightXRange.y);
                    float yVariation = Random.Range(0, spawnYStep / 2);
                    SpawnAndRegister(new Vector3(x, nextSpawnY + yVariation, 0f));
                }
            }

            nextSpawnY += spawnYStep;
        }
    }

    private void SpawnAndRegister(Vector3 pos)
    {
        if (activeTrees.Count >= maxActiveTrees) return;

        GameObject t = SpawnTree(pos);
        if (t != null)
        {
            activeTrees.Add(t);
        }
    }

    private void HandleDespawning()
    {
        // Loop backwards to safely remove items
        for (int i = activeTrees.Count - 1; i >= 0; i--)
        {
            var t = activeTrees[i];

            if (t == null)
            {
                activeTrees.RemoveAt(i);
                continue;
            }

            // FIX: Only check if the tree is BEHIND the player (y < player.y)
            // The previous code deleted trees that were too far AHEAD as well.
            if (t.transform.position.y < (player.position.y - despawnDistance))
            {
                DespawnTree(t);
                activeTrees.RemoveAt(i);
            }
        }
    }

    private GameObject SpawnTree(Vector3 worldPos)
    {
        Transform parent = treeContainer != null ? treeContainer : null;

        if (usePooling && PoolingManager.Instance != null)
        {
            return PoolingManager.Instance.Spawn(treePrefab, worldPos, Quaternion.identity, parent, true);
        }
        else
        {
            return Instantiate(treePrefab, worldPos, Quaternion.identity, parent);
        }
    }

    private void DespawnTree(GameObject t)
    {
        if (usePooling && PoolingManager.Instance != null)
        {
            PoolingManager.Instance.Despawn(t);
        }
        else
        {
            Destroy(t);
        }
    }

    private void OnDisable()
    {
        if (usePooling && PoolingManager.Instance != null)
        {
            foreach (var tree in activeTrees)
            {
                if (tree != null) PoolingManager.Instance.Despawn(tree);
            }
        }
        activeTrees.Clear();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.green;
        // Visualize Spawn Area
        Vector3 spawnStart = new Vector3(0, player.position.y, 0);
        Vector3 spawnEnd = new Vector3(0, player.position.y + spawnAheadDistance, 0);
        Gizmos.DrawLine(spawnStart, spawnEnd);

        Gizmos.color = Color.red;
        // Visualize Despawn Line
        Vector3 despawnLine = new Vector3(0, player.position.y - despawnDistance, 0);
        Gizmos.DrawSphere(despawnLine, 0.5f);
    }
#endif
}