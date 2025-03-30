using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using Ezyslice;

namespace EzySlice
{
    public sealed class CuttingLineAndTri{
        public Line line;
        public int TriIndex;
        public int flag;
        public (int,bool)[] doubletri;
        public CuttingLineAndTri(Vector3D a, Vector3D b, int i, int f)
        {
            this.line = new Line(a, b);
            this.TriIndex = i;
            this.flag = f;
            if (i == -1)
            {
                doubletri = new (int, bool)[2];
            }
        }
    }
}
