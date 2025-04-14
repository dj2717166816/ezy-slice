Shader "Custom/NeuronVisualization"
{
    Properties
    {
        //_GlowIntensity ("Glow Intensity", Range(0, 1)) = 1
        _Color ("Color", Color) = (1, 1, 1, 1)  // 颜色属性
        _Shininess ("Shininess", Range(0.1, 1)) = 0.5 // 高光强度
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 5.0 // Fresnel效应的强度
        //_GlowColor ("Glow Color", Color) = (1, 0, 0, 1) // 发光颜色
        _Metallic ("Metallic", Range(0, 1)) = 0.0 // 金属度
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5 // 平滑度
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode"="ForwardBase" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR; // 从顶点读取颜色
                float3 normal : NORMAL; // 顶点法线
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR; // 传递颜色到片段着色器
                float3 normal : NORMAL; // 传递法线到片段着色器
                float3 worldPos : TEXCOORD0; // 传递世界空间位置
            };

            // Shader属性
            //float _GlowIntensity;
            float _Shininess;
            float4 _Color;
            float _FresnelPower;
            //float4 _GlowColor;
            float _Metallic;
            float _Smoothness;

            // 顶点着色器
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);  // 顶点位置变换
                o.color = v.color * _Color;  // 传递颜色并应用着色器的颜色属性
                o.normal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal)); // 计算世界空间法线
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz; // 计算世界空间位置
                return o;
            }

            // 计算标准光照的片段着色器
            fixed4 frag(v2f i) : SV_Target
            {
                // 获取世界空间的光源方向
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                // 计算漫反射光照（Lambertian反射）并减少阴影强度
                float diff = dot(i.normal, lightDir) * 0.5+0.5; // 减弱漫反射阴影
                // 计算高光（Blinn-Phong模型）并减少高光强度
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);  // 视线方向
                float3 halfDir = normalize(lightDir + viewDir);  // 半程向量
                float spec = pow(max(dot(i.normal, halfDir), 0.0), _Shininess * 128.0) * 0.5; // 减弱高光强度

                // Fresnel效果计算
                float3 safeNormal = normalize(i.normal);
                float NdotV = saturate(dot(safeNormal, normalize(viewDir)));
                float fresnel = pow(1.0 - NdotV, _FresnelPower); // 计算Fresnel效应

                // 发光强度控制
                float glowFactor = fresnel * (i.color.r-i.color.b)*1.5;

                // 漫反射 + 高光计算
                fixed4 finalColor = i.color * diff; // 漫反射
                finalColor += spec; // 高光

                // 发光效果（基于Fresnel）
                finalColor.rgb *= 0.5;
                finalColor.rgb += i.color.rgb * glowFactor; // 添加发光颜色

                // HDR颜色适配
                finalColor.rgb = min(finalColor.rgb, 5.0); // 防止过曝

                // 物理属性赋值
                finalColor.a = 1.0; // 保持不透明
                finalColor.rgb *= finalColor.a; // 影响颜色的Alpha通道

                // 输出最终颜色
                return finalColor*((i.color.r-i.color.b)*0.4+0.6);
            }
            ENDCG
        }
    }
}




