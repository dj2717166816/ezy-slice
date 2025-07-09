using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[System.Serializable]
public class NeuronLink // 替换原有NeuronConnection结构
{
    public NeuronNode source;  // 信号来源节点[1](@ref)
    public NeuronNode target;  // 信号目标节点
    public float resistance = 1e-1f;  // 连接阻抗
    [Range(0, 1)] public float weight = 1f; // 新增权重参数[3](@ref)
}

public class NeuronNode : MonoBehaviour
{
    // Izhikevich模型参数（预设四种典型神经元类型）
    [Header("神经元参数")]
    public float a = 0.02f;// 恢复变量时间尺度
    public float b = 0.2f;// 恢复变量敏感度
    public float c = -65f;// 脉冲后重置电压
    public float d = 8f;// 脉冲后恢复变量调整值

    [Header("连接配置")]
    public List<NeuronLink> incomingLinks = new List<NeuronLink>();  // 输入连接[1](@ref)
    public List<NeuronLink> outgoingLinks = new List<NeuronLink>();  // 输出连接[1](@ref)

    [Header("视觉反馈")]
    public float minScale = 0.5f;// 最小缩放尺寸（对应静息状态）
    public float maxScale = 2f;// 最大缩放尺寸（对应兴奋状态）

    private float currentV = -65f;     // 当前帧膜电位
    private float currentU = 0f;       // 当前帧恢复变量
    private float previousV = -65f;    // 上一帧膜电位
    private float previousU = 0f;      // 上一帧恢复变量
    public float inputCurrent = 0f;
    public Gradient potentialGradient;

    private Mesh cachedMesh;
    private Color[] cachedColors;

    [Header("外部输入")]
    public bool isFirstNode = false;     // 标记是否为起始节点
    public float externalPulse = 0f;    // 外部脉冲强度
    public float pulseDuration = 0.1f;  // 脉冲持续时间

