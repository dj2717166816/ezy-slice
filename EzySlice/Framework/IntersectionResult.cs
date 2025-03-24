using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EzySlice {

    public sealed class IntersectionResult {

        private List<Triangle> upper_tris;
        private List<Triangle> lower_tris;
        private List<Vector3> intersection_pt;//切割出的点
        private List<CuttingLineAndTri> cuttingLineAndTris;
        public Dictionary<CuttingLineAndTri,(int,bool)> has_clat;

        public sealed class CuttingLineAndTriComparer : IEqualityComparer<CuttingLineAndTri>
        {
            public bool Equals(CuttingLineAndTri x, CuttingLineAndTri y)
            {
                // 处理null情况
                if (ReferenceEquals(x, y)) return true;
                if (x is null || y is null) return false;

                // 获取两条线段
                Line lineX = x.line;
                Line lineY = y.line;

                // 比较线段端点，允许顺序相反
                return (lineX.positionA == lineY.positionA && lineX.positionB == lineY.positionB) ||
                       (lineX.positionA == lineY.positionB && lineX.positionB == lineY.positionA);
            }

            public int GetHashCode(CuttingLineAndTri obj)
            {
                if (obj is null) return 0;

                Line line = obj.line;
                // 使用异或交换不敏感的特性计算哈希码
                return line.positionA.GetHashCode() ^ line.positionB.GetHashCode();
            }
        }

        class ClatComparer : IEqualityComparer<CuttingLineAndTri>
        {
            private const float Tolerance = 1e-4f; // 允许的误差

            public bool Equals(CuttingLineAndTri a, CuttingLineAndTri b)
            {
                return (Equals(a.line.positionA, b.line.positionA) && Equals(a.line.positionB, b.line.positionB)) ||
                       (Equals(a.line.positionA, b.line.positionB) && Equals(a.line.positionB, b.line.positionA));
            }

            public bool Equals(Vector3 a, Vector3 b)
            {
                return Mathf.Abs(a.x - b.x) < Tolerance &&
                       Mathf.Abs(a.y - b.y) < Tolerance &&
                       Mathf.Abs(a.z - b.z) < Tolerance;
            }

            public int GetHashCode(CuttingLineAndTri obj)
            {
                return GetHashCodeUnordered(obj.line.positionA, obj.line.positionB);
            }
            private int GetHashCodeUnordered(Vector3 a, Vector3 b)
            {
                int hashA = GetHashCode(a);
                int hashB = GetHashCode(b);

                return hashA < hashB ? hashA * 31 + hashB : hashB * 31 + hashA;
            }

            public int GetHashCode(Vector3 obj)
            {
                unchecked
                {
                    // 先排序，但保留原始符号信息
                    float[] vals = new float[] { obj.x, obj.y, obj.z };
                    Array.Sort(vals);  // 先从小到大排序

                    int xHash = Mathf.RoundToInt(vals[0] * 1000) * 73856093;
                    int yHash = Mathf.RoundToInt(vals[1] * 1000) * 19349663;
                    int zHash = Mathf.RoundToInt(vals[2] * 1000) * 83492791;

                    // 加入符号影响，确保 -5 和 5 得到不同哈希
                    int signHash = (BitConverter.SingleToInt32Bits(obj.z) & 0x80000000) == 0 ? 1234577 : -1234577;

                    return xHash + yHash * 31 + zHash * 17 + signHash;
                }
            }
        }

        public IntersectionResult() {
            this.upper_tris = new List<Triangle>();
            this.lower_tris = new List<Triangle>(); 
            this.intersection_pt = new List<Vector3>();
            this.cuttingLineAndTris = new List<CuttingLineAndTri>();
            this.has_clat = new Dictionary<CuttingLineAndTri, (int, bool)>(new CuttingLineAndTriComparer());
        }

        public  List<Triangle> upperTri
        {
            get { return upper_tris; }
        }
        public List<Triangle> lowerTri
        {
            get { return lower_tris; }
        }
        public List<CuttingLineAndTri> intersectLines
        {
            get { return cuttingLineAndTris; }
        }
        public void AddIntersectionPoint(Vector3 pt) {
            intersection_pt.Add(pt);
        }
        public void AddCuttingLine(CuttingLineAndTri clat)
        {
            cuttingLineAndTris.Add(clat);
        }
        public void Clear() {
            upper_tris.Clear();
            lower_tris.Clear();
            intersection_pt.Clear();
            cuttingLineAndTris.Clear();
        }
    }
}