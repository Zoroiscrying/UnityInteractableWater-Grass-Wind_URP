using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// WaterManager from DreamFairy@Github
/// </summary>
public class WaterManager : MonoBehaviour
{
    public GameObject WaterPlane;
    public float WaterPlaneWidth;
    public float WaterPlaneLength;
    public float WaveRadius = 1.0f;
    public float WaveSpeed = 1.0f;
    public float WaveViscosity = 1.0f; //粘度
    public float WaveAtten = 0.99f; //衰减
    [Range(0, 0.999f)]
    public float WaveHeight = 0.999f;
    public int WaveTextureResolution = 512;
    
    private RenderTexture m_waterWaveMarkTexture;
    private RenderTexture m_waveTransmitTexture;
    private RenderTexture m_prevWaveMarkTexture;
    
    public UnityEngine.UI.RawImage WaveMarkDebugImg;
    public UnityEngine.UI.RawImage WaveTransmitDebugImg;
    public UnityEngine.UI.RawImage PrevWaveTransmitDebugImg;

    private Material m_waterWaveMarkMat;
    private Material m_waveTransmitMat;

    private Vector4 m_waveTransmitParams;
    private Vector4 m_waveMarkParams;

    private CommandBuffer m_cmd;

    // Start is called before the first frame update
    void Awake()
    {
        m_waterWaveMarkTexture = new RenderTexture(WaveTextureResolution, WaveTextureResolution, 0, RenderTextureFormat.Default);
        m_waterWaveMarkTexture.name = "m_waterWaveMarkTexture";
        
        m_waveTransmitTexture = new RenderTexture(WaveTextureResolution, WaveTextureResolution, 0, RenderTextureFormat.Default);
        m_waveTransmitTexture.name = "m_waveTransmitTexture";
        
        m_prevWaveMarkTexture = new RenderTexture(WaveTextureResolution, WaveTextureResolution, 0, RenderTextureFormat.Default);
        m_prevWaveMarkTexture.name = "m_prevWaveMarkTexture";

        m_waterWaveMarkMat = new Material(Shader.Find("Custom/WaveMarkerShader"));
        m_waveTransmitMat = new Material(Shader.Find("Custom/WaveTransmitShader"));

        Shader.SetGlobalTexture("_WaterWaveResult", m_waterWaveMarkTexture);
        Shader.SetGlobalFloat("_WaveHeight", WaveHeight);
        Shader.SetGlobalVector("_WaterSimulationParams",
            new Vector4(WaterPlane.transform.position.x, WaterPlane.transform.position.z, WaterPlaneWidth, WaterPlaneLength));

        WaveMarkDebugImg.texture = m_waterWaveMarkTexture;
        WaveTransmitDebugImg.texture = m_waveTransmitTexture;
        PrevWaveTransmitDebugImg.texture = m_prevWaveMarkTexture;
        
        InitWaveTransmitParams();
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    /// <summary>
    /// 公式中的除uv外的常数在外部计算，以提升Shader性能
    /// </summary>
    void InitWaveTransmitParams()
    {
        float uvStep = 1.0f / WaveTextureResolution;
        float dt = Time.fixedDeltaTime;
        //最大递进粘性
        float maxWaveStepVisosity = uvStep / (2 * dt) * (Mathf.Sqrt(WaveViscosity * dt + 2));
        //粘度平方 u^2
        float waveVisositySqr = WaveViscosity * WaveViscosity;
        //当前速度
        float curWaveSpeed = maxWaveStepVisosity * WaveSpeed;
        //速度平方 c^2
        float curWaveSpeedSqr = curWaveSpeed * curWaveSpeed;
        //波单次位移平方 d^2
        float uvStepSqr = uvStep * uvStep;

        float i = Mathf.Sqrt(waveVisositySqr + 32 * curWaveSpeedSqr / uvStepSqr);
        float j = 8 * curWaveSpeedSqr / uvStepSqr;

        //波传递公式
        // (4 - 8 * c^2 * t^2 / d^2) / (u * t + 2) + (u * t - 2) / (u * t + 2) * z(x,y,z, t - dt) + (2 * c^2 * t^2 / d ^2) / (u * t + 2)
        // * (z(x + dx,y,t) + z(x - dx, y, t) + z(x,y + dy, t) + z(x, y - dy, t);

        //ut
        float ut = WaveViscosity * dt;
        //c^2 * t^2 / d^2
        float ctdSqr = curWaveSpeedSqr * dt * dt / uvStepSqr;
        // ut + 2
        float utp2 = ut + 2;
        // ut - 2
        float utm2 = ut - 2;
        //(4 - 8 * c^2 * t^2 / d^2) / (u * t + 2) 
        float p1 = (4 - 8 * ctdSqr) / utp2;
        //(u * t - 2) / (u * t + 2)
        float p2 = utm2 / utp2;
        //(2 * c^2 * t^2 / d ^2) / (u * t + 2)
        float p3 = (2 * ctdSqr) / utp2;

        m_waveTransmitParams.Set(p1, p2, p3, uvStep);

        //Debug.LogFormat("i {0} j {1} maxSpeed {2}", i, j, maxWaveStepVisosity);
        //Debug.LogFormat("p1 {0} p2 {1} p3 {2}", p1, p2, p3);
    }

    private int counter = 0;

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (counter % 4 == 0)
        {
            //如果产生碰撞，标记碰撞位置，设置碰撞变量为True
            WaterPlaneCollider();
            //
            WaterMark();
            //
            WaveTransmit();   
        }

        counter++;
    }
    
    Vector2 hitPos = Vector2.zero;
    bool hasHit = false;
    
    /// <summary>
    /// 检测从Camera发出的射线，将射线碰撞点的齐次世界坐标转化为Plane本地的Object坐标，再转化为方向坐标
    /// </summary>
    void WaterPlaneCollider()
    {
        hasHit = false;
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo = new RaycastHit();
            bool ret = Physics.Raycast(ray.origin, ray.direction, out hitInfo);
            if (ret)
            {
                //转化为本地坐标
                Vector3 waterPlaneSpacePos = WaterPlane.transform.worldToLocalMatrix * new Vector4(hitInfo.point.x, hitInfo.point.y, hitInfo.point.z, 1);
                
                //由于本地坐标的碰撞位置是基于plane的TransformPosition，而plane的TransformPosition实际上是位于模型中心位置
                //我们需要实际位置映射到0-1空间下，即以模型Object Space空间左下角位置为(0,0)
                //由此考虑，我们需要在当前的本地坐标除以模型长和宽后再加上0.5的偏移使坐标从(-0.5,0.5)取值范围转化到(0,1)取值范围
                float dx = (waterPlaneSpacePos.x / WaterPlaneWidth);
                float dy = (waterPlaneSpacePos.z / WaterPlaneLength); 
                //Debug.Log(dx + "," + dy);

                //设置碰撞位置(UV)
                hitPos.Set(dx, dy);
                //设置dx，dy，碰撞水花半径，水花高度
                m_waveMarkParams.Set(dx, dy, WaveRadius * WaveRadius, WaveHeight);
                //标记产生了碰撞
                hasHit = true;
            }
        }
    }

    /// <summary>
    /// 如果碰撞，将具体的碰撞参数传入，利用WaterMark材质处理，将上一帧的传递结果结合shader进行结合处理
    /// 最后传入waterWaveMarkTexture
    /// </summary>
    void WaterMark()
    {
        if (hasHit)
        {
            m_waterWaveMarkMat.SetVector("_WaveMarkParams", m_waveMarkParams);
            Graphics.Blit(m_waveTransmitTexture, m_waterWaveMarkTexture, m_waterWaveMarkMat);
        }
    }

    /// <summary>
    /// 首先传入水波传递相关的参数和上一帧的水面贴图
    /// 接着创建临时的RT，先利用TransmitShader将这一帧的水面标记贴图传入RT中
    /// 此时的RT中储存的就是这一帧碰撞和处理水面传递后的结果
    /// 然后将这一帧未经过传递处理的水面贴图传入prevWaterMarkTexture
    /// 将RT传入waterWaveMarkTexture 和 waveTransmitTexture，更新水面波情况
    /// </summary>
    void WaveTransmit()
    {
        m_waveTransmitMat.SetVector("_WaveTransmitParams", m_waveTransmitParams);
        m_waveTransmitMat.SetFloat("_WaveAtten", WaveAtten);
        m_waveTransmitMat.SetTexture("_PrevWaveMarkTex", m_prevWaveMarkTexture);

        RenderTexture rt = RenderTexture.GetTemporary(WaveTextureResolution, WaveTextureResolution);
        Graphics.Blit(m_waterWaveMarkTexture, rt, m_waveTransmitMat);
        Graphics.Blit(m_waterWaveMarkTexture, m_prevWaveMarkTexture);
        Graphics.Blit(rt, m_waterWaveMarkTexture);
        Graphics.Blit(rt, m_waveTransmitTexture);
        RenderTexture.ReleaseTemporary(rt);
    }
}
