float EdgeEnhancement_InverseLerp(float v, float a, float b)
{
	return (v - a) / (b - a);
}

void EdgeEnhancement_float(float In, float WeakEdgeRemoval, float StrongEdgeHighlight, float EdgeClarity, out float Out)
{
	float inLow = WeakEdgeRemoval * WeakEdgeRemoval;
	float inHigh = 1 - StrongEdgeHighlight * StrongEdgeHighlight;
	float fClarity = lerp(0.01, 0.99, 1 - EdgeClarity);
	float inMid = lerp(inLow, inHigh, fClarity);

	float v0 = EdgeEnhancement_InverseLerp(In, inLow, inMid * 2 - inLow);
	float v1 = EdgeEnhancement_InverseLerp(In, inMid * 2 - inHigh, inHigh);

	Out = v0 * (In <= 0.5) + v1 * (In > 0.5);
}

void RemoveEdgeAtScreenBorder_float(float In, float2 ScreenPosition01, float ScreenWidth, float ScreenHeight, out float Out)
{
	float tX = 1 / ScreenWidth;
	float tY = 1 / ScreenHeight;
	float clipX = (ScreenPosition01.x > 0) * (ScreenPosition01.x < 1 - tX);
	float clipY = (ScreenPosition01.y > 0) * (ScreenPosition01.y < 1 - tY);

	Out = In * clipX * clipY;
}

void RemoveEdgeAtScreenBorder_half(half In, half2 ScreenPosition01, half ScreenWidth, half ScreenHeight, out half Out)
{
	half tX = 1 / ScreenWidth;
	half tY = 1 / ScreenHeight;
	half clipX = (ScreenPosition01.x > 0) * (ScreenPosition01.x < 1 - tX);
	half clipY = (ScreenPosition01.y > 0) * (ScreenPosition01.y < 1 - tY);

	Out = In * clipX * clipY;
}