void CustomMainLight_Half(out half3 Direction, out half3 Color)
{
    #if SHADER_GRAPH_PREVIEW
        Direction = half3(0.5, 0.5, 0);
        Color = 1;
    #else
        Light light = GetMainLight();
        Direction = light.direction;
        Color = light.color;
    #endif
}