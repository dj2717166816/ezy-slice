Shader "Custom/EmissiveShader"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            }; // 修复：结构体定义后添加分号

            struct v2f
            {
                float4 pos : SV_POSITION; // 修正语义为SV_POSITION
                float4 worldPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex); // 世界坐标计算
                o.pos = UnityObjectToClipPos(v.vertex); // 裁剪空间坐标
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // 使用frac获取坐标的小数部分，确保颜色在0-1范围内
                return half4(0.3+0.7*abs(frac(i.worldPos.x)), 0.6+0.4*abs(frac(i.worldPos.x)), 0.9+0.1*abs(frac(i.worldPos.x)), 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