    private float pulseTimer = 0f;
    private float currentPulseStrength = 0f;
    void Start()
    {
        previousV = c;// 初始化为重置电压
        previousU = b * c;// 恢复变量初始值
        currentV = previousV;
        currentU = previousU;
        InitializeDefaultGradient();

        // 只执行一次：获取 mesh 引用并分配颜色数组
        cachedMesh = GetComponent<MeshFilter>().mesh;
        cachedColors = new Color[cachedMesh.vertexCount];
    }
    // 在场景视图中绘制连接线（调试用）
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;// 使用绿色表示输出连接
        foreach (var link in outgoingLinks)
        {
            if (link.target) Gizmos.DrawLine(transform.position, link.target.transform.position);
        }
    }
    // 计算来自输入连接的电流
    void CalculateInput()
    {
        foreach (var link in incomingLinks)
        {
            if (link.source != null)
            {
                // 欧姆定律计算电流：I = ΔV/R * weight
                float voltageDiff = link.source.previousV - previousV;
                inputCurrent += (voltageDiff / link.resistance) * link.weight;
            }
        }
    }
    // 更新神经元状态
    void UpdateNeuron()
    {
        bool spiked = IzhikevichUpdate(inputCurrent, Time.deltaTime);

        //if (spiked)
        //{
        //    foreach (var link in outgoingLinks)
        //    {
        //        if (link.target != null)
        //        {
        //            // 添加脉冲衰减机制[3](@ref)
        //            float effectiveCurrent = 325f * link.weight;
        //            link.target.ReceiveSpike(effectiveCurrent);
        //        }
        //    }
        //}
    }
    // 新增的双缓冲更新方法
    public bool IzhikevichUpdate(float I, float dt)
    {
        // Heun法两次积分 v
        float v = previousV;
        float u = previousU;

        float dv1 = 0.04f * v * v + 5f * v + 140f - u + I;
        v += 0.5f * dv1 * dt;

        float dv2 = 0.04f * v * v + 5f * v + 140f - u + I;
        v += 0.5f * dv2 * dt;

        float du = a * (b * v - u) * dt;
        u += du;

        currentV = v;
        currentU = u;

        if (currentV >= 30f)
        {
            currentV = c;
            currentU += d;
            return true;
        }

        return false;

        //// 使用上一帧状态(previousV/U)计算当前状态
        //float dv = (0.04f * previousV * previousV + 5f * previousV + 140f - previousU + I) * dt;
        //float du = (a * (b * previousV - previousU)) * dt;
        //// 更新当前状态
        //currentV = previousV + dv;
        //currentU = previousU + du;
        //// 检测脉冲（膜电位达到阈值30mV）
        //if (currentV >= 30f)
        //{
        //    currentV = c;
        //    currentU = currentU + d;
        //    return true;
        //}
        //return false;
    }

    // 新增动态连接方法（没啥用）
    public void AddConnection(NeuronNode target, float res, bool isBidirectional = true)
    {

        var newLink = new NeuronLink
        {
            source = this,
            target = target,
            resistance = res
        };
        outgoingLinks.Add(newLink);
        target.incomingLinks.Add(newLink);

        if (isBidirectional)
        {
            var reverseLink = new NeuronLink
            {
                source = target,
                target = this,
                resistance = res
            };
            incomingLinks.Add(reverseLink);
            target.outgoingLinks.Add(reverseLink);
        }
    }


    void Update()
    {
        // 1. 缓冲状态交换：将上一帧状态设为当前状态
        previousV = currentV;
        previousU = currentU;
        float decayedCurrent = inputCurrent * Mathf.Exp(-Time.deltaTime * 10f);
        //inputCurrent = decayedCurrent; // 替换原衰减逻辑
        inputCurrent = 0;
        HandleExternalPulse();

        CalculateInput();    // 计算来自前驱的电流
        UpdateNeuron();      // 更新神经元状态
        UpdateVisuals();     // 更新物体外观
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
            ApplyPulse(externalPulse); // 示例强度
        }
    }

    // 外部调用接口
    public void ApplyPulse(float strength)
    {
        pulseTimer = pulseDuration;
        currentPulseStrength = strength;
    }

    // 接收前驱神经元脉冲
    public void ReceiveSpike(float current)
    {
        inputCurrent += current; // 累加来自前驱的脉冲电流
    }
    // 更新视觉表现（根据膜电位动态缩放）
    void UpdateVisuals()
    {
        // 根据膜电位 currentV 在 -70 到 30 之间归一化
        float normalizedV = Mathf.InverseLerp(-70f, 30f, currentV);

        for (int i = 0; i < cachedColors.Length; i++)
        {
            cachedColors[i] = potentialGradient.Evaluate(normalizedV);
        }
        cachedMesh.colors = cachedColors;
    }
    void InitializeDefaultGradient()
    {
        if (potentialGradient == null || potentialGradient.colorKeys == null || potentialGradient.colorKeys.Length == 0)
        {
            potentialGradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[4];
            colorKeys[0].color = Color.cyan;   // 低电位：青
            colorKeys[0].time = 0f;

            colorKeys[1].color = Color.green;  // 稍高电位：绿
            colorKeys[1].time = 0.33f;

            colorKeys[2].color = Color.yellow; // 高电位：黄
            colorKeys[2].time = 0.66f;

            colorKeys[3].color = Color.red;    // 最高电位：红
            colorKeys[3].time = 1f;

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].alpha = 1f;
            alphaKeys[0].time = 0f;
            alphaKeys[1].alpha = 1f;
            alphaKeys[1].time = 1f;

            potentialGradient.SetKeys(colorKeys, alphaKeys);
        }
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
        
        //I = sum[(v_j-v_i)/R] j是和i相邻的单元

        v += dv;
        u += du;
        //Debug.Log($"u: {u}");
        if (v >= 30f)
        {
            v = c;
            u += d;
            return true;
        }
        return false;
    }
}

