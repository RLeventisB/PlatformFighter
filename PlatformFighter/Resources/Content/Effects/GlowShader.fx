texture2D text;
sampler TextureSampler : register(s0) = sampler_state 
{
	 Texture = (text); 
};

bool isGlow;
float4x4 WorldViewProjection;

void MainVS(	inout float4 color    : COLOR0,
                inout float2 texCoord : TEXCOORD0,
                inout float4 position : SV_Position)
{
	position = mul(position, WorldViewProjection);
}

float4 MainPS(float2 pos : TEXCOORD0, float4 DesiredColor : COLOR0) : SV_Target0
{
	float4 color = tex2D(TextureSampler, pos);
	if(isGlow)
	{
		float luminosity = (color.r + color.g + color.b) / 3.0f;
		color.rgb = lerp(DesiredColor.rgb, color.rgb, color.a * luminosity);
		color.a *= DesiredColor.a;
	}
	else
	{
		color *= DesiredColor;
	}
	return color;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 MainPS();
	}
};