using EzySlice;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DataCaculater
{
    public static class SurfaceArea
    {
        public static float CaculateArea(this GameObject obj)
        {
            float area = 0f;
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.mesh == null)
            {
                Debug.LogError("No Mesh found!");
                return 0f;
            }
            Mesh mesh = meshFilter.mesh;

            Vector3[] vertices = mesh.vertices; // 所有顶点坐标
            int[] triangles = mesh.triangles;   // 每3个元素组成1个三角形的顶点索引

            for (int i = 0; i < triangles.Length; i += 3)
            {
                // 获取每个三角形的3个顶点索引
                int vertexIndex1 = triangles[i];
                int vertexIndex2 = triangles[i + 1];
                int vertexIndex3 = triangles[i + 2];

                // 取出对应的顶点坐标
                Vector3 v1 = vertices[vertexIndex1];
                Vector3 v2 = vertices[vertexIndex2];
                Vector3 v3 = vertices[vertexIndex3];

                //转换到世界坐标系
                Vector3 worldV1 = obj.transform.TransformPoint(v1);
                Vector3 worldV2 = obj.transform.TransformPoint(v2);
                Vector3 worldV3 = obj.transform.TransformPoint(v3);

                area += CalculateTriangleArea(worldV1, worldV2, worldV3);
            }
            return area;
        }

        public static float CalculateTriangleArea(Vector3 A, Vector3 B, Vector3 C)
        {
            // 计算边向量 AB 和 AC
            Vector3 AB = B - A;
            Vector3 AC = C - A;

            // 叉乘计算法向量
            Vector3 crossProduct = Vector3.Cross(AB, AC);

            // 面积 = 叉乘长度的一半
            float area = crossProduct.magnitude * 0.5f;
            return area;
        } 
    }
}
