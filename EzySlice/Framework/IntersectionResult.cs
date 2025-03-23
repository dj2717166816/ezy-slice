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

        class ClatComparer : IEqualityComparer<CuttingLineAndTri>
        {
            private const float Tolerance = 1e-4f; // 允许的误差

            public bool Equals(CuttingLineAndTri a, CuttingLineAndTri b)
            {
                return Equals(a.line.positionA, b.line.positionA) && Equals(a.line.positionB, b.line.positionB);
            }
            public bool Equals(Vector3 a, Vector3 b)
            {
                return Mathf.Abs(a.x - b.x) < Tolerance &&
                       Mathf.Abs(a.y - b.y) < Tolerance &&
                       Mathf.Abs(a.z - b.z) < Tolerance;
            }
            public int GetHashCode(CuttingLineAndTri obj)
            {
                return GetHashCode(obj.line.positionA) + GetHashCode(obj.line.positionB);
            }
            public int GetHashCode(Vector3 obj)
            {
                int xHash = Mathf.RoundToInt(obj.x * 1000).GetHashCode();
                int yHash = Mathf.RoundToInt(obj.y * 1000).GetHashCode();
                int zHash = Mathf.RoundToInt(obj.z * 1000).GetHashCode();
                return xHash ^ (yHash << 2) ^ (zHash >> 2);
            }
        }

        public IntersectionResult() {
            this.upper_tris = new List<Triangle>();
            this.lower_tris = new List<Triangle>(); 
            this.intersection_pt = new List<Vector3>();
            this.cuttingLineAndTris = new List<CuttingLineAndTri>();
            this.has_clat = new Dictionary<CuttingLineAndTri, (int, bool)>(new ClatComparer());
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