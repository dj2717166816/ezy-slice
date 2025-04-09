//using EzySlice;
//using System;
//using System.Collections.Generic;
//using System.Threading;
//using UnityEngine;
//using Ezyslice;

//namespace EzySlice
//{
//    public class TriangleSearcher
//    {
//        private Thread searchThread;
//        private List<Triangle> triangles;
//        private Dictionary<Line, List<int>> LineTri;
//        private List<bool> visited;
//        private List<Triangle> output;
//        private Triangle startTriangle;

//        public TriangleSearcher(Triangle startTriangle, Dictionary<Line, List<int>> LineTri, List<Triangle> triangles, List<bool> visited, List<Triangle> output)
//        {
//            this.startTriangle = startTriangle;
//            this.LineTri = LineTri;
//            this.triangles = triangles;
//            this.visited = visited;
//            this.output = output;
//        }

//        public void StartSearch()
//        {
//            searchThread = new Thread(RunSearch, 1024 * 1024 * 1024); // 1G 栈大小，防止爆栈
//            searchThread.Start();
//            searchThread.Join(); // 等待搜索完成
//        }

//        private void RunSearch()
//        {
//            Search(startTriangle);
//        }

//        private void Search(Triangle tri)
//        {
//            Vector3D a = tri.positionA;
//            Vector3D b = tri.positionB;
//            Vector3D c = tri.positionC;
//            Line[] lines = { new Line(a, b), new Line(b, c), new Line(c, a) };

//            foreach (Line line in lines)
//            {
//                if (LineTri.TryGetValue(line, out List<int> triIndices))
//                {
//                    foreach (int index in triIndices)
//                    {
//                        if (!visited[index])
//                        {
//                            visited[index] = true;
//                            output.Add(triangles[index]);
//                            Search(triangles[index]); // 递归搜索
//                        }
//                    }
//                }
//            }
//        }
//    }
//}



using Ezyslice;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EzySlice
{
    public class TriangleSearcher
    {
        private int startIndex;
        private List<Triangle> triangles;
        private Dictionary<Line, List<int>> LineTri;
        private int[] visited; // 用数组代替 List<bool> 提高性能
        private ConcurrentQueue<int> bfsQueue = new ConcurrentQueue<int>();
        private ConcurrentBag<Triangle> output = new ConcurrentBag<Triangle>();

        private int threadCount;

        public void FillResult(List<Triangle> result)
        {
            result.AddRange(output);
        }

        public TriangleSearcher(Triangle startTriangle, Dictionary<Line, List<int>> LineTri, List<Triangle> triangles, List<bool> visitedList)
        {
            this.triangles = triangles;
            this.LineTri = LineTri;

            visited = new int[visitedList.Count];

            for (int i=0; i < visitedList.Count; i++)
            {
                visited[i] = (visitedList[i] ? 1 : 0);
            }

            // 寻找有效的起点索引
            Vector3D a = startTriangle.positionA;
            Vector3D b = startTriangle.positionB;
            Vector3D c = startTriangle.positionC;
            Line[] lines = { new Line(a, b), new Line(b, c), new Line(c, a) };

            foreach (Line line in lines)
            {
                if (LineTri.TryGetValue(line, out var triIndices))
                {
                    foreach (int idx in triIndices)
                    {
                        if (visited[idx] == 0)
                        {
                            startIndex = idx;
                            break;
                        }
                    }
                }
            }
        }

        public void StartSearch()
        {
            threadCount = Environment.ProcessorCount;

            // 标记起点
            visited[startIndex] = 1;
            bfsQueue.Enqueue(startIndex);
            output.Add(triangles[startIndex]);

            // 启动任务
            Task[] tasks = new Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = Task.Run(() => RunSearch());
            }

            Task.WaitAll(tasks);
        }

        private void RunSearch()
        {
            while (bfsQueue.TryDequeue(out int currentIndex))
            {
                Triangle current = triangles[currentIndex];

                Vector3D a = current.positionA;
                Vector3D b = current.positionB;
                Vector3D c = current.positionC;
                Line[] lines = { new Line(a, b), new Line(b, c), new Line(c, a) };

                foreach (Line line in lines)
                {
                    if (LineTri.TryGetValue(line, out var triIndices))
                    {
                        foreach (int neighborIndex in triIndices)
                        {
                            // 原子检查并设置 visited
                            if (visited[neighborIndex] == 0)
                            {
                                // 替代：if (!visited[neighborIndex]) { visited[neighborIndex] = true; ... }

                                if (Interlocked.CompareExchange(ref visited[neighborIndex], 1, 0) == 0)
                                {
                                    bfsQueue.Enqueue(neighborIndex);
                                    output.Add(triangles[neighborIndex]);
                                }

                            }
                        }
                    }
                }
            }
        }
    }
}
