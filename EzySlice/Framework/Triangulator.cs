using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EzySlice {

    /**
     * Contains static functionality for performing Triangulation on arbitrary vertices.
     * Read the individual function descriptions for specific details.
     */
    public sealed class Triangulator {

        /**
         * Represents a 3D Vertex which has been mapped onto a 2D surface
         * and is mainly used in MonotoneChain to triangulate a set of vertices
         * against a flat plane.
         */
        //将三维点投影到二维平面
        internal struct Mapped2D {
            private readonly Vector3 original;
            private readonly Vector2 mapped;

            public Mapped2D(Vector3 newOriginal, Vector3 u, Vector3 v) {
                this.original = newOriginal;
                this.mapped = new Vector2(Vector3.Dot(newOriginal, u), Vector3.Dot(newOriginal, v));
            }

            public Vector2 mappedValue {
                get { return this.mapped; }
            }

            public Vector3 originalValue {
                get { return this.original; }
            }
        }

        //对轮廓三角形化
        //输入有顺序的轮廓点集、平面法线，输出三角形集
        public static bool Triangulate(List<Vector3> vertices, Vector3 normal, out List<Triangle> tri, TextureRegion texRegion)
        {
            //vertices.Reverse();

            int count = vertices.Count;

            if (count < 3)
            {
                tri = null;
                return false;
            }

            tri = new List<Triangle>();
            List<int> indices = new List<int>();
            for (int i = 0; i < vertices.Count; i++)
            {
                indices.Add(i);
            }

            //创建平面上的正交向量
            Vector3 u = Vector3.Normalize(Vector3.Cross(normal, Vector3.up));
            if (Vector3.zero == u)
            {//防止法线与上方向平行
                u = Vector3.Normalize(Vector3.Cross(normal, Vector3.forward));
            }
            Vector3 v = Vector3.Cross(u, normal);

            //创建投影操作的数组
            Mapped2D[] mapped = new Mapped2D[count];
            float maxDivX = float.MinValue;
            float maxDivY = float.MinValue;
            float minDivX = float.MaxValue;
            float minDivY = float.MaxValue;

            //投影到二维平面
            for (int i = 0; i < count; i++)
            {
                Vector3 vertToAdd = vertices[i];

                Mapped2D newMappedValue = new Mapped2D(vertToAdd, u, v);
                Vector2 mapVal = newMappedValue.mappedValue;

                //确定uv范围
                maxDivX = Mathf.Max(maxDivX, mapVal.x);
                maxDivY = Mathf.Max(maxDivY, mapVal.y);
                minDivX = Mathf.Min(minDivX, mapVal.x);
                minDivY = Mathf.Min(minDivY, mapVal.y);

                mapped[i] = newMappedValue;
            }
            float width = maxDivX - minDivX;
            float height = maxDivY - minDivY;

//AI算法需检查
            //耳切法
            while (indices.Count >= 3)
            {
                bool earFound = false;
                for (int i = 0; i < indices.Count; i++)
                {

                    int prev = indices[(i - 1 + indices.Count) % indices.Count];
                    int curr = indices[i];
                    int next = indices[(i + 1) % indices.Count];

                    Vector2 a = mapped[prev].mappedValue;
                    Vector2 b = mapped[curr].mappedValue;
                    Vector2 c = mapped[next].mappedValue;

                    if (IsConvex(a, b, c) && !ContainsPoint(mapped, indices, a, b, c))
                    {
                        //获取点的uv位置
                        Vector2 uvA = mapped[prev].mappedValue;
                        Vector2 uvB = mapped[curr].mappedValue;
                        Vector2 uvC = mapped[next].mappedValue;

                        //将坐标映射到0-1
                        uvA.x = (uvA.x - minDivX) / width;
                        uvA.y = (uvA.y - minDivY) / height;

                        uvB.x = (uvB.x - minDivX) / width;
                        uvB.y = (uvB.y - minDivY) / height;

                        uvC.x = (uvC.x - minDivX) / width;
                        uvC.y = (uvC.y - minDivY) / height;

                        //以三维坐标创建三角形
                        Triangle newTriangle = new Triangle(mapped[prev].originalValue, mapped[curr].originalValue, mapped[next].originalValue);
                        newTriangle.SetUV(texRegion.Map(uvA), texRegion.Map(uvB), texRegion.Map(uvC));
                        newTriangle.SetNormal(normal, normal, normal);
                        newTriangle.ComputeTangents();

                        //将新生成的三角形加入输出网格
                        tri.Add(newTriangle);

                        indices.RemoveAt(i);
                        earFound = true;
                        break;
                    }
                }
                if (!earFound)
                {
                    break;
                }
            }
            
            if (indices.Count == 3)
            {
                //获取点的uv位置
                Vector2 uvA = mapped[0].mappedValue;
                Vector2 uvB = mapped[1].mappedValue;
                Vector2 uvC = mapped[2].mappedValue;

                //将坐标映射到0-1
                uvA.x = (uvA.x - minDivX) / width;
                uvA.y = (uvA.y - minDivY) / height;

                uvB.x = (uvB.x - minDivX) / width;
                uvB.y = (uvB.y - minDivY) / height;

                uvC.x = (uvC.x - minDivX) / width;
                uvC.y = (uvC.y - minDivY) / height;

                //以三维坐标创建三角形
                Triangle newTriangle = new Triangle(mapped[0].originalValue, mapped[1].originalValue, mapped[2].originalValue);
                newTriangle.SetUV(texRegion.Map(uvA), texRegion.Map(uvB), texRegion.Map(uvC));
                newTriangle.SetNormal(normal, normal, normal);
                newTriangle.ComputeTangents();

                //将新生成的三角形加入输出网格
                tri.Add(newTriangle);
            }
            if (tri.Count != count - 2)
            {
                vertices.Reverse();
                if (!Triangulate(vertices, normal, out tri, texRegion))
                {
                    return false; // 避免无限递归
                }
            }
            return true;
        }

        private static bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) > 0;
        }

        private static bool ContainsPoint(Mapped2D[] polygon, List<int> indices, Vector2 a, Vector2 b, Vector2 c)
        {
            for (int i = 0; i < indices.Count; i++)
            {
                Vector2 p = polygon[indices[i]].mappedValue;
                if (p != a && p != b && p != c && IsPointInTriangle(p, a, b, c))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float area = 0.5f * (-b.y * c.x + a.y * (-b.x + c.x) + a.x * (b.y - c.y) + b.x * c.y);

            if (Mathf.Abs(area) < 1e-6f) return false;  // 避免除零错误

            float invDenom = 1.0f / (2.0f * area);
            float s = ((a.y * c.x - a.x * c.y) + (c.y - a.y) * p.x + (a.x - c.x) * p.y) * invDenom;
            float t = ((a.x * b.y - a.y * b.x) + (a.y - b.y) * p.x + (b.x - a.x) * p.y) * invDenom;

            return s >= 0 && t >= 0 && (1 - s - t) >= 0;
        }

    }
}