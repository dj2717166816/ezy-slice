Shader "Custom/FlowColorAlongX"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _FlowSpeed ("Flow Speed", Range(0,5)) = 0.5    // 新增流动速度参数
        _WaveLength ("Wave Length", Range(0,2)) = 0.3  // 新增波长参数
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _FlowSpeed;
        float _WaveLength;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // 计算流动参数
            float flow = _Time.y * _FlowSpeed - IN.worldPos.x * _WaveLength;
            float gradient = frac(flow); // 生成0-1循环渐变
            
            // 创建颜色渐变（从红色渐变到蓝色）
            float3 flowColor = lerp(float3(1,0,0), float3(0,0,1), gradient);
            
            // 混合贴图颜色和流动颜色
            fixed4 texColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            float3 finalColor = lerp(texColor.rgb, flowColor, gradient * 0.5);
            
            // 输出表面属性
            o.Albedo = finalColor;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = texColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
//Shader "Custom/NeuronVisualization"
//{
//    Properties
//    {
//        _GlowIntensity ("Glow Intensity", Range(0, 1)) = 1
//        _Color ("Color", Color) = (1, 1, 1, 1)  // 颜色属性
//        _Shininess ("Shininess", Range(0.1, 1)) = 0.5 // 高光强度
//    }

//    SubShader
//    {
//        Tags { "RenderType"="Opaque" "LightMode"="ForwardBase" }

//        Pass
//        {
//            CGPROGRAM
//            #pragma vertex vert
//            #pragma fragment frag
//            #include "UnityCG.cginc"

//            struct appdata
//            {
//                float4 vertex : POSITION;
//                float4 color : COLOR; // 从顶点读取颜色
//                float3 normal : NORMAL; // 顶点法线
//            };

//            struct v2f
//            {
//                float4 pos : SV_POSITION;
//                float4 color : COLOR; // 传递颜色到片段着色器
//                float3 normal : NORMAL; // 传递法线到片段着色器
//            };

//            // Shader属性
//            float _GlowIntensity;
//            float _Shininess;
//            float4 _Color;

//            // 顶点着色器
//            v2f vert(appdata v)
//            {
//                v2f o;
//                o.pos = UnityObjectToClipPos(v.vertex);  // 顶点位置变换
//                o.color = v.color * _Color;  // 传递颜色并应用着色器的颜色属性
//                o.normal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal)); // 计算世界空间法线
//                return o;
//            }

//            // 计算标准光照的片段着色器
//            fixed4 frag(v2f i) : SV_Target
//            {
//                // 获取世界空间的光源方向
//                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
//                // 计算漫反射光照（Lambertian反射）
//                float diff = max(dot(i.normal, lightDir), 0.0);
//                // 计算高光（Blinn-Phong模型）
//                float3 viewDir = normalize(_WorldSpaceCameraPos - i.pos.xyz);  // 视线方向
//                float3 halfDir = normalize(lightDir + viewDir);  // 半程向量
//                float spec = pow(max(dot(i.normal, halfDir), 0.0), _Shininess * 128.0); // 高光强度

//                // 输出光照后的颜色
//                fixed4 finalColor = i.color * diff; // 漫反射
//                finalColor += spec; // 高光

//                // 添加发光效果
//                finalColor *= _GlowIntensity;

//                return finalColor;
//            }
//            ENDCG
//        }
//    }
//}
