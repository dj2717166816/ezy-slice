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
        // 创建一个空物体来承载所有的 LineRenderers
        GameObject treeParent = new GameObject("NeuronsTree");

        // 遍历所有节点，并用 LineRenderer 连接父节点和子节点
        for (int i = 0; i < nodes.Count; i++)
        {
            int parentId = parents[i];

            // 如果当前节点有父节点，绘制从父节点到当前节点的线
            if (parentId != -1)
            {
                // 创建一个 LineRenderer 用于连接父节点和当前节点
                GameObject lineObject = new GameObject("Line_" + i);
                lineObject.transform.parent = treeParent.transform;

                LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
                lineRenderer.material = lineMaterial;
                lineRenderer.startWidth = lineThickness;
                lineRenderer.endWidth = lineThickness;

                // 设置 LineRenderer 的点，连接父节点和当前节点
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, nodes[parentId-1]);
                lineRenderer.SetPosition(1, nodes[i]);
            }
        }
    }
}

/*
将swc解析为图
检查child多于两个的节点，标记为分叉点
用线段树维护未切割的节点属于哪个分支
 */