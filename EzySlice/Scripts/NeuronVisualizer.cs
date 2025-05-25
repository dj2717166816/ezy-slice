using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

// Izhikevich神经元模型C#实现
public class OldIzhikevichNeuron
{
    // 神经元模型的参数（Izhikevich模型）
    public float a = 0.02f;  // 时间常数 a
    public float b = 0.2f;   // 参数 b
    public float c = -65f;   // 阈值电位 c
    public float d = 8f;     // 恢复电位的变化量 d

    public float v = -65f;   // 膜电位（初始为 -65mV）
    public float u;          // 恢复变量，初始化为 b * v

    // 构造函数，初始化 u 变量
    public OldIzhikevichNeuron()
    {
        u = b * v;
    }

    // 更新神经元状态的函数，I 为输入电流，dt 为时间步长
    public bool Update(float I, float dt)
    {
        // 计算膜电位和恢复变量的变化
        float dv = (0.04f * v * v + 5f * v + 140f - u + I) * dt;  // 膜电位更新公式
        float du = (a * (b * v - u)) * dt;                          // 恢复变量更新公式

        // 更新膜电位和恢复变量
        v += dv;
        u += du;

        // 当膜电位达到阈值时，重置膜电位，并增加恢复变量
        if (v >= 30f)
        {
            Debug.Log("spiking");
            v = c;    // 重置膜电位
            u += d;   // 增加恢复变量
            return true;  // 返回 true 表示神经元发放脉冲
        }

        return false; // 返回 false 表示没有发放脉冲
    }
}

// Unity组件实现，用于可视化神经元模型
[RequireComponent(typeof(MeshFilter))]  // 强制要求组件附加 MeshFilter
public class NeuronVisualizer : MonoBehaviour
{
    // 神经元参数，用于控制模拟的速度和电流幅度
    public float simulationSpeed = 1f;  // 模拟速度，控制时间进程
    public float currentAmplitude = 100f; // 设置输入电流的幅度为 20

    // 可视化参数，使用渐变显示膜电位
    public Gradient potentialGradient;  // 用于显示膜电位的渐变颜色
    public float minPotential = -100f;  // 膜电位的最小值
    public float maxPotential = 40f;  // 膜电位的最大值

    private UnityEngine.Mesh _mesh;         // Mesh 用于可视化
    private OldIzhikevichNeuron _neuron;    // 存储神经元对象
    private Color[] _vertexColors;         // 用于存储每个顶点的颜色

    // Start 函数在组件启动时被调用
    void Start()
    {
        InitializeNeurons();  // 初始化神经元
        InitializeMeshColors();  // 初始化 Mesh 顶点颜色
    }

    // 初始化神经元对象
    void InitializeNeurons()
    {
        _mesh = GetComponent<MeshFilter>().mesh;  // 获取附加在该物体上的 Mesh
        int vertexCount = _mesh.vertexCount;  // 获取 Mesh 的顶点数

        // 根据顶点数创建神经元对象
        _neuron = new OldIzhikevichNeuron();
        _vertexColors = new Color[vertexCount];  // 初始化顶点颜色数组
    }

    // 初始化 Mesh 顶点的颜色
    void InitializeMeshColors()
    {
        _mesh.colors = new Color[_mesh.vertexCount];  // 初始化颜色数组
    }

    // Update 函数每帧调用，用于更新神经元状态和顶点颜色
    void Update()
    {
        SimulateNeurons();  // 模拟神经元的活动
    }

    // 模拟神经元的活动
    void SimulateNeurons()
    {
        // 计算输入电流，使用20幅度的正弦波电流
        float inputCurrent = Mathf.Sin(Time.time * simulationSpeed) * currentAmplitude;
        //float inputCurrent = 0;
        //if (Time.time>=0&&Time.time<=0.6)
        //{
        //    inputCurrent = 100f;
        //}
        // 更新神经元状态
        _neuron.Update(inputCurrent, Time.deltaTime *simulationSpeed);

        // 计算膜电位对应的颜色插值
        float t = Mathf.InverseLerp(minPotential, maxPotential, _neuron.v);
        //sDebug.Log($"{_neuron.v}  {t}");

        // 使用渐变颜色显示神经元膜电位
        for (int i = 0; i < _vertexColors.Length; i++)
        {
            _vertexColors[i] = potentialGradient.Evaluate(t);
        }

        // 将计算出的颜色应用到 Mesh 的顶点
        _mesh.colors = _vertexColors;
    }
}


