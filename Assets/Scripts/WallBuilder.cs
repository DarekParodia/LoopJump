using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WallBuilder : MonoBehaviour
{
    [Header("Dimensions")]
    public float width = 4f;
    public float height = 4f;

    [Header("Cap Heights")]
    public float bottomCapHeight = 1f;
    public float topCapHeight = 1f;

    [Header("Materials")]
    public Material bottomMaterial;
    public Material midMaterial;
    public Material topMaterial;

    void Start()
    {
        Build();
    }

    public void Build()
    {
        float bot = bottomCapHeight;
        float top = topCapHeight;
        float mid = Mathf.Max(0f, height - bot - top);

        float y0 = 0f;
        float y1 = bot;
        float y2 = bot + mid;
        float y3 = height;

        float midTileY = bot > 0f ? mid / bot : 1f;

        // 3 quads, 4 verts each
        Vector3[] vertices = new Vector3[]
        {
            // Bottom (0-3)
            new Vector3(0, y0, 0), new Vector3(width, y0, 0),
            new Vector3(width, y1, 0), new Vector3(0, y1, 0),

            // Mid (4-7)
            new Vector3(0, y1, 0), new Vector3(width, y1, 0),
            new Vector3(width, y2, 0), new Vector3(0, y2, 0),

            // Top (8-11)
            new Vector3(0, y2, 0), new Vector3(width, y2, 0),
            new Vector3(width, y3, 0), new Vector3(0, y3, 0),
        };

        Vector2[] uvs = new Vector2[]
        {
            // Bottom cap
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1),

            // Mid — tiled
            new Vector2(0, 0),        new Vector2(1, 0),
            new Vector2(1, midTileY), new Vector2(0, midTileY),

            // Top cap
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1),
        };

        // Normals pointing forward
        Vector3[] normals = new Vector3[12];
        for (int i = 0; i < 12; i++)
            normals[i] = Vector3.back;

        // Each submesh is one quad (two triangles)
        int[] triBottom = { 0, 2, 1, 0, 3, 2 };
        int[] triMid    = { 4, 6, 5, 4, 7, 6 };
        int[] triTop    = { 8,10, 9, 8,11,10 };

        Mesh mesh = new Mesh();
        mesh.name = "Wall";
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.subMeshCount = 3;
        mesh.SetTriangles(triBottom, 0);
        mesh.SetTriangles(triMid,    1);
        mesh.SetTriangles(triTop,    2);

        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().sharedMaterials = new Material[]
        {
            bottomMaterial,
            midMaterial,
            topMaterial
        };
    }
}