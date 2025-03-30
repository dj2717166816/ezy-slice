using EzySlice;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Ezyslice;

namespace EzySlice
{
    public class TriangleSearcher
    {
        private Thread searchThread;
        private List<Triangle> triangles;
        private Dictionary<Line, List<int>> LineTri;
        private List<bool> visited;
        private List<Triangle> output;
        private Triangle startTriangle;

        public TriangleSearcher(Triangle startTriangle, Dictionary<Line, List<int>> LineTri, List<Triangle> triangles, List<bool> visited, List<Triangle> output)
        {
            this.startTriangle = startTriangle;
            this.LineTri = LineTri;
            this.triangles = triangles;
            this.visited = visited;
            this.output = output;
        }

        public void StartSearch()
        {
            searchThread = new Thread(RunSearch, 1024 * 1024 * 1024); // 1G Õ»´óÐ¡£¬·ÀÖ¹±¬Õ»
            searchThread.Start();
            searchThread.Join(); // µÈ´ýËÑË÷Íê³É
        }

        private void RunSearch()
        {
            Search(startTriangle);
        }

        private void Search(Triangle tri)
        {
            Vector3D a = tri.positionA;
            Vector3D b = tri.positionB;
            Vector3D c = tri.positionC;
            Line[] lines = { new Line(a, b), new Line(b, c), new Line(c, a) };

            foreach (Line line in lines)
            {
                if (LineTri.TryGetValue(line, out List<int> triIndices))
                {
                    foreach (int index in triIndices)
                    {
                        if (!visited[index])
                        {
                            visited[index] = true;
                            output.Add(triangles[index]);
                            Search(triangles[index]); // µÝ¹éËÑË÷
                        }
                    }
                }
            }
        }
    }

}