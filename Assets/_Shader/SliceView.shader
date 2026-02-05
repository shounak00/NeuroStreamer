Shader "Medical/SliceView"
{
    Properties
    {
        _Volume ("Volume Texture", 3D) = "" {}
        _SlicePosition ("Slice Position", Range(0, 1)) = 0.5
        _SliceAxis ("Slice Axis", Int) = 2
        _MinThreshold ("Min Threshold", Range(0, 1)) = 0.0
        _MaxThreshold ("Max Threshold", Range(0, 1)) = 1.0
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };
            
            sampler3D _Volume;
            float _SlicePosition;
            int _SliceAxis;
            float _MinThreshold;
            float _MaxThreshold;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            
            float4 GetSliceColor(float density)
            {
                if (density < _MinThreshold || density > _MaxThreshold)
                {
                    return float4(0, 0, 0, 0.1);
                }
                
                float4 color;
                
                if (density < 0.2)
                {
                    color = float4(0.05, 0.05, 0.1, 1);
                }
                else if (density < 0.4)
                {
                    float t = (density - 0.2) / 0.2;
                    color = float4(0.2 + t * 0.2, 0.2 + t * 0.2, 0.2 + t * 0.2, 1);
                }
                else if (density < 0.6)
                {
                    float t = (density - 0.4) / 0.2;
                    color = float4(0.4 + t * 0.3, 0.4 + t * 0.3, 0.4 + t * 0.3, 1);
                }
                else if (density < 0.8)
                {
                    float t = (density - 0.6) / 0.2;
                    color = float4(0.7 + t * 0.25, 0.7 + t * 0.25, 0.65 + t * 0.2, 1);
                }
                else
                {
                    float t = (density - 0.8) / 0.2;
                    color = float4(0.9 + t * 0.1, 0.4 - t * 0.2, 0.3 - t * 0.1, 1);
                }
                
                return color;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float3 texCoord;
                
                if (_SliceAxis == 0)
                {
                    texCoord = float3(_SlicePosition, i.uv.x, i.uv.y);
                }
                else if (_SliceAxis == 1)
                {
                    texCoord = float3(i.uv.x, _SlicePosition, i.uv.y);
                }
                else
                {
                    texCoord = float3(i.uv.x, i.uv.y, _SlicePosition);
                }
                
                float density = tex3D(_Volume, texCoord).r;
                float4 color = GetSliceColor(density);
                
                float2 grid = frac(i.uv * 10.0);
                float gridLine = step(0.95, max(grid.x, grid.y));
                color.rgb = lerp(color.rgb, float3(0.3, 0.3, 0.3), gridLine * 0.15);
                
                float2 offset = float2(0.01, 0.01);
                float d1 = tex3D(_Volume, texCoord + float3(offset.x, 0, 0)).r;
                float d2 = tex3D(_Volume, texCoord - float3(offset.x, 0, 0)).r;
                float d3 = tex3D(_Volume, texCoord + float3(0, offset.y, 0)).r;
                float d4 = tex3D(_Volume, texCoord - float3(0, offset.y, 0)).r;
                
                float edge = abs(d1 - d2) + abs(d3 - d4);
                color.rgb += edge * 0.5;
                
                return color;
            }
            ENDCG
        }
    }
}
