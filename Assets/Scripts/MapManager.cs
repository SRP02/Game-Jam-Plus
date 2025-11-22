using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Transform))]
public class MapManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Road Sprite")]
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

    private readonly List<RoadSegment> segments = new();
    private float lastY;
    private float noiseSeed;

    // --- store previous segment samples so the spline continues smoothly ---
    private Vector3 prevSegmentSecondToLastSample;
    private Vector3 prevSegmentLastSample;
    private bool hasPrevSegment = false;
    // --- END ---

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

        // Apply initial Y offset so the first segment spawns higher/lower
        lastY = player.position.y + initialYOffset;

        noiseSeed = Random.Range(0f, 1000f);

        // pre-generate starting segments
        for (int i = 0; i < 3; i++)
            GenerateSegment();
    }


    private void Update()
    {
        if (player == null) return;

        float playerY = player.position.y;

        if (playerY + segmentLength * 1.5f > lastY)
            GenerateSegment();

        // cull old segments
        if (segments.Count > keepSegments)
        {
            var seg = segments[0];
            if (playerY > seg.endY + 20f)
            {
                Destroy(seg.go);
                segments.RemoveAt(0);
            }
        }
    }

    private void GenerateSegment()
    {
        float startY = lastY;
        float endY = startY + segmentLength;

        // 1. Build coarse control points
        Vector3[] control = new Vector3[controlPoints];
        for (int i = 0; i < controlPoints; i++)
        {
            float t = i / (float)(controlPoints - 1);
            float y = Mathf.Lerp(startY, endY, t);
            float noise = Mathf.PerlinNoise(noiseSeed, y * 0.1f) - 0.5f;
            float x = noise * curveStrength;
            control[i] = new Vector3(x, y, 0f);
        }

        // 2. Sample spline densely
        List<Vector3> samples = new List<Vector3>();
        for (int i = 0; i < controlPoints - 1; i++)
        {
            Vector3 p0;
            if (i == 0)
            {
                if (hasPrevSegment)
                {
                    // use previously stored sample so the new segment is smooth with the earlier segment
                    p0 = prevSegmentSecondToLastSample;
                }
                else
                {
                    p0 = control[0] - (control[1] - control[0]);
                }
            }
            else
            {
                p0 = control[i - 1];
            }

            Vector3 p1 = control[i];
            Vector3 p2 = control[i + 1];
            Vector3 p3 = (i + 2 < controlPoints) ? control[i + 2] : control[i + 1] + (control[i + 1] - control[i]);

            for (int s = 0; s < smoothPointsPerSegment; s++)
            {
                float tt = s / (float)(smoothPointsPerSegment - 1);
                Vector3 pt = CatmullRom(p0, p1, p2, p3, tt);

                // Avoid duplicating points at seams (except for very last point of segment)
                if (s == smoothPointsPerSegment - 1 && i < controlPoints - 2) continue;

                samples.Add(pt);
            }
        }

        // Ensure exact positional continuity with previous segment
        if (hasPrevSegment && samples.Count > 0)
            samples[0] = prevSegmentLastSample;

        // store samples for next segment continuity (grab last two samples from this segment)
        if (samples.Count >= 2)
        {
            prevSegmentSecondToLastSample = samples[Mathf.Max(0, samples.Count - 2)];
            prevSegmentLastSample = samples[samples.Count - 1];
            hasPrevSegment = true;
        }

        // 3. Build Mesh
        GameObject go = new GameObject("RoadSegment");
        go.transform.parent = transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.name = "RoadMesh";
        int n = samples.Count;

        if (n < 2) { Destroy(go); return; }

        Vector3[] verts = new Vector3[n * 2];
        Vector2[] uvs = new Vector2[n * 2];
        int[] tris = new int[(n - 1) * 6];
        float halfWidth = roadWorldWidth * 0.5f;

        float[] cumLen = new float[n];
        cumLen[0] = 0f;
        for (int i = 1; i < n; i++)
            cumLen[i] = cumLen[i - 1] + Vector3.Distance(samples[i], samples[i - 1]);

        float totalLen = cumLen[n - 1] > 0f ? cumLen[n - 1] : 1f;

        for (int i = 0; i < n; i++)
        {
            Vector3 p = samples[i];
            Vector3 tangent;

            if (i == 0)
            {
                if (hasPrevSegment)
                {
                    tangent = (samples[0] - prevSegmentSecondToLastSample).normalized;
                }
                else tangent = (samples[i + 1] - samples[i]).normalized;
            }
            else if (i == n - 1) tangent = (samples[i] - samples[i - 1]).normalized;
            else tangent = (samples[i + 1] - samples[i - 1]).normalized;

            Vector3 normal = new Vector3(-tangent.y, tangent.x, 0f).normalized;

            verts[i * 2 + 0] = p + normal * halfWidth;
            verts[i * 2 + 1] = p - normal * halfWidth;

            float v;
            if (tileTextureAlongLength)
                v = cumLen[i] * textureRepeatPerUnit;
            else
                v = cumLen[i] / totalLen;

            uvs[i * 2 + 0] = new Vector2(0f, v);
            uvs[i * 2 + 1] = new Vector2(1f, v);
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

        // ---------------------------
        // Material / Sprite-Lit setup
        // ---------------------------
        Material matToUse = null;

        if (roadMaterial != null)
        {
            // Use assigned material (recommended: a Sprite-Lit material)
            matToUse = new Material(roadMaterial);
        }
        else
        {
            // Try common sprite-lit shaders (URP 2D Lit first, then built-in Sprites/Lit), fallback to Sprites/Default
            Shader litURP = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            Shader litBuiltIn = Shader.Find("Sprites/Lit");
            Shader fallback = Shader.Find("Sprites/Default");

            Shader chosen = litURP ? litURP : (litBuiltIn ? litBuiltIn : fallback);
            matToUse = new Material(chosen);
        }

        // Assign the sprite texture and set tiling/offset if needed
        if (roadSprite != null)
        {
            matToUse.mainTexture = roadSprite.texture;
            // If using sprite atlases or textureRect, map UVs via _MainTex_ST if shader supports it
            Rect texRect = roadSprite.textureRect;
            Vector2 texSize = new Vector2(roadSprite.texture.width, roadSprite.texture.height);
            Vector2 uvOffset = new Vector2(texRect.x / texSize.x, texRect.y / texSize.y);
            Vector2 uvScale = new Vector2(texRect.width / texSize.x, texRect.height / texSize.y);

            // Many sprite shaders read _MainTex and use _MainTex_ST for tiling/offset.
            matToUse.SetTexture("_MainTex", roadSprite.texture);
            matToUse.SetVector("_MainTex_ST", new Vector4(uvScale.x, uvScale.y, uvOffset.x, uvOffset.y));
        }

        mr.sharedMaterial = matToUse;
        mr.sortingOrder = 0;

        // ---------------------------
        // Add PolygonCollider2D and set layer
        // ---------------------------

        // Set layer (make sure the layer exists in the project)
        int layer = LayerMask.NameToLayer(roadLayerName);
        if (layer == -1)
        {
            Debug.LogWarning($"MapManager: Layer '{roadLayerName}' does not exist. Please add it in Tags & Layers. Road GameObject will keep default layer.");
        }
        else
        {
            go.layer = layer;
        }

        // Build collider points following the mesh edges (clockwise or counter-clockwise)
        // NOTE: PolygonCollider2D expects local-space Vector2[] points forming a single closed loop.
        PolygonCollider2D poly = go.AddComponent<PolygonCollider2D>();

        // Prepare points: left edge from start->end, then right edge from end->start (reversed)
        Vector2[] colliderPoints = new Vector2[n * 2];

        // left side (verts[0], verts[2], verts[4], ...)
        for (int i = 0; i < n; i++)
        {
            Vector3 v = verts[i * 2 + 0]; // left
            colliderPoints[i] = new Vector2(v.x, v.y);
        }

        // right side reversed (verts[1], verts[3], ... reversed)
        for (int i = 0; i < n; i++)
        {
            Vector3 v = verts[(n - 1 - i) * 2 + 1]; // right side reversed
            colliderPoints[n + i] = new Vector2(v.x, v.y);
        }

        poly.points = colliderPoints;

        // optional: make collider used by physics queries immediately
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(poly);
#endif

        // add to segment list
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
