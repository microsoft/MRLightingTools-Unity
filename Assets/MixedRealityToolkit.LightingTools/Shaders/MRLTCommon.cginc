
float4 _CubePos;
float3 _CubeMin;
float3 _CubeMax;
float4 _CubeRot;

float3 BoxProjection(float3 direction, float3 position) {
	float3 boxPos = _CubePos.xyz;
	float2 boxRot = _CubeRot;
	float3 boxMin = _CubeMin;
	float3 boxMax = _CubeMax;

	position = position - boxPos;
	position.xz = float2(
		position.x * boxRot.x - position.z * boxRot.y,
		position.x * boxRot.y + position.z * boxRot.x);
	direction.xz = float2(
		direction.x * boxRot.x - direction.z * boxRot.y,
		direction.x * boxRot.y + direction.z * boxRot.x);
	//position = clamp(position, boxMin*.8, boxMax*.8); // don't let the position go outside the box, this leads to ugly!
	position = position + boxPos;

	float3 factors = ((direction > 0 ? boxMax : boxMin) - position) / direction;
	float  scalar  = min(min(factors.x, factors.y), factors.z);
	direction = direction * scalar + (position - boxPos);

	direction.xz = float2(
		direction.x *  boxRot.x + direction.z * boxRot.y,
		direction.x * -boxRot.y + direction.z * boxRot.x);
	return direction;
}