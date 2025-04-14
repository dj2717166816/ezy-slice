Shader "Custom/GlowEffect" {
    Properties {
        [Header(Base Settings)]
        //_MainTex ("Base Color", 2D) = "white" {}
        _Color ("Color", Color) = (1.0,1.0,1.0,1)
        _Smoothness ("Smoothness",Range(0,1))=0.5
        _Metallic ("Metallic",Range(0,1))=0
        
        [Header(Glow Settings)]
        [HDR]_GlowColor ("Glow Color", Color) = (1,0.5,0,1)  // 添加HDR属性
        _GlowIntensity ("Intensity", Range(0,5)) = 1         // 降低最大强度
        _FresnelPower ("Fresnel Power", Range(0.1,5)) = 1    // 增加最小值限制
        
        [Header(Flow Settings)]
        _FlowSpeed ("Flow Speed", Range(0,3)) = 1.0    
        _WaveLength ("Wave Length", Range(0.01,2)) = 0.5  // 防止除零错误
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" "DisableBatching"="True" }
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input {
            float2 uv_MainTex;
            float3 viewDir;     
            float3 worldPos;     // 新增世界坐标
            float3 worldNormal;  
        };

        //sampler2D _MainTex;
        float4 _Color;
        float4 _GlowColor;
        float _GlowIntensity;
        float _FresnelPower;
        float _Smoothness;
        float _Metallic;
        float _FlowSpeed;
        float _WaveLength;

        void surf (Input IN, inout SurfaceOutputStandard o) {
            // 基础颜色采样
            //fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            float4 c=_Color;
            o.Albedo = c.rgb;
            
            // 流动参数修正
            float flow = _Time.y * _FlowSpeed + IN.worldPos.x / _WaveLength; // 改用除法控制波形
            float gradient = (sin(flow) + 1) * 0.5; // 使用sin函数获得平滑过渡
            
            // 菲涅尔计算优化
            float3 safeNormal = normalize(IN.worldNormal);
            float NdotV = saturate(dot(safeNormal, normalize(IN.viewDir)));
            float fresnel = pow(1.0 - NdotV, _FresnelPower);
            
            // 发光强度控制
            float glowFactor = fresnel * gradient * _GlowIntensity;
            
            // HDR颜色适配
            o.Emission = _GlowColor.rgb * glowFactor;
            
            // 物理属性赋值
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = c.a;
            
            // 亮度限制（可选）
            o.Emission = min(o.Emission, 5.0); // 防止HDR过曝
        }
        ENDCG
    }
    FallBack "Diffuse"
}