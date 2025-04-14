using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Ezyslice;

namespace EzySlice {

    public sealed class IntersectionResult {

        private List<Triangle> upper_tris;
        private List<Triangle> lower_tris;
        private List<Vector3D> intersection_pt;//切割出的点
        private List<CuttingLineAndTri> cuttingLineAndTris;
        public Dictionary<CuttingLineAndTri,(int,bool)> has_clat;

        public sealed class CuttingLineAndTriComparer : IEqualityComparer<CuttingLineAndTri>
        {
            double Tolerance = 1e-3;

            public bool Equals(CuttingLineAndTri x, CuttingLineAndTri y)
            {
                // 获取两条线段
                Line lineX = x.line;
                Line lineY = y.line;

                // 比较线段端点，允许顺序相反
                return (Equals(lineX.positionA, lineY.positionA) && Equals(lineX.positionB, lineY.positionB)) ||
                       (Equals(lineX.positionA, lineY.positionB) && Equals(lineX.positionB, lineY.positionA));
            }

            public int GetHashCode(CuttingLineAndTri obj)
            {
                Line line = obj.line;
                // 使用异或交换不敏感的特性计算哈希码
                return line.positionA.GetHashCode() ^ line.positionB.GetHashCode();
            }

            public bool Equals(Vector3D a, Vector3D b)
            {
                return (Math.Abs(a.x - b.x) < Tolerance &&
                       Math.Abs(a.y - b.y) < Tolerance &&
                       Math.Abs(a.z - b.z) < Tolerance);
            }
        }

        public IntersectionResult() {
            this.upper_tris = new List<Triangle>();
            this.lower_tris = new List<Triangle>(); 
            this.intersection_pt = new List<Vector3D>();
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
        public void AddIntersectionPoint(Vector3D pt) {
            intersection_pt.Add(pt);
        }
        public void AddCuttingLine(CuttingLineAndTri clat)
        {
            //Debug.Log("man");
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