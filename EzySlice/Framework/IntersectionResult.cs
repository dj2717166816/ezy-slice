using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Ezyslice;

namespace EzySlice {

    public sealed class IntersectionResult {

        public int hash;
        private List<Triangle> upper_tris;
        private List<Triangle> lower_tris;
        private List<Vector3D> intersection_pt;//切割出的点
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
                Line lineX = new Line(x.line.positionA, x.line.positionB);
                Line lineY = new Line(y.line.positionA, y.line.positionB);

                // 比较线段端点，允许顺序相反
                return (lineX.positionA == lineY.positionA && lineX.positionB == lineY.positionB) ||
                       (lineX.positionA == lineY.positionB && lineX.positionB == lineY.positionA);
            }

            public int GetHashCode(CuttingLineAndTri obj)
            {
                if (obj is null) return 0;

                Line line = new Line(obj.line.positionA, obj.line.positionB);
                // 使用异或交换不敏感的特性计算哈希码
                return line.positionA.GetHashCode() ^ line.positionB.GetHashCode();
            }
        }

        public IntersectionResult() {
            this.upper_tris = new List<Triangle>();
            this.lower_tris = new List<Triangle>(); 
            this.intersection_pt = new List<Vector3D>();
            this.cuttingLineAndTris = new List<CuttingLineAndTri>();
            this.has_clat = new Dictionary<CuttingLineAndTri, (int, bool)>(new CuttingLineAndTriComparer());
            this.hash = 0;
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
            cuttingLineAndTris.Add(clat);
            hash += 2;
        }
        public void Clear() {
            upper_tris.Clear();
            lower_tris.Clear();
            intersection_pt.Clear();
            cuttingLineAndTris.Clear();
            hash = 0;
        }
    }
}