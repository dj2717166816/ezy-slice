using UnityEngine;
using UnityEditor;
using System.Collections.Generic; 

[System.Serializable]
public class NeuronLink // 替换原有NeuronConnection结构
{
    public NeuronNode source;  // 信号来源节点[1](@ref)
    public NeuronNode target;  // 信号目标节点
    public float resistance = 0.0001f;  // 连接阻抗
    [Range(0,1)] public float weight = 1f; // 新增权重参数[3](@ref)
}

public class NeuronNode : MonoBehaviour
{
    [Header("神经元参数")]
    public float a = 0.02f;
    public float b = 0.2f;
    public float c = -65f;
    public float d = 8f;

    [Header("连接配置")]
    public List<NeuronLink> incomingLinks = new List<NeuronLink>();  // 输入连接[1](@ref)
    public List<NeuronLink> outgoingLinks = new List<NeuronLink>();  // 输出连接[1](@ref)

    [Header("视觉反馈")]
    public float minScale = 0.5f;
    public float maxScale = 2f;

    private IzhikevichNeuron neuron;
    public float inputCurrent;
    [Header("外部输入")]
    public bool isFirstNode = false;     // 标记是否为起始节点
    public float externalPulse = 0f;    // 外部脉冲强度
    public float pulseDuration = 0.1f;  // 脉冲持续时间

    private float pulseTimer = 0f;
    //private int pressCount = 0;
    private float currentPulseStrength = 0f;
    void Start()
    {
        neuron = new IzhikevichNeuron(a, b, c, d);
    }

    void CalculateInput()
    {
        foreach (var link in incomingLinks)
        {
            if (link.source != null)
            {
                // 计算电压差并考虑权重[3](@ref)
                float voltageDiff = link.source.neuron.v - neuron.v;
                inputCurrent += (voltageDiff / link.resistance) * link.weight;
            }
        }
    }
    void UpdateNeuron()
    {
        bool spiked = neuron.Update(inputCurrent, Time.deltaTime);

        if (spiked)
        {
            foreach (var link in outgoingLinks)
            {
                if (link.target != null)
                {
                    // 添加脉冲衰减机制[3](@ref)
                    float effectiveCurrent = 100f * link.weight;
                    link.target.ReceiveSpike(effectiveCurrent);
                }
            }
        }
    }
    bool CheckConnectionLoop(NeuronNode checkNode, int depth = 0)
{
    if(depth > 10) return true; // 防止无限递归
    foreach(var link in checkNode.outgoingLinks)
    {
        if(link.target == this) return true;
        if(CheckConnectionLoop(link.target, depth+1)) return true;
    }
    return false;
}
    // 新增动态连接方法
    public void AddConnection(NeuronNode target, bool isBidirectional = false)
    {
        // 深度检测环路
        if(CheckConnectionLoop(target))
        {
            Debug.LogError("禁止创建循环连接");
            return;
        }
        var newLink = new NeuronLink {
            source = this,
            target = target,
            resistance = 0.0001f
        };
        outgoingLinks.Add(newLink);
        target.incomingLinks.Add(newLink);

        if (isBidirectional) {
            var reverseLink = new NeuronLink {
                source = target,
                target = this,
                resistance = 0.0001f
            };
            incomingLinks.Add(reverseLink);
            target.outgoingLinks.Add(reverseLink);
        }
    }


    void Update()
    {
        inputCurrent *= Mathf.Exp(-Time.deltaTime * 10f);
        HandleExternalPulse();
        
        CalculateInput();    // 计算来自前驱的电流
        UpdateNeuron();      // 更新神经元状态
        UpdateVisuals();     // 更新物体外观
    //    if (Input.GetKeyDown(KeyCode.Space))
   // {
   //     pressCount++;
   //     Debug.Log($"Press count: {pressCount}");
   // }
    }
        // 处理外部脉冲输入
    void HandleExternalPulse()
    {
        if (pulseTimer > 0)
        {
            // 使用指数衰减模型
            float decayFactor = Mathf.Exp(-Time.deltaTime * 5f); 
            currentPulseStrength *= decayFactor;
            
            inputCurrent += currentPulseStrength;
            pulseTimer -= Time.deltaTime;
        }
        else
        {
            currentPulseStrength = 0f; // 确保完全重置
        }
 
        // 按键触发
        if (isFirstNode && Input.GetKeyDown(KeyCode.Space))
        {
            ApplyPulse(10f); // 示例强度
        }
    }

    // 外部调用接口
    public void ApplyPulse(float strength)
    {
        pulseTimer = pulseDuration;
        currentPulseStrength = strength;
    }


    public void ReceiveSpike(float current)
    {
        inputCurrent += current; // 累加来自前驱的脉冲电流
    }

    void UpdateVisuals()
    {
        // 根据膜电位动态缩放
        float normalizedV = Mathf.InverseLerp(-70f, 30f, neuron.v);
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // 颜色从蓝色（低电位）到红色（高电位）渐变
            Color color = Color.Lerp(Color.blue, Color.red, normalizedV * 0.01f);
            rend.material.color = color;
        }
        //transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, normalizedV);
    }
}


public class IzhikevichNeuron
{
    private float a, b, c, d;
    public float v { get; private set; }
    private float u;

    public IzhikevichNeuron(float a, float b, float c, float d)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
        v = -65f;
        u = b * v;
    }

    public bool Update(float I, float dt)
    {
        float dv = (0.04f * v * v + 5f * v + 140f - u + I) * dt;
        float du = (a * (b * v - u)) * dt;

        v += dv;
        u += du;
        Debug.Log($"u: {u}");
        if (v >= 30f)
        {
            v = c;
            u += d;
            return true;
        }
        return false;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(NeuronNode))]
public class NeuronNodeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        NeuronNode node = (NeuronNode)target;
        
        GUILayout.Space(10);
        EditorGUILayout.LabelField("连接管理", EditorStyles.boldLabel);
        
        if(GUILayout.Button("添加随机连接")) 
        {
            var newNode = new GameObject("NeuronNode").AddComponent<NeuronNode>();
            node.AddConnection(newNode);
        }
        
        EditorGUILayout.HelpBox("输入连接数: " + node.incomingLinks.Count + 
                              "\n输出连接数: " + node.outgoingLinks.Count, 
                              MessageType.Info);
    }
}
#endif