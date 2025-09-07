void Kernel_UV_3x3_float(float2 UV, float2 Texel_Size,
	out float2 UV_TL,
	out float2 UV_T,
	out float2 UV_TR,
	out float2 UV_L,
	out float2 UV_R,
	out float2 UV_BL,
	out float2 UV_B,
	out float2 UV_BR,
	out float2 UV_C)
{
	float tx = Texel_Size.x;
	float ty = Texel_Size.y;
	UV_TL = UV + float2(-tx, ty);
	UV_T = UV + float2(0, ty);
	UV_TR = UV + float2(tx, ty);
	
	UV_L = UV + float2(-tx, 0);
	UV_C = UV;
	UV_R = UV + float2(tx, 0);

	UV_BL = UV + float2(-tx, -ty);
	UV_B = UV + float2(0, -ty);
	UV_BR = UV + float2(tx, -ty);
}

void Kernel_UV_3x3_half(half2 UV, half2 Texel_Size,
	out half2 UV_TL,
	out half2 UV_T,
	out half2 UV_TR,
	out half2 UV_L,
	out half2 UV_R,
	out half2 UV_BL,
	out half2 UV_B,
	out half2 UV_BR,
	out half2 UV_C)
{
	half tx = Texel_Size.x;
	half ty = Texel_Size.y;
	UV_TL = UV + half2(-tx, ty);
	UV_T = UV + half2(0, ty);
	UV_TR = UV + half2(tx, ty);

	UV_L = UV + half2(-tx, 0);
	UV_C = UV;
	UV_R = UV + half2(tx, 0);

	UV_BL = UV + half2(-tx, -ty);
	UV_B = UV + half2(0, -ty);
	UV_BR = UV + half2(tx, -ty);
}

void Kernel_UV_Plus_float(float2 UV, float2 Texel_Size,
	out float2 UV_T,
	out float2 UV_L,
	out float2 UV_R,
	out float2 UV_B,
	out float2 UV_C)
{
	float tx = Texel_Size.x;
	float ty = Texel_Size.y;
	UV_L = UV + float2(-tx, 0);
	UV_T = UV + float2(0, ty);
	UV_R = UV + float2(tx, 0);
	UV_B = UV + float2(0, -ty);
	UV_C = UV;
}

void Kernel_UV_Plus_half(half2 UV, half2 Texel_Size,
	out half2 UV_T,
	out half2 UV_L,
	out half2 UV_R,
	out half2 UV_B,
	out half2 UV_C)
{
	half tx = Texel_Size.x;
	half ty = Texel_Size.y;
	UV_L = UV + half2(-tx, 0);
	UV_T = UV + half2(0, ty);
	UV_R = UV + half2(tx, 0);
	UV_B = UV + half2(0, -ty);
	UV_C = UV;
}
