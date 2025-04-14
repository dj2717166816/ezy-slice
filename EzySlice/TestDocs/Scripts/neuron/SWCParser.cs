using System.Collections.Generic;
using UnityEngine;

public class SWCNode
{
    public int ID;
    public int Type;
    public Vector3 Position;
    public float Radius;
    public int ParentID;
}

public class SWCParser : MonoBehaviour
{
    public TextAsset swcFile; // 拖拽SWC文件到Inspector

    public List<SWCNode> nodes = new List<SWCNode>();

    void Start()
    {
        ParseSWC();
        GenerateSkeleton();
    }

    void ParseSWC()
    {
        string[] lines = swcFile.text.Split('\n');
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            string[] parts = line.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            SWCNode node = new SWCNode
            {
                ID = int.Parse(parts[0]),
                Type = int.Parse(parts[1]),
                Position = new Vector3(
                    float.Parse(parts[2]),
                    float.Parse(parts[3]),
                    float.Parse(parts[4])) / 100,
                Radius = float.Parse(parts[5]) / 100,
                ParentID = int.Parse(parts[6])
            };
            nodes.Add(node);
        }
    }

    void GenerateSkeleton()
    {
        foreach (SWCNode node in nodes)
        {
            // 生成节点球体
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = node.Position;
            sphere.transform.localScale = Vector3.one * node.Radius * 2; // 直径

            // 连接父节点
            if (node.ParentID != -1)
            {
                SWCNode parent = nodes.Find(n => n.ID == node.ParentID);
                if (parent != null)
                {
                    CreateConnection(parent.Position, node.Position, node.Radius);
                }
            }
        }
    }

    void CreateConnection(Vector3 start, Vector3 end, float radius)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.position = (start + end) / 2;
        cylinder.transform.up = (end - start).normalized;
        cylinder.transform.localScale = new Vector3(
            radius,
            Vector3.Distance(start, end) / 2,
            radius);
    }
}