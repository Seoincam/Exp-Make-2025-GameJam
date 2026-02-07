Shader "Custom/HexCircleScroll"
{
    Properties
    {
        _MainColor ("Circle Color", Color) = (0.3, 0.6, 1.0, 1.0)
        _BGColor ("Background Color", Color) = (0.05, 0.05, 0.15, 1.0)
        _CircleRadius ("Circle Radius", Range(0.05, 0.45)) = 0.3
        _Softness ("Edge Softness", Range(0.001, 0.15)) = 0.03
        _Scale ("Pattern Scale", Range(1, 30)) = 8
        _ScrollSpeed ("Scroll Speed", Vector) = (0.3, 0.2, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            fixed4 _MainColor;
            fixed4 _BGColor;
            float _CircleRadius;
            float _Softness;
            float _Scale;
            float4 _ScrollSpeed;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            // 육각형 격자에서 가장 가까운 셀 중심까지의 거리 반환
            float hexCircle(float2 uv)
            {
                // 육각형 격자 기저 벡터
                // 행 간격: sqrt(3)/2, 홀수 행은 x로 0.5 오프셋
                float2 scaled = uv * _Scale;

                // 사선 스크롤 적용
                scaled += _ScrollSpeed.xy * _Time.y;

                // 행/열 계산
                float rowH = 0.866025; // sqrt(3)/2
                float row = floor(scaled.y / rowH);
                float xOffset = fmod(row, 2.0) * 0.5;
                float col = floor(scaled.x - xOffset);

                // 현재 셀 중심
                float2 center = float2(col + xOffset + 0.5, row * rowH + rowH * 0.5);

                // 인접 6개 셀 포함해서 최소 거리 찾기
                float minDist = 100.0;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        float neighborRow = row + dy;
                        float neighborXOffset = fmod(abs(neighborRow), 2.0) * 0.5;
                        float neighborCol = col + dx;

                        float2 neighborCenter = float2(
                            neighborCol + neighborXOffset + 0.5,
                            neighborRow * rowH + rowH * 0.5
                        );

                        float dist = length(scaled - neighborCenter);
                        minDist = min(minDist, dist);
                    }
                }

                return minDist;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float dist = hexCircle(i.uv);

                // 부드러운 원 마스크
                float circle = 1.0 - smoothstep(_CircleRadius - _Softness, _CircleRadius + _Softness, dist);

                // 색상 혼합
                fixed4 col = lerp(_BGColor, _MainColor, circle);
                col.a *= i.color.a;

                return col;
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
