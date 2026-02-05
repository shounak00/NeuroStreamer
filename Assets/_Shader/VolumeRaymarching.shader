Shader "Medical/VolumeRaymarching"
{
    Properties
    {
        _Volume ("Volume Texture", 3D) = "" {}
        _MinThreshold ("Min Threshold", Range(0, 1)) = 0.0
        _MaxThreshold ("Max Threshold", Range(0, 1)) = 1.0
        _StepSize ("Step Size", Range(0.001, 0.1)) = 0.01
        _DensityMultiplier ("Density Multiplier", Range(0.1, 5)) = 2.0
        _Brightness ("Brightness", Range(0.5, 3)) = 1.5
        _Contrast ("Contrast", Range(0.5, 2)) = 1.2
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Front
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 objectPos : TEXCOORD1;
            };
            
            sampler3D _Volume;
            float _MinThreshold;
            float _MaxThreshold;
            float _StepSize;
            float _DensityMultiplier;
            float _Brightness;
            float _Contrast;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.objectPos = v.vertex.xyz;
                return o;
            }
            
            // Enhanced lighting calculation
            float3 CalculateNormal(float3 pos, float stepSize)
            {
                float3 offset = float3(stepSize, stepSize, stepSize);
                
                float dx = tex3D(_Volume, pos + float3(offset.x, 0, 0)).r - 
                          tex3D(_Volume, pos - float3(offset.x, 0, 0)).r;
                float dy = tex3D(_Volume, pos + float3(0, offset.y, 0)).r - 
                          tex3D(_Volume, pos - float3(0, offset.y, 0)).r;
                float dz = tex3D(_Volume, pos + float3(0, 0, offset.z)).r - 
                          tex3D(_Volume, pos - float3(0, 0, offset.z)).r;
                
                return normalize(float3(dx, dy, dz));
            }
            
            // Improved transfer function
            float4 TransferFunction(float density, float3 normal, float3 viewDir)
            {
                float4 color = float4(0, 0, 0, 0);
                
                // Apply threshold
                if (density < _MinThreshold || density > _MaxThreshold)
                {
                    return color;
                }
                
                // Adjust for contrast and brightness
                density = saturate((density - 0.5) * _Contrast + 0.5) * _Brightness;
                
                // Enhanced color mapping
                if (density < 0.25)
                {
                    // CSF/Low density (dark blue-black)
                    float t = density / 0.25;
                    color = float4(0.05 * t, 0.05 * t, 0.15 * t, density * 0.4);
                }
                else if (density < 0.45)
                {
                    // Gray matter (gray)
                    float t = (density - 0.25) / 0.2;
                    color = float4(0.3 + t * 0.2, 0.3 + t * 0.2, 0.35 + t * 0.15, density * 0.6);
                }
                else if (density < 0.65)
                {
                    // White matter (lighter gray)
                    float t = (density - 0.45) / 0.2;
                    color = float4(0.5 + t * 0.25, 0.5 + t * 0.25, 0.5 + t * 0.2, density * 0.75);
                }
                else if (density < 0.82)
                {
                    // Bone (white/cream)
                    float t = (density - 0.65) / 0.17;
                    color = float4(0.75 + t * 0.25, 0.72 + t * 0.23, 0.65 + t * 0.2, density * 0.9);
                }
                else
                {
                    // High density - tumor/abnormality (red-orange highlight)
                    float t = (density - 0.82) / 0.18;
                    color = float4(0.9 + t * 0.1, 0.4 - t * 0.1, 0.2 - t * 0.1, density * 1.1);
                }
                
                // Apply basic lighting
                float3 lightDir = normalize(float3(1, 1, -1));
                float ndotl = max(0, dot(normal, lightDir));
                float ambient = 0.3;
                float diffuse = ndotl * 0.7;
                
                color.rgb *= (ambient + diffuse);
                
                // Add specular highlight for high-density regions
                if (density > 0.7)
                {
                    float3 halfDir = normalize(lightDir + viewDir);
                    float spec = pow(max(0, dot(normal, halfDir)), 20.0) * 0.5;
                    color.rgb += spec;
                }
                
                // Apply density multiplier
                color.a *= _DensityMultiplier;
                color.a = saturate(color.a);
                
                return color;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Ray setup
                float3 rayOrigin = i.objectPos + float3(0.5, 0.5, 0.5);
                float3 cameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1)).xyz + float3(0.5, 0.5, 0.5);
                float3 rayDir = normalize(rayOrigin - cameraPos);
                
                // Calculate ray entry and exit points
                float3 invRayDir = 1.0 / (rayDir + 0.0001);
                float3 t0 = (float3(0, 0, 0) - rayOrigin) * invRayDir;
                float3 t1 = (float3(1, 1, 1) - rayOrigin) * invRayDir;
                
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);
                
                float tNear = max(max(tmin.x, tmin.y), tmin.z);
                float tFar = min(min(tmax.x, tmax.y), tmax.z);
                
                // Check if ray intersects the volume
                if (tNear >= tFar || tFar <= 0)
                {
                    discard;
                }
                
                tNear = max(0, tNear);
                
                // Raymarching
                float3 rayPos = rayOrigin + rayDir * tNear;
                float4 accumulatedColor = float4(0, 0, 0, 0);
                float transmittance = 1.0;
                
                int maxSteps = 200;
                float rayLength = tFar - tNear;
                float actualStepSize = max(_StepSize, rayLength / float(maxSteps));
                
                [loop]
                for (int step = 0; step < maxSteps; step++)
                {
                    if (transmittance < 0.01) break;
                    
                    // Sample volume
                    float density = tex3D(_Volume, rayPos).r;
                    
                    // Calculate normal for lighting
                    float3 normal = CalculateNormal(rayPos, actualStepSize);
                    
                    // Get color from transfer function
                    float4 sampleColor = TransferFunction(density, normal, -rayDir);
                    
                    // Front-to-back compositing
                    sampleColor.a *= actualStepSize * 80;
                    sampleColor.rgb *= sampleColor.a;
                    
                    accumulatedColor += sampleColor * transmittance;
                    transmittance *= (1.0 - sampleColor.a);
                    
                    // Advance ray
                    rayPos += rayDir * actualStepSize;
                    
                    // Check if we've exited the volume
                    if (any(rayPos < 0) || any(rayPos > 1))
                        break;
                }
                
                // Apply transmittance to alpha
                accumulatedColor.a = 1.0 - transmittance;
                
                // Tone mapping for better contrast
                accumulatedColor.rgb = accumulatedColor.rgb / (accumulatedColor.rgb + 1.0);
                
                return accumulatedColor;
            }
            ENDCG
        }
    }
}
