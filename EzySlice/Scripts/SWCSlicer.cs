using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Ezyslice;
using EzySlice;
using System.Linq;
using UnityEditor.UI;
using NUnit.Framework;
using JetBrains.Annotations;

public class SWCSlicer : MonoBehaviour
{
    public string swcFilePath;  // SWC 文件路径
    public GameObject obj;
    public float step;
    public bool use_step_weight;
    public float step_weight;
    public Material crossmat;
    private class Node 
    {
        public int id;
        public Vector3 position;
        public float radius;
        public int parent;
        public List<int> children;
        public int compartment = -1;

        public Node(int id, Vector3 position, float radius, int parent)
        {
            this.id = id;
            this.position = position;
            this.radius = radius;
            this.parent = parent;
            this.children = new List<int>();
        }
    }

    private class Compartment
    {
        public int id;
        public List<int> neighbors;

        public Compartment(int id)
        {
            this.id = id;
            neighbors = new List<int>();
        }

        public void ProcessNeighbors()
        {
            this.neighbors = neighbors.Distinct().OrderBy(x => x).ToList();
        }
    }

    // 存储节点的列表
    private List<Node> nodes = new List<Node>();
    private List<Compartment> compartments = new List<Compartment>();

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
        //创建父物体集成所有子物体
        GameObject tree = new GameObject("NeuronsTree");

        CutNeuronWithPostOrder(tree, obj, 1);

        SetNeighbor();

