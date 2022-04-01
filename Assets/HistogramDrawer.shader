Shader "Custom/Histogram"
{
    Properties
    {
        [MainTexture][HideInInspector]
        _MainTex("Texture", 2D) = "white" {}
        _Size("Size", Range(0.0, 1.0)) = 0.02
        _Minimum("Minimum", Range(0.0, 1.0)) = 0.2
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            StructuredBuffer<int4> _HistogramValues;
            float4 _HistogramScalar;
            float _Size;
            float _Minimum;

            

            float columnColor(float valY, float posY)
            {
                if(posY > valY) return 0;
                
                const float size = _Size;
                const float minimum = _Minimum;

                float sizeRev = 1 / size;
                float linearFuncVal = sizeRev * posY + 1 - sizeRev * valY;
                
                return max(linearFuncVal, minimum);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                const float u = i.uv.x;
                const float v = i.uv.y;

                float4 result;

                if(v < 0.5)
                {
                    const int countI = 256;
                    const float countF = float(countI);

                    int x = floor(i.uv.x * 1 * countF); // argument funkcji

                    float4 value = float4(_HistogramValues[x]) * _HistogramScalar * 0.5;
                    
                    result.r = columnColor(value.r, v);
                    result.g = columnColor(value.g, v);
                    result.b = columnColor(value.b, v);
                }
                else
                {
                    float2 uvTexture = float2(u,v * 2.0 - 1.0);
                    result = tex2D(_MainTex, uvTexture);
                }
                
                return result;
            }
            
            ENDCG
        }
    }
}