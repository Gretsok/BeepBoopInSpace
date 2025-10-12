void EdgeDetectColor_float(
	float4 TL, float4 T, float4 TR,
	float4 L, float4 R,
	float4 BL, float4 B, float4 BR,
	float PeakBrightness,
	out float Edge)
{
	float4 zero = float4(0, 0, 0, 0);
	float4 peak = float4(PeakBrightness.xxxx);

	TL = clamp(TL, zero, peak);
	T = clamp(T, zero, peak);
	TR = clamp(TR, zero, peak);

	L = clamp(L, zero, peak);
	R = clamp(R, zero, peak);

	BL = clamp(BL, zero, peak);
	B = clamp(B, zero, peak);
	BR = clamp(BR, zero, peak);

	float K1 = 1;
	float K2 = 2;

	float2 gR = float2(
		K1 * TL.r + K2 * L.r + K1 * BL.r - K1 * TR.r - K2 * R.r - K1 * BR.r,
		K1 * TL.r + K2 * T.r + K1 * TR.r - K1 * BL.r - K2 * B.r - K1 * BR.r);
	float gmR = (gR.x * gR.x + gR.y * gR.y);

	float2 gG = float2(
		K1 * TL.g + K2 * L.g + K1 * BL.g - K1 * TR.g - K2 * R.g - K1 * BR.g,
		K1 * TL.g + K2 * T.g + K1 * TR.g - K1 * BL.g - K2 * B.g - K1 * BR.g);
	float gmG = (gG.x * gG.x + gG.y * gG.y);

	float2 gB = float2(
		K1 * TL.b + K2 * L.b + K1 * BL.b - K1 * TR.b - K2 * R.b - K1 * BR.b,
		K1 * TL.b + K2 * T.b + K1 * TR.b - K1 * BL.b - K2 * B.b - K1 * BR.b);
	float gmB = (gB.x * gB.x + gB.y * gB.y);

	Edge = (gmR + gmG + gmB) * 0.333;
}

void EdgeDetectColor_half(
	half4 TL, half4 T, half4 TR,
	half4 L, half4 R,
	half4 BL, half4 B, half4 BR,
	half PeakBrightness,
	out half Edge)
{
	half4 zero = half4(0, 0, 0, 0);
	half4 peak = half4(PeakBrightness.xxxx);

	TL = clamp(TL, zero, peak);
	T = clamp(T, zero, peak);
	TR = clamp(TR, zero, peak);

	L = clamp(L, zero, peak);
	R = clamp(R, zero, peak);

	BL = clamp(BL, zero, peak);
	B = clamp(B, zero, peak);
	BR = clamp(BR, zero, peak);

	half K1 = 1;
	half K2 = 2;

	half2 gR = half2(
		K1 * TL.r + K2 * L.r + K1 * BL.r - K1 * TR.r - K2 * R.r - K1 * BR.r,
		K1 * TL.r + K2 * T.r + K1 * TR.r - K1 * BL.r - K2 * B.r - K1 * BR.r);
	half gmR = (gR.x * gR.x + gR.y * gR.y);

	half2 gG = half2(
		K1 * TL.g + K2 * L.g + K1 * BL.g - K1 * TR.g - K2 * R.g - K1 * BR.g,
		K1 * TL.g + K2 * T.g + K1 * TR.g - K1 * BL.g - K2 * B.g - K1 * BR.g);
	half gmG = (gG.x * gG.x + gG.y * gG.y);

	half2 gB = half2(
		K1 * TL.b + K2 * L.b + K1 * BL.b - K1 * TR.b - K2 * R.b - K1 * BR.b,
		K1 * TL.b + K2 * T.b + K1 * TR.b - K1 * BL.b - K2 * B.b - K1 * BR.b);
	half gmB = (gB.x * gB.x + gB.y * gB.y);

	Edge = (gmR + gmG + gmB) * 0.333;
}

void EdgeDetectWorldNormal_float(
	float3 TL, float3 T, float3 TR,
	float3 L, float3 R,
	float3 BL, float3 B, float3 BR,
	out float Edge)
{
	TL = TL * 0.5 + 0.5;
	T = T * 0.5 + 0.5;
	TR = TR * 0.5 + 0.5;
	L = L * 0.5 + 0.5;
	R = R * 0.5 + 0.5;
	BL = BL * 0.5 + 0.5;
	B = B * 0.5 + 0.5;
	BR = BR * 0.5 + 0.5;

	float K1 = 1;
	float K2 = 2;

	float2 gR = float2(
		K1 * TL.r + K2 * L.r + K1 * BL.r - K1 * TR.r - K2 * R.r - K1 * BR.r,
		K1 * TL.r + K2 * T.r + K1 * TR.r - K1 * BL.r - K2 * B.r - K1 * BR.r);
	float gmR = (gR.x * gR.x + gR.y * gR.y);

	float2 gG = float2(
		K1 * TL.g + K2 * L.g + K1 * BL.g - K1 * TR.g - K2 * R.g - K1 * BR.g,
		K1 * TL.g + K2 * T.g + K1 * TR.g - K1 * BL.g - K2 * B.g - K1 * BR.g);
	float gmG = (gG.x * gG.x + gG.y * gG.y);

	float2 gB = float2(
		K1 * TL.b + K2 * L.b + K1 * BL.b - K1 * TR.b - K2 * R.b - K1 * BR.b,
		K1 * TL.b + K2 * T.b + K1 * TR.b - K1 * BL.b - K2 * B.b - K1 * BR.b);
	float gmB = (gB.x * gB.x + gB.y * gB.y);

	Edge = (gmR + gmG + gmB) * 0.333;
}

void EdgeDetectWorldNormal_half(
	half3 TL, half3 T, half3 TR,
	half3 L, half3 R,
	half3 BL, half3 B, half3 BR,
	out half Edge)
{
	TL = TL * 0.5 + 0.5;
	T = T * 0.5 + 0.5;
	TR = TR * 0.5 + 0.5;
	L = L * 0.5 + 0.5;
	R = R * 0.5 + 0.5;
	BL = BL * 0.5 + 0.5;
	B = B * 0.5 + 0.5;
	BR = BR * 0.5 + 0.5;

	half K1 = 1;
	half K2 = 2;

	half2 gR = half2(
		K1 * TL.r + K2 * L.r + K1 * BL.r - K1 * TR.r - K2 * R.r - K1 * BR.r,
		K1 * TL.r + K2 * T.r + K1 * TR.r - K1 * BL.r - K2 * B.r - K1 * BR.r);
	half gmR = (gR.x * gR.x + gR.y * gR.y);

	half2 gG = half2(
		K1 * TL.g + K2 * L.g + K1 * BL.g - K1 * TR.g - K2 * R.g - K1 * BR.g,
		K1 * TL.g + K2 * T.g + K1 * TR.g - K1 * BL.g - K2 * B.g - K1 * BR.g);
	half gmG = (gG.x * gG.x + gG.y * gG.y);

	half2 gB = half2(
		K1 * TL.b + K2 * L.b + K1 * BL.b - K1 * TR.b - K2 * R.b - K1 * BR.b,
		K1 * TL.b + K2 * T.b + K1 * TR.b - K1 * BL.b - K2 * B.b - K1 * BR.b);
	half gmB = (gB.x * gB.x + gB.y * gB.y);

	Edge = (gmR + gmG + gmB) * 0.333;
}


void EdgeDetectDepthRaw_float(
	float TL, float T, float TR,
	float L, float R,
	float BL, float B, float BR,
	float Response,
	out float Edge)
{
#if UNITY_REVERSE_Z
	TL = 1 - TL;
	T = 1 - T;
	TR = 1 - TR;
	L = 1 - L;
	R = 1 - R;
	BL = 1 - BL;
	B = 1 - B;
	BR = 1 - BR;
#endif

	float KRES = 100;
	TL *= Response * KRES;
	T *= Response * KRES;
	TR *= Response * KRES;
	L *= Response * KRES;
	R *= Response * KRES;
	BL *= Response * KRES;
	B *= Response * KRES;
	BR *= Response * KRES;

	float2 gradient = float2(
		TL + 2 * L + BL - TR - 2 * R - BR,
		TL + 2 * T + TR - BL - 2 * B - BR);
	float gradientMagnitude = (gradient.x * gradient.x + gradient.y * gradient.y);

	Edge = gradientMagnitude;
}


void EdgeDetectDepthRaw_half(
	half TL, half T, half TR,
	half L, half R,
	half BL, half B, half BR,
	half Response,
	out half Edge)
{
	TL *= Response;
	T *= Response;
	TR *= Response;
	L *= Response;
	R *= Response;
	BL *= Response;
	B *= Response;
	BR *= Response;

	half2 gradient = half2(
		TL + 2 * L + BL - TR - 2 * R - BR,
		TL + 2 * T + TR - BL - 2 * B - BR);
	half gradientMagnitude = (gradient.x * gradient.x + gradient.y * gradient.y);

	Edge = gradientMagnitude * Response;
}

void EdgeDetectDepthRawFast_float(
	float T, float L, float R, float B,
	float Response,
	out float Edge)
{
#if UNITY_REVERSE_Z
	T = 1 - T;
	L = 1 - L;
	R = 1 - R;
	B = 1 - B;
#endif

	float KRES = 100;
	T *= Response * KRES;
	L *= Response * KRES;
	R *= Response * KRES;
	B *= Response * KRES;

	float2 gradient = float2(
		2 * L - 2 * R,
		2 * T - 2 * B);
	float gradientMagnitude = (gradient.x * gradient.x + gradient.y * gradient.y);

	Edge = gradientMagnitude;
}

void EdgeDetectDepthRawFast_half(
	half T, half L, half R, half B,
	half Response,
	out half Edge)
{
#if UNITY_REVERSE_Z
	T = 1 - T;
	L = 1 - L;
	R = 1 - R;
	B = 1 - B;
#endif

	half KRES = 100;
	T *= Response * KRES;
	L *= Response * KRES;
	R *= Response * KRES;
	B *= Response * KRES;

	half2 gradient = half2(
		2 * L - 2 * R,
		2 * T - 2 * B);
	half gradientMagnitude = (gradient.x * gradient.x + gradient.y * gradient.y);

	Edge = gradientMagnitude;
}


void EdgeDetectWorldNormalFast_float(
	float3 T, float3 L, float3 R, float3 B,
	out float Edge)
{
	T = T * 0.5 + 0.5;
	L = L * 0.5 + 0.5;
	R = R * 0.5 + 0.5;
	B = B * 0.5 + 0.5;

	float2 gR = float2(
		2 * L.r - 2 * R.r,
		2 * T.r - 2 * B.r);
	float gmR = (gR.x * gR.x + gR.y * gR.y);

	float2 gG = float2(
		2 * L.g - 2 * R.g,
		2 * T.g - 2 * B.g);
	float gmG = (gG.x * gG.x + gG.y * gG.y);

	float2 gB = float2(
		2 * L.b - 2 * R.b,
		2 * T.b - 2 * B.b);
	float gmB = (gB.x * gB.x + gB.y * gB.y);

	Edge = (gmR + gmG + gmB) * 0.333;
}

void EdgeDetectWorldNormalFast_half(
	half3 T, half3 L, half3 R, half3 B,
	out half Edge)
{
	T = T * 0.5 + 0.5;
	L = L * 0.5 + 0.5;
	R = R * 0.5 + 0.5;
	B = B * 0.5 + 0.5;

	half2 gR = half2(
		2 * L.r - 2 * R.r,
		2 * T.r - 2 * B.r);
	half gmR = (gR.x * gR.x + gR.y * gR.y);

	half2 gG = half2(
		2 * L.g - 2 * R.g,
		2 * T.g - 2 * B.g);
	half gmG = (gG.x * gG.x + gG.y * gG.y);

	half2 gB = half2(
		2 * L.b - 2 * R.b,
		2 * T.b - 2 * B.b);
	half gmB = (gB.x * gB.x + gB.y * gB.y);

	Edge = (gmR + gmG + gmB) * 0.333;
}


void EdgeDetectColorFast_float(
	float4 T, float4 L, float4 R, float4 B,
	float PeakBrightness,
	out float Edge)
{
	float4 zero = float4(0, 0, 0, 0);
	float4 peak = float4(PeakBrightness.xxxx);

	T = clamp(T, zero, peak);
	L = clamp(L, zero, peak);
	R = clamp(R, zero, peak);
	B = clamp(B, zero, peak);

	float2 gR = float2(
		2 * L.r - 2 * R.r,
		2 * T.r - 2 * B.r);
	float gmR = (gR.x * gR.x + gR.y * gR.y);

	float2 gG = float2(
		2 * L.g - 2 * R.g,
		2 * T.g - 2 * B.g);
	float gmG = (gG.x * gG.x + gG.y * gG.y);

	float2 gB = float2(
		2 * L.b - 2 * R.b,
		2 * T.b - 2 * B.b);
	float gmB = (gB.x * gB.x + gB.y * gB.y);

	Edge = (gmR + gmG + gmB) * 0.333;
}

void EdgeDetectColorFast_half(
	half4 T, half4 L, half4 R, half4 B,
	half PeakBrightness,
	out half Edge)
{
	half4 zero = half4(0, 0, 0, 0);
	half4 peak = half4(PeakBrightness.xxxx);

	T = clamp(T, zero, peak);
	L = clamp(L, zero, peak);
	R = clamp(R, zero, peak);
	B = clamp(B, zero, peak);

	half2 gR = half2(
		2 * L.r - 2 * R.r,
		2 * T.r - 2 * B.r);
	half gmR = (gR.x * gR.x + gR.y * gR.y);

	half2 gG = half2(
		2 * L.g - 2 * R.g,
		2 * T.g - 2 * B.g);
	half gmG = (gG.x * gG.x + gG.y * gG.y);

	half2 gB = half2(
		2 * L.b - 2 * R.b,
		2 * T.b - 2 * B.b);
	half gmB = (gB.x * gB.x + gB.y * gB.y);

	Edge = (gmR + gmG + gmB) * 0.333;
}
