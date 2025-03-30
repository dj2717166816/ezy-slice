using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using Ezyslice;

namespace EzySlice
{
    public class hashline {
        public Vector3D positionA;
        public Vector3D positionB;
        public List<int> i;
        public List<int> j;


        public hashline(Vector3D a, Vector3D b, int i, int j)
        {
            this.positionA = a;
            this.positionB = b;
            this.i = new List<int>();
            this.i.Add(i);
            this.j = new List<int>();
            this.j.Add(i);
        }
    }
    public sealed class CuttingLineAndTri {
        public hashline line;
        public int TriIndex;
        public int flag;
        public (int, bool)[] doubletri;

        public CuttingLineAndTri(Vector3D a, Vector3D b, int x, int y, int i, int f)
        {
            this.line = new hashline(a, b, x, y);
            this.TriIndex = i;
            this.flag = f;
            if (i == -1)
            {
                doubletri = new (int, bool)[2];
            }
        }
    }
}
