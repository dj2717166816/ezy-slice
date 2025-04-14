using UnityEngine;

namespace DataCaculater
{
    public static class Volume
    {
        public static float CalculateMeshVolume(this GameObject obj)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.mesh;
            if (mesh == null||meshFilter == null) { return 0; }

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            float volume = 0f;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = meshFilter.transform.TransformPoint(vertices[triangles[i]]);
                Vector3 v1 = meshFilter.transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 v2 = meshFilter.transform.TransformPoint(vertices[triangles[i + 2]]);

                volume += SignedVolumeOfTriangle(v0, v1, v2);
            }

            return Mathf.Abs(volume);
        }

        public static float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return Vector3.Dot(p1, Vector3.Cross(p2, p3)) / 6f;
        }
    }
}