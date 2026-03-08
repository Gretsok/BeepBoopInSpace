void ComputeWorldSpacePosition_float(float2 positionNDC, float deviceDepth, out float3 worldPosition)
{
	worldPosition = ComputeWorldSpacePosition(positionNDC, deviceDepth, UNITY_MATRIX_I_VP);
}

void ComputeWorldSpacePosition_half(half2 positionNDC, half deviceDepth, out half3 worldPosition)
{
	worldPosition = ComputeWorldSpacePosition(positionNDC, deviceDepth, UNITY_MATRIX_I_VP);
}