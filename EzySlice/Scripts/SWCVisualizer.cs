using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SWCVisualizer : MonoBehaviour
{
    public string swcFilePath;  // SWC 文件路径
    public Material lineMaterial;  // LineRenderer 的材质
    public float lineThickness;
    public float scale;

    // 存储节点的列表
    private List<Vector3> nodes = new List<Vector3>();
    private List<int> parents = new List<int>();

    void Start()
    {
        swcFilePath = Application.dataPath + swcFilePath;
        if (string.IsNullOrEmpty(swcFilePath))
        {
            Debug.LogError("SWC file path is not set.");
            return;
        }

        // 解析 SWC 文件
        ParseSWCFile(swcFilePath);

        // 可视化神经元的树状结构
        VisualizeTree();
    }

    // 解析 SWC 文件
    void ParseSWCFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("SWC file not found.");
            return;
        }

        string[] lines = File.ReadAllLines(filePath);
        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            string[] parts = line.Split(' ');
            if (parts.Length < 7)
                continue;

            int id = int.Parse(parts[0]);
            float x = float.Parse(parts[2]);
            float y = float.Parse(parts[3]);
            float z = float.Parse(parts[4]);
            int parentId = int.Parse(parts[6]);

            nodes.Add(new Vector3(-x, y, z)*scale);
            parents.Add(parentId);
        }
    }

    // 使用 LineRenderer 可视化树状结构
    void VisualizeTree()
    {
        GameObject treeParent = new GameObject("NeuronsTree");
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i] = treeParent.transform.InverseTransformPoint(nodes[i]);
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            int parentId = parents[i];
            if (parentId != -1)
            {
                GameObject lineObject = new GameObject("Line_" + i);
                lineObject.transform.parent = treeParent.transform;

                LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
                lineRenderer.material = lineMaterial;
                lineRenderer.startWidth = lineThickness;
                lineRenderer.endWidth = lineThickness;

                // 使用局部坐标
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, nodes[parentId - 1] + this.transform.position);
                lineRenderer.SetPosition(1, nodes[i] + this.transform.position);
            }
        }

        
    }
}

/*
将swc解析为图
检查child多于两个的节点，标记为分叉点
用线段树维护未切割的节点属于哪个分支
 */