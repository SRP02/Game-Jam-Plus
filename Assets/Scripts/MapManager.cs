using UnityEngine;
using System.Collections.Generic;
using Race.Utility.PoolingSystem; // for PoolingManager

[RequireComponent(typeof(Transform))]
public class MapManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Road Sprite")]
    [SerializeField] private Color roadColor = Color.white;
    [SerializeField] private Sprite roadSprite;
    [SerializeField] private float roadWorldWidth = 4f;
    [SerializeField] private bool tileTextureAlongLength = true;
    [SerializeField] private float textureRepeatPerUnit = 0.5f;
    [Tooltip("Optional: assign a material (use a Sprite-Lit material to get lighting).")]
    [SerializeField] private Material roadMaterial;

    [Header("Road Settings")]
    [SerializeField] private float segmentLength = 40f;
    [SerializeField] private int controlPoints = 4;
    [SerializeField] private int smoothPointsPerSegment = 25;
    [SerializeField] private float curveStrength = 5f;
    [SerializeField] private int keepSegments = 6;
    [Header("Initial Offset")]
    [SerializeField] private float initialYOffset = -10f;

    [Header("Auto-layering")]
    [Tooltip("Name of layer to assign to each generated road GameObject. Create this layer in the editor (e.g. 'Road').")]
    [SerializeField] private string roadLayerName = "Road";

    [Header("Collider Settings")]
    [SerializeField] private float colliderSidePadding = 0.25f;

    [Header("Pooling (optional)")]
    [Tooltip("Assign a prefab to enable pooling. Prefab isn't required — pooling will be skipped if null.")]
    [SerializeField] private GameObject roadSegmentPrefab;
    [SerializeField] private bool usePooling = true;

    private readonly List<RoadSegment> segments = new();
    private float lastY;
    private float noiseSeed;

    // --- FIXED VARIABLES ---
    // Stores the second-to-last control point of the previous segment (P0 for the next spline)
    private Vector3 prevSegmentSecondToLastControlPoint;
    // Stores the last sample point for perfect vertex snapping between meshes
    private Vector3 prevSegmentLastSample;
    private bool hasPrevSegment = false;
    // ---------------------

    private class RoadSegment
    {
        public GameObject go;
        public float startY;
        public float endY;
    }

    private void Start()
    {
        if (roadSprite == null)
        {
            Debug.LogError("MapManager: roadSprite not assigned!");
            enabled = false;
            return;
        }

        if (roadWorldWidth <= 0f)
        {
            roadWorldWidth = roadSprite.rect.width / roadSprite.pixelsPerUnit;
        }

        if (player == null)
        {
            Debug.LogError("MapManager: player reference not assigned!");
            enabled = false;
            return;
        }

        lastY = player.position.y + initialYOffset;
        noiseSeed = Random.Range(0f, 1000f);

        for (int i = 0; i < 3; i++)
            GenerateSegment();
    }

    private void Update()
    {
        if (player == null) return;

        float playerY = player.position.y;

        if (playerY + segmentLength * 1.5f > lastY)
            GenerateSegment();

        if (segments.Count > keepSegments)
        {
            var seg = segments[0];
            if (playerY > seg.endY + 20f)
            {
                if (usePooling && roadSegmentPrefab != null && PoolingManager.Instance != null)
                {
                    PoolingManager.Instance.Despawn(seg.go);
                }
                else
                {
                    Destroy(seg.go);
                }

                segments.RemoveAt(0);
            }
        }
    }

    private void GenerateSegment()
    {
        float startY = lastY;
        float endY = startY + segmentLength;

        Vector3[] control = new Vector3[controlPoints];
        for (int i = 0; i < controlPoints; i++)
        {
            float t = i / (float)(controlPoints - 1);
            float y = Mathf.Lerp(startY, endY, t);
            float noise = Mathf.PerlinNoise(noiseSeed, y * 0.1f) - 0.5f;
            float x = noise * curveStrength;
            control[i] = new Vector3(x, y, 0f);
        }

        List<Vector3> samples = new List<Vector3>();
        for (int i = 0; i < controlPoints - 1; i++)
        {
            Vector3 p0;
            // FIX: Use the stored Control Point for Catmull-Rom continuity
            if (i == 0)
            {
                if (hasPrevSegment)
                {
                    p0 = prevSegmentSecondToLastControlPoint;
                }
                else
                {
                    p0 = control[0] - (control[1] - control[0]);
                }
            }
            else
                p0 = control[i - 1];

            Vector3 p1 = control[i];
            Vector3 p2 = control[i + 1];
            Vector3 p3 = (i + 2 < controlPoints) ? control[i + 2] : control[i + 1] + (control[i + 1] - control[i]);

            for (int s = 0; s < smoothPointsPerSegment; s++)
            {
                float tt = s / (float)(smoothPointsPerSegment - 1);
                Vector3 pt = CatmullRom(p0, p1, p2, p3, tt);

                if (s == smoothPointsPerSegment - 1 && i < controlPoints - 2)
                    continue;

                samples.Add(pt);
            }
        }

        // Snap the first vertex to the exact end of the previous mesh to avoid pixel gaps
        if (hasPrevSegment && samples.Count > 0)
            samples[0] = prevSegmentLastSample;

        // --- UPDATE STATE FOR NEXT SEGMENT ---
        if (controlPoints >= 2)
        {
            // Store the second-to-last control point for the next segment's P0
            prevSegmentSecondToLastControlPoint = control[controlPoints - 2];
        }

        if (samples.Count >= 2)
        {
            prevSegmentLastSample = samples[samples.Count - 1];
            hasPrevSegment = true;
        }
        // -------------------------------------

        GameObject go = null;
        bool spawnedFromPool = false;

        if (usePooling && roadSegmentPrefab != null && PoolingManager.Instance != null)
        {
            go = PoolingManager.Instance.Spawn(roadSegmentPrefab, Vector3.zero, Quaternion.identity, transform, true);
            spawnedFromPool = true;
        }
        else
        {
            go = new GameObject("RoadSegment");
            go.transform.parent = transform;
            go.transform.localPosition = Vector3.zero;
        }

        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null) mf = go.AddComponent<MeshFilter>();

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr == null) mr = go.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.name = "RoadMesh";
        int n = samples.Count;

        if (n < 2)
        {
            if (spawnedFromPool && PoolingManager.Instance != null)
                PoolingManager.Instance.Despawn(go);
            else if (!spawnedFromPool)
                Destroy(go);

            return;
        }

        Vector3[] verts = new Vector3[n * 2];
        Vector2[] uvs = new Vector2[n * 2];
        int[] tris = new int[(n - 1) * 6];

        float halfWidth = roadWorldWidth * 0.5f;

        float[] cumLen = new float[n];
        for (int i = 1; i < n; i++)
            cumLen[i] = cumLen[i - 1] + Vector3.Distance(samples[i], samples[i - 1]);

        float totalLen = Mathf.Max(1f, cumLen[n - 1]);

        // FIX 2: TEXTURE WRAPPING (UV Inset)
        // Pull the UVs in slightly from the edge (0.0 and 1.0) to prevent texture bleeding.
        float uvInset = 0.01f;

        for (int i = 0; i < n; i++)
        {
            Vector3 p = samples[i];
            Vector3 tangent;

            // CORRECTED TANGENT LOGIC (using proper if/else structure)
            if (i == 0)
            {
                // Start of segment: Forward difference
                tangent = (samples[i + 1] - samples[i]).normalized;
            }
            else if (i == n - 1)
            {
                // End of segment: Backward difference
                tangent = (samples[i] - samples[i - 1]).normalized;
            }
            else
            {
                // Middle of segment: Central difference
                tangent = (samples[i + 1] - samples[i - 1]).normalized;
            }
            // END CORRECTED TANGENT LOGIC

            Vector3 normal = new Vector3(-tangent.y, tangent.x, 0f).normalized;

            verts[i * 2 + 0] = p + normal * halfWidth;
            verts[i * 2 + 1] = p - normal * halfWidth;

            float v = tileTextureAlongLength ? cumLen[i] * textureRepeatPerUnit : cumLen[i] / totalLen;

            // Apply the UV Inset here
            uvs[i * 2 + 0] = new Vector2(uvInset, v);
            uvs[i * 2 + 1] = new Vector2(1f - uvInset, v);
        }

        int ti = 0;
        for (int i = 0; i < n - 1; i++)
        {
            int idx = i * 2;
            tris[ti++] = idx;
            tris[ti++] = idx + 2;
            tris[ti++] = idx + 1;
            tris[ti++] = idx + 1;
            tris[ti++] = idx + 2;
            tris[ti++] = idx + 3;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.mesh = mesh;

        Material matToUse = roadMaterial != null
            ? new Material(roadMaterial)
            : new Material(Shader.Find("Sprites/Default"));

        if (roadSprite != null)
        {
            matToUse.mainTexture = roadSprite.texture;
            Rect texRect = roadSprite.textureRect;
            Vector2 texSize = new Vector2(roadSprite.texture.width, roadSprite.texture.height);
            Vector2 uvOffset = new Vector2(texRect.x / texSize.x, texRect.y / texSize.y);
            Vector2 uvScale = new Vector2(texRect.width / texSize.x, texRect.height / texSize.y);

            matToUse.SetTexture("_MainTex", roadSprite.texture);
            matToUse.SetVector("_MainTex_ST", new Vector4(uvScale.x, uvScale.y, uvOffset.x, uvOffset.y));
        }
        matToUse.color = roadColor;

        mr.sharedMaterial = matToUse;

        int layer = LayerMask.NameToLayer(roadLayerName);
        if (layer != -1)
            go.layer = layer;

        PolygonCollider2D poly = go.GetComponent<PolygonCollider2D>();
        if (poly == null) poly = go.AddComponent<PolygonCollider2D>();
        Vector2[] colliderPoints = new Vector2[n * 2];

        for (int i = 0; i < n; i++)
        {
            Vector3 left = verts[i * 2 + 0];
            Vector3 right = verts[i * 2 + 1];

            Vector3 sideNormal = (left - right).normalized;

            left += sideNormal * colliderSidePadding;
            right -= sideNormal * colliderSidePadding;

            colliderPoints[i] = new Vector2(left.x, left.y);
            colliderPoints[n + (n - 1 - i)] = new Vector2(right.x, right.y);
        }

        poly.points = colliderPoints;

        if (spawnedFromPool)
        {
            go.transform.SetParent(transform, true);
            go.transform.localPosition = Vector3.zero;
        }
        else
        {
            go.name = "RoadSegment";
        }

        segments.Add(new RoadSegment { go = go, startY = startY, endY = endY });
        lastY = endY;
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
}