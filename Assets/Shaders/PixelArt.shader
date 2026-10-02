// Hậu kỳ "3D pixel art" (built-in pipeline): mỗi điểm ảnh của khung hình nhỏ lấy màu ở tâm ô tương ứng của khung hình
// gốc, viền tối ở mép vật thể (so độ sâu với ô bên cạnh), rồi ép về bảng màu cố định. PixelArtCamera.cs gọi shader này.
Shader "Hidden/PrehistoricTribe/PixelArt"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D_float _CameraDepthTexture;

            float4 _LowTexel;          // xy = 1 / độ phân giải nhỏ
            float _OutlineStrength;    // 0 = không viền, 1 = viền đen
            float _OutlineThreshold;   // chênh lệch độ sâu (tương đối) coi là mép
            float _PaletteStrength;    // 0 = giữ màu gốc, 1 = ép hẳn về bảng màu
            float _ColorBoost;         // tăng độ bão hòa trước khi ép màu
            float _Dither;             // độ rải điểm (Bayer 4×4) để chuyển màu mượt khi ép bảng màu
            int _PaletteSize;
            float4 _Palette[48];

            float Depth(float2 uv)
            {
                return LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
            }

            float3 NearestInPalette(float3 c)
            {
                float best = 1e9;
                float3 result = c;
                for (int i = 0; i < _PaletteSize; i++)
                {
                    float3 d = c - _Palette[i].rgb;
                    // Mắt nhạy với xanh lá hơn → trọng số kiểu "redmean" đơn giản.
                    float dist = dot(d * d, float3(0.3, 0.59, 0.11));
                    if (dist < best)
                    {
                        best = dist;
                        result = _Palette[i].rgb;
                    }
                }
                return result;
            }

            // Ma trận Bayer 4×4 (0..15) — rải điểm có trật tự kiểu pixel art cổ điển.
            float Bayer(float2 cell)
            {
                int x = (int)fmod(cell.x, 4.0);
                int y = (int)fmod(cell.y, 4.0);
                const float m[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
                return m[y * 4 + x] / 16.0 - 0.47;
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                // Lấy màu đúng tâm ô điểm ảnh của khung hình nhỏ (không pha trộn với ô bên cạnh).
                float2 cell = floor(i.uv / _LowTexel.xy);
                float2 uv = (cell + 0.5) * _LowTexel.xy;
                float3 col = tex2D(_MainTex, uv).rgb;

                // Viền: ô này nằm trước (gần camera hơn) hẳn so với một ô bên cạnh → là mép vật thể phía trước.
                float d = Depth(uv);
                float dl = Depth(uv - float2(_LowTexel.x, 0));
                float dr = Depth(uv + float2(_LowTexel.x, 0));
                float du = Depth(uv + float2(0, _LowTexel.y));
                float dd = Depth(uv - float2(0, _LowTexel.y));
                float farthest = max(max(dl, dr), max(du, dd));
                float edge = step(_OutlineThreshold * d, farthest - d);
                col *= 1.0 - edge * _OutlineStrength;

                float grey = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(grey.xxx, col, _ColorBoost);
                float3 dithered = saturate(col + Bayer(cell) * _Dither);
                col = lerp(col, NearestInPalette(dithered), _PaletteStrength);
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
