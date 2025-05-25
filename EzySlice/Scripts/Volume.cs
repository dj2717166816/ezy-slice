using UnityEngine;

public class Volume : MonoBehaviour
{
    public MeshFilter meshFilter; // 将你的 Mesh 对象拖入 Unity 编辑器

    void Start()
    {
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter is not assigned.");
            return;
        }
        
    }

    void Update()
    {
        float volume = CalculateMeshVolume(meshFilter.mesh);
        Debug.Log("Mesh Volume: " + volume);
    }

    float CalculateMeshVolume(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        float volume = 0f;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            //Vector3 v0 = vertices[triangles[i]];
            //Vector3 v1 = vertices[triangles[i + 1]];
            //Vector3 v2 = vertices[triangles[i + 2]];

            Vector3 v0 = meshFilter.transform.TransformPoint(vertices[triangles[i]]);
            Vector3 v1 = meshFilter.transform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 v2 = meshFilter.transform.TransformPoint(vertices[triangles[i + 2]]);


            volume += SignedVolumeOfTriangle(v0, v1, v2);
        }

        return Mathf.Abs(volume);
    }

    float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return Vector3.Dot(p1, Vector3.Cross(p2, p3)) / 6f;
    }
}


