// Địa hình + cây cỏ trang trí (LandscapeBuilder): màu lấy từ màu đỉnh (mỗi mặt một màu → low-poly), chiếu sáng Lambert,
// đổ/nhận bóng; thêm "hạt" pixel theo toạ độ thế giới (texture xám lọc Point) cho mặt đất bớt trơn.
Shader "PrehistoricTribe/VertexColorLit"
{
    Properties
    {
        _Grain ("Grain (xám, ~0.5 trung bình)", 2D) = "gray" {}
        _GrainStrength ("Độ mạnh hạt", Range(0, 1)) = 0.35
        _GrainScale ("Số ô hạt mỗi mét", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows
        #pragma target 3.0

        sampler2D _Grain;
        float _GrainStrength;
        float _GrainScale;

        struct Input
        {
            float4 color : COLOR;
            float3 worldPos;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            float grain = tex2D(_Grain, IN.worldPos.xz * _GrainScale).r * 2.0; // 0.5 → 1 (giữ nguyên màu)
            o.Albedo = IN.color.rgb * lerp(1.0, grain, _GrainStrength);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
