using Ezyslice;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EzySlice
{
    public class TriangleSearcher
    {
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

        public TriangleSearcher(List<Triangle> startlist, Dictionary<Line, List<int>> LineTri, List<Triangle> triangles, List<bool> visitedList)
        {
            this.triangles = triangles;
            this.LineTri = LineTri;

            visited = new int[visitedList.Count];

            for (int i=0; i < visitedList.Count; i++)
            {
                visited[i] = (visitedList[i] ? 1 : 0);
            }

            foreach (Triangle startTriangle in startlist)
            {
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
                                bfsQueue.Enqueue(idx);
                                output.Add(triangles[idx]);
                                visited[idx] = 1;
                            }
                        }
                    }
                }
            }
        }

        public void StartSearch()
        {
            threadCount = Environment.ProcessorCount;

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