        AddScripts(tree);
    }

    // 解析 SWC 文件
    void ParseSWCFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("SWC file not found.");
            return;
        }

        nodes.Add(new Node(0,Vector3.zero,0f,0));//添加一个使初始编号为1
        compartments.Add(new Compartment(0));

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
            float radius = float.Parse(parts[5]);
            int parentId = int.Parse(parts[6]);

            nodes.Add(new Node(id, new Vector3(-x, y, z), radius, parentId));
        }

        foreach(Node node in nodes)
        {
            if(node.parent != -1)
            {
                nodes[node.parent].children.Add(node.id);
            }
        }
    }

    public void CutNeuronWithPostOrder(GameObject parent, GameObject obj, int rootIndex)
    {
        Stack<(int, float, bool)> stack1 = new Stack<(int, float, bool)>();
        Stack<(int, bool)> stack2 = new Stack<(int, bool)>();

        stack1.Push((rootIndex, 0f, false));

        // 构造后序遍历顺序
        while (stack1.Count > 0)
        {
            var (node, distance, ok) = stack1.Pop();//前序遍历确定切割位置，a、b间有切割则b的ok=true
            stack2.Push((node, ok));//模拟后序真正切割

            if (nodes[node].children.Count == 0)//切到末端了，返回
                continue;

            int a = node;

            foreach (var b in nodes[a].children)
            {
                float segmentLength = LengthBetween(nodes[a].position, nodes[b].position);
                float newDistance = distance + segmentLength;

                if (newDistance > step)
                {
                    Vector3 mean = (nodes[a].position + nodes[b].position) / 2;
                    Vector3 v = nodes[b].position - nodes[a].position;

                    int ptnum = NumOfPtIn(obj, mean, v);

                    if (use_step_weight && ptnum == 1) { step = step_weight * Slicer.area; }

                    stack1.Push((b, ptnum == 1 ? 0f : newDistance, ptnum == 1));
                }
                else
                {
                    // 距离未到 step，继续往下推进
                    stack1.Push((b, newDistance, false));
                }
            }
        }

        GameObject tar = obj;

        // 后序处理
        while (stack2.Count > 0)
        {
            var (node, ok) = stack2.Pop();
            if (!ok) continue;

            int a = nodes[node].parent;
            int b = node;
            Vector3 mean = (nodes[a].position + nodes[b].position) / 2;
            Vector3 v = nodes[b].position - nodes[a].position;

            //Debug.Log($"{a}->{b}");

            GameObject[] outcomes = tar.SliceInstantiate(new EzySlice.Plane(new Vector3D(mean), new Vector3D(v)), new TextureRegion(0.0f, 0.0f, 1.0f, 1.0f), crossmat);//0是upper，1是lower
            if (outcomes.Length == 2)
            {
                if (tar == obj)
                {
                    tar.SetActive(false);
                }
                else
                {
                    Destroy(tar);
                }
                tar = outcomes[1];
                //step = Slicer.area * step_weight;//调整步长(在这调没用，要改)
                outcomes[0].transform.parent = parent.transform;
                outcomes[0].name = $"Compartment_{parent.transform.childCount}";
                Compartment newpart = new Compartment(parent.transform.childCount);
                compartments.Add(newpart);
                FindNeighbor(b, parent.transform.childCount);
            }
        }
        tar.transform.parent = parent.transform;
        tar.name = $"Compartment_{parent.transform.childCount}";
        Compartment np = new Compartment(parent.transform.childCount);
        compartments.Add(np);
        FindNeighbor(1, parent.transform.childCount);
    }

    //获取每个节点所属的部分，以找到每个部分的邻居
    public void FindNeighbor(int b, int com)
    {
        Queue<int> queue = new Queue<int>();
        queue.Enqueue(b);
        while (queue.Count > 0)
        {
            int pt = queue.Dequeue();
            if (nodes[pt].compartment == -1)
            {
                nodes[pt].compartment = com;
                foreach (var child in nodes[pt].children)
                {
                    queue.Enqueue(child);
                }
            }
            else
            {
                continue;
            }
        }
    }

    public void SetNeighbor()
    {
        Stack<(int, int)> stack = new Stack<(int, int)> ();
        stack.Push((1, nodes[1].compartment));
        while (stack.Count > 0)
        {
            var (node, com) = stack.Pop ();
            foreach (var child in nodes[node].children)
            {
                if (nodes[child].compartment != com)
                {
                    Debug.Log(com);
                    compartments[com].neighbors.Add(nodes[child].compartment);
                    compartments[nodes[child].compartment].neighbors.Add(com);
                }
                stack.Push((child, nodes[child].compartment));
            }
        }
        for (int i = 1; i < compartments.Count; i++)
        {
            compartments[i].ProcessNeighbors();
        }
    }

    public void AddScripts(GameObject parent)
    {
        for(int i = 1; i < compartments.Count; i++)
        {
            GameObject child = parent.transform.Find($"Compartment_{i}").gameObject;
            var ChildNode = child.AddComponent<NeuronNode>();
        }
        for (int i = 1; i < compartments.Count; i++)
        {
            GameObject child = parent.transform.Find($"Compartment_{i}").gameObject;
            var ChildNode = child.GetComponent<NeuronNode>();
            foreach (int neighbor in compartments[i].neighbors)
            {
                ChildNode.AddConnection(parent.transform.Find($"Compartment_{neighbor}").gameObject.GetComponent<NeuronNode>());
            }
        }
    }

    //获取在截面内的交点个数
    public int NumOfPtIn(GameObject tar, Vector3 pos, Vector3 normal)
    {
        int num = 0;
        Slicer.Slice(tar, new EzySlice.Plane(new Vector3D(pos), new Vector3D(normal)), new TextureRegion(0.0f, 0.0f, 1.0f, 1.0f), null, true);//获得area和VertOnContour
        List<Vector2> interpts = GettingInterpt(pos, normal);
        List<Vector3D> contour = Slicer.VertOnContour;
        List<Vector2> contour2d = new List<Vector2>();
        foreach (Vector3D v in contour)
        {
            Mapped2D map = new Mapped2D(v, Slicer.u, Slicer.v);
            contour2d.Add(map.mappedValue);
        }
        foreach (Vector2 pt in interpts)
        {
            if(IsPointInPolygon(pt, contour2d)) num++;
        }
        return num;
    }

    public List<Vector2> GettingInterpt(Vector3 pos, Vector3 normal)//获得swc与截面的交点
    {
        Slicer.SetUV(new Vector3D(normal));//获得uv
        List<Vector2> vertexs = new List<Vector2>();
        foreach(Node node in nodes)
        {
            if(node.parent != -1)
            {
                Vector3D v;
                Intersector.Intersect(new EzySlice.Plane(new Vector3D(pos), new Vector3D(normal)),new Vector3D(node.position), new Vector3D(nodes[node.parent].position), out v);
                //Debug.Log($"{v},{Slicer.u}, {Slicer.v}");
                Mapped2D map = new Mapped2D(v, Slicer.u, Slicer.v);
                vertexs.Add(map.mappedValue);
            }
        }
        return vertexs;
    }

    //判断点是否在截面轮廓内
    public bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        int count = polygon.Count;
        bool inside = false;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];

            bool intersect = ((pi.y > point.y) != (pj.y > point.y)) &&
                             (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y + Mathf.Epsilon) + pi.x);

            if (intersect)
                inside = !inside;
        }

        return inside;
    }

    public float LengthBetween(Vector3 start, Vector3 end)
    {
        Vector3 a = obj.transform.TransformPoint(start);
        Vector3 b = obj.transform.TransformPoint(end);
        Vector3 v = a - b;
        return v.magnitude;
    }
}