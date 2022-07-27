using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace URPShaderTestBed.Water.Scripts
{
    [ExecuteAlways]
    public class WaterSimulationManager : MonoBehaviour
    {
        [Header("Water Rendering Settings")] public static Transform WaterSimTarget = null;
        private Vector3 _simTargetPositionLastFrame = Vector3.zero;
        private Vector3 _deltaSimTargetPosition = Vector3.zero;
        private Texture2D _rampTexture;
        private static readonly int AbsorptionScatteringRamp = Shader.PropertyToID("_AbsorptionScatteringRamp");
        public Gradient ScatteringGradient;
        public Gradient AbsorptionGradient;    
        private static readonly int MaxDepth = Shader.PropertyToID("_MaxDepth");
        public float WaterMaxVisibility = 10.0f;

        [Header("Water Wave Simulation")] 
        //[SerializeField] private Transform targetToFollow;
        public float SimulationLengthX;
        public float SimulationLengthZ;
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
        
        //public UnityEngine.UI.RawImage WaveMarkDebugImg;
        //public UnityEngine.UI.RawImage WaveTransmitDebugImg;
        //public UnityEngine.UI.RawImage PrevWaveTransmitDebugImg;

        private Material m_waterWaveMarkMat;
        private Material m_waveTransmitMat;

        private Vector4 m_waveTransmitParams;
        private Vector4 m_waveMarkParams;

        private CommandBuffer m_cmd;

        private void Start()
        {
            //Init();
        }

        private void OnEnable()
        {
            Init();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
            CleanUp();
        }

        private void OnValidate()
        {
            Init();
        }

        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitInfo = new RaycastHit();
                bool ret = Physics.Raycast(ray.origin, ray.direction, out hitInfo);
                if (ret)
                {
                    RegisterCollision(hitInfo.point);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 squareCenter = this.transform.position;
            Vector3 upLeft    = squareCenter + new Vector3(-SimulationLengthX/2f, 0f, SimulationLengthZ/2);
            Vector3 upRight   = squareCenter + new Vector3(SimulationLengthX/2f, 0f, SimulationLengthZ/2);
            Vector3 downLeft  = squareCenter + new Vector3(-SimulationLengthX/2f, 0f, -SimulationLengthZ/2);
            Vector3 downRight = squareCenter + new Vector3(SimulationLengthX/2f, 0f, -SimulationLengthZ/2);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(upLeft, upRight);
            Gizmos.DrawLine(upRight, downRight);
            Gizmos.DrawLine(downRight, downLeft);
            Gizmos.DrawLine(downLeft, upLeft);
        }

        public void Init()
        {
            //Water Wave Simulation
            m_waterWaveMarkTexture = new RenderTexture(WaveTextureResolution, WaveTextureResolution, 0, RenderTextureFormat.Default);
            m_waterWaveMarkTexture.name = "m_waterWaveMarkTexture";
            
            m_waveTransmitTexture = new RenderTexture(WaveTextureResolution, WaveTextureResolution, 0, RenderTextureFormat.Default);
            m_waveTransmitTexture.name = "m_waveTransmitTexture";
            
            m_prevWaveMarkTexture = new RenderTexture(WaveTextureResolution, WaveTextureResolution, 0, RenderTextureFormat.Default);
            m_prevWaveMarkTexture.name = "m_prevWaveMarkTexture";

            m_waterWaveMarkMat = new Material(Shader.Find("Custom/WaveMarkerShader"));
            m_waveTransmitMat = new Material(Shader.Find("Custom/WaveTransmitShader"));
            
            //WaveMarkDebugImg.texture = m_waterWaveMarkTexture;
            //WaveTransmitDebugImg.texture = m_waveTransmitTexture;
            //PrevWaveTransmitDebugImg.texture = m_prevWaveMarkTexture;

            Shader.SetGlobalTexture("_WaterWaveResult", m_waterWaveMarkTexture);
            Shader.SetGlobalFloat("_WaveHeight", WaveHeight);
            if (WaterSimulationManager.WaterSimTarget)
            {
                Shader.SetGlobalVector("_WaterSimulationParams",
                    new Vector4(WaterSimTarget.position.x, WaterSimTarget.position.z, SimulationLengthX, SimulationLengthZ));
                _simTargetPositionLastFrame = WaterSimTarget.position;
            }
            else
            {
                Shader.SetGlobalVector("_WaterSimulationParams",
                    new Vector4(transform.position.x, transform.position.z, SimulationLengthX, SimulationLengthZ));
            }

            InitWaveTransmitParams();
            
            //Water Rendering Setups
            GenerateColorRamp();
            Shader.SetGlobalFloat(MaxDepth, WaterMaxVisibility);
            
        }

        private void CleanUp()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        #region Water Wave Simulation

                public void RegisterCollision(Vector3 position)
                {
                    Vector3 simulationSpacePosition = Vector3.zero;
                    //转化为本地坐标
                    if (WaterSimulationManager.WaterSimTarget)
                    {
                        simulationSpacePosition = WaterSimulationManager.WaterSimTarget.worldToLocalMatrix * new Vector4(position.x, position.y, position.z, 1);
                    }
                    else
                    {
                        simulationSpacePosition = transform.worldToLocalMatrix * new Vector4(position.x, position.y, position.z, 1);
                    }
                         
                    //由于本地坐标的碰撞位置是基于plane的TransformPosition，而plane的TransformPosition实际上是位于模型中心位置
                    //我们需要实际位置映射到0-1空间下，即以模型Object Space空间左下角位置为(0,0)
                    //由此考虑，我们需要在当前的本地坐标除以模型长和宽后再加上0.5的偏移使坐标从(-0.5,0.5)取值范围转化到(0,1)取值范围
                    float dx = (simulationSpacePosition.x / SimulationLengthX) + 0.5f;
                    float dy = (simulationSpacePosition.z / SimulationLengthZ) + 0.5f;
        
                    //设置碰撞位置(UV)
                    hitPos.Set(dx, dy);
                    //设置dx，dy，碰撞水花半径，水花高度
                    m_waveMarkParams.Set(dx, dy, WaveRadius * WaveRadius, WaveHeight);
                    //标记产生了碰撞
                    hasHit = true;
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
                    if (WaterSimTarget)
                    {
                        Shader.SetGlobalVector("_WaterSimulationParams",
                            new Vector4(WaterSimTarget.position.x, WaterSimTarget.position.z, SimulationLengthX, SimulationLengthZ));
                        // calculate delta position
                        var position1 = WaterSimTarget.position;
                        _deltaSimTargetPosition = position1 - _simTargetPositionLastFrame;
                        _simTargetPositionLastFrame = position1;
                        Shader.SetGlobalVector("_DeltaTargetPositionUV", 
                            new Vector4(_deltaSimTargetPosition.x / SimulationLengthX, _deltaSimTargetPosition.z / SimulationLengthZ));
                        Debug.Log(_deltaSimTargetPosition);
                    }
                    else
                    {
                        Shader.SetGlobalVector("_WaterSimulationParams",
                            new Vector4(transform.position.x, transform.position.z, SimulationLengthX, SimulationLengthZ));   
                    }

                    //如果产生碰撞，标记碰撞位置，设置碰撞变量为True
                    //WaterPlaneCollider();
                    WaterMark();
                    //slower transmission
                    //if (counter % 4 == 0)
                    //{
                    WaveTransmit();   
                    //}
        
                    counter++;
                    hitLastFrame = hasHit;
                    hasHit = false;
                }
                
                Vector2 hitPos = Vector2.zero;
                private bool hasHit = false;
                private bool hitLastFrame = false;
                
                /// <summary>
                /// 检测从Camera发出的射线，将射线碰撞点的齐次世界坐标转化为Plane本地的Object坐标，再转化为方向坐标
                /// </summary>
                void WaterPlaneCollider()
                {
                    
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

        #endregion

        #region Water Rendering Setups

        private void GenerateColorRamp()
        {
            if(_rampTexture == null)
                _rampTexture = new Texture2D(128, 2, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
            _rampTexture.wrapMode = TextureWrapMode.Clamp;
            
            var cols = new Color[256];
            for (var i = 0; i < 128; i++)
            {
                cols[i] = AbsorptionGradient.Evaluate(i / 128f);
            }
            for (var i = 0; i < 128; i++)
            {
                cols[i + 128] = ScatteringGradient.Evaluate(i / 128f);
            }
            
            _rampTexture.SetPixels(cols);
            _rampTexture.Apply();
            Shader.SetGlobalTexture(AbsorptionScatteringRamp, _rampTexture);
        }

        #endregion
        
        
    }
}