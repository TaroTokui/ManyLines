using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class Lines : MonoBehaviour
{
    // --------------------------------------------------
    #region // Defines
        
    const int MeshVertices = 100;

    struct LineData
    {
        public bool Active;
        //public Vector4 BasePosition;
        //public Vector4 Position;
        //public Vector4 Velocity;
        //public Vector4 Normal;
        //public Vector4 Tangent;
        public Vector3 Albedo;
        public float Length;
        public float Time;
        public float LifeTime;
    }
    
    #endregion // Defines

    // --------------------------------------------------
    #region // Serialize Fields

    [SerializeField]
    int _instanceCount = 100;

    [SerializeField]
    float _lifeTime = 5;

    [SerializeField]
    Vector3 _airFlow = new Vector3(0 , 0, 0);

    [SerializeField]
    ComputeShader _computeShader;

    [SerializeField]
    Material _material;

    //[SerializeField]
    //Vector3 _MeshScale = new Vector3(1f, 1f, 1f);

    /// 表示領域の中心座標
    [SerializeField]
    Vector3 _boundCenter = Vector3.zero;

    /// 表示領域のサイズ
    [SerializeField]
    Vector3 _boundSize = new Vector3(1000f, 1000f, 1000f);

    #endregion // Serialize Fields

    // --------------------------------------------------
    #region // Private Fields

    ComputeBuffer _lineDataBuffer;
    ComputeBuffer _positionBuffer;
    ComputeBuffer _velocityBuffer;
    ComputeBuffer _tangentBuffer;
    ComputeBuffer _normalBuffer;

    /// GPU Instancingの為の引数
    uint[] _GPUInstancingArgs = new uint[5] { 0, 0, 0, 0, 0 };

    /// GPU Instancingの為の引数バッファ
    ComputeBuffer _GPUInstancingArgsBuffer;

    // point for particle
    Mesh _lineMesh;

    #endregion // Private Fields

    #region Compute configurations

    const int kThreadCount = 256;
    int ThreadGroupCount { get { return _instanceCount / kThreadCount + 1; } }
    //int InstanceCount { get { return kThreadCount * ThreadGroupCount; } }
    //int HistoryLength { get { return _template.segments + 1; } }

    #endregion

    // --------------------------------------------------
    #region Public Methods

    public void ResetPositions()
    {
        if (_lineDataBuffer != null)
        {
            // Invoke the initialization kernel.
            var kernel = _computeShader.FindKernel("Init");
            _computeShader.SetInt("InstanceCount", _instanceCount);
            _computeShader.SetInt("MeshVertices", MeshVertices);
            _computeShader.SetFloat("LifeTime", _lifeTime);
            _computeShader.SetBuffer(kernel, "LineDataBuffer", _lineDataBuffer);
            _computeShader.SetBuffer(kernel, "PositionBuffer", _positionBuffer);
            _computeShader.SetBuffer(kernel, "VelocityBuffer", _velocityBuffer);
            _computeShader.SetBuffer(kernel, "TangentBuffer", _tangentBuffer);
            _computeShader.SetBuffer(kernel, "NormalBuffer", _normalBuffer);
            _computeShader.Dispatch(kernel, ThreadGroupCount, 1, 1);
        }

        //var totalVertices = _instanceCount * MeshVertices;
        //var positionDataArr = new Vector4[totalVertices];
        //_positionBuffer.GetData(positionDataArr);

        //Debug.Log("----------");
        //for (int i = 0; i < totalVertices; i++)
        //{
        //    //Debug.Log(particleDataArr[i].Active);
        //    //Debug.Log(particleDataArr[i].BasePosition);
        //    Debug.Log(positionDataArr[i]);
        //    //Debug.Log(particleDataArr[i].Velocity);
        //    //Debug.Log(particleDataArr[i].Normal);
        //    //Debug.Log(particleDataArr[i].Tangent);
        //    //Debug.Log(particleDataArr[i].Albedo);
        //    //Debug.Log(particleDataArr[i].Length);
        //    //Debug.Log(particleDataArr[i].Time);
        //    //Debug.Log(particleDataArr[i].LifeTime);
        //}

    }

    public void EmitParticle()
    {
        //var lineDataArr = new LineData[_instanceCount];
        //_lineDataBuffer.GetData(lineDataArr);

        //for (int i = 0; i < _instanceCount; i++)
        //{
        //    if (lineDataArr[i].Active)
        //}

        if (_lineDataBuffer != null)
        {
            // Invoke the initialization kernel.
            var kernel = _computeShader.FindKernel("Emit");
            _computeShader.SetInt("InstanceCount", _instanceCount);
            _computeShader.SetInt("MeshVertices", MeshVertices);
            _computeShader.SetFloat("LifeTime", _lifeTime);
            _computeShader.SetBuffer(kernel, "LineDataBuffer", _lineDataBuffer);
            _computeShader.SetBuffer(kernel, "PositionBuffer", _positionBuffer);
            _computeShader.SetBuffer(kernel, "VelocityBuffer", _velocityBuffer);
            _computeShader.SetBuffer(kernel, "TangentBuffer", _tangentBuffer);
            _computeShader.SetBuffer(kernel, "NormalBuffer", _normalBuffer);
            _computeShader.Dispatch(kernel, ThreadGroupCount, 1, 1);
        }
    }

    #endregion

    // --------------------------------------------------
    #region // MonoBehaviour Methods

    void Awake()
    {
        Application.targetFrameRate = 90;
        QualitySettings.vSyncCount = 0;
    }

    void Start()
    {
        // バッファ生成
        var totalVertices = _instanceCount * MeshVertices;
        _lineDataBuffer = new ComputeBuffer(_instanceCount, Marshal.SizeOf(typeof(LineData)));
        _positionBuffer = new ComputeBuffer(totalVertices, 16);
        _velocityBuffer = new ComputeBuffer(totalVertices, 16);
        _tangentBuffer = new ComputeBuffer(totalVertices, 16);
        _normalBuffer = new ComputeBuffer(totalVertices, 16);
        _GPUInstancingArgsBuffer = new ComputeBuffer(1, _GPUInstancingArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        ResetPositions();

        // creat point mesh
        _lineMesh = new Mesh();

        // creat vertices
        var vertices = new List<Vector3>();
        for (var i = 0; i < MeshVertices; i++)
        {
            vertices.Add(new Vector3(i, 0, 0));
        }

        // set indices
        var indices = new List<int>();
        for (var i = 0; i < MeshVertices - 1; i++)
        {
            indices.Add(i);
            indices.Add(i + 1);
        }

        _lineMesh.Clear();
        _lineMesh.SetVertices(vertices);
        _lineMesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
        _lineMesh.UploadMeshData(true);
        
    }

    void Update()
    {

        if (Input.GetMouseButtonDown(0))
        {
            EmitParticle();
            //Debug.Log("click");
        }

        var totalVertices = _instanceCount * MeshVertices;

        // Invoke the initialization kernel.
        var kernel = _computeShader.FindKernel("Update");
        _computeShader.SetInt("InstanceCount", _instanceCount);
        _computeShader.SetInt("MeshVertices", MeshVertices);
        _computeShader.SetFloat("DeltaTime", Time.deltaTime / 0.5f);
        //_computeShader.SetFloat("LifeTime", _lifeTime);
        //_computeShader.SetVector("AirFlow", Vector4(_airFlow.x, _airFlow.y, _airFlow.z, 0));
        _computeShader.SetVector("AirFlow", _airFlow);
        _computeShader.SetBuffer(kernel, "LineDataBuffer", _lineDataBuffer);
        _computeShader.SetBuffer(kernel, "PositionBuffer", _positionBuffer);
        _computeShader.SetBuffer(kernel, "VelocityBuffer", _velocityBuffer);
        _computeShader.SetBuffer(kernel, "TangentBuffer", _tangentBuffer);
        _computeShader.SetBuffer(kernel, "NormalBuffer", _normalBuffer);
        _computeShader.Dispatch(kernel, ThreadGroupCount, 1, 1);

        // ComputeShader
        //int kernel = _computeShader.FindKernel("Update");
        //_computeShader.SetFloat("_time", Time.time / 1.0f);
        //_computeShader.SetInt("_instanceCount", _instanceCount);
        //_computeShader.SetInt("_meshVertices", MeshVertices);
        //_computeShader.SetBuffer(kernel, "LineDataBuffer", _lineDataBuffer);
        //_computeShader.Dispatch(kernel, ThreadGroupCount, 1, 1);

        //var particleDataArr = new LineData[totalVertices];
        //_lineDataBuffer.GetData(particleDataArr);

        //Debug.Log("----------");
        //for (int i=0; i< totalVertices; i++)
        //{
        //    Debug.Log(particleDataArr[i].Active);
        //    Debug.Log(particleDataArr[i].BasePosition);
        //    Debug.Log(particleDataArr[i].Position);
        //    Debug.Log(particleDataArr[i].Velocity);
        //    Debug.Log(particleDataArr[i].Normal);
        //    Debug.Log(particleDataArr[i].Tangent);
        //    Debug.Log(particleDataArr[i].Albedo);
        //    Debug.Log(particleDataArr[i].Length);
        //    Debug.Log(particleDataArr[i].Time);
        //    Debug.Log(particleDataArr[i].LifeTime);
        //}


        // GPU Instaicing
        _GPUInstancingArgs[0] = (_lineMesh != null) ? _lineMesh.GetIndexCount(0) : 0;
        _GPUInstancingArgs[1] = (uint)_instanceCount;
        _GPUInstancingArgsBuffer.SetData(_GPUInstancingArgs);
        //Debug.Log(_lineMesh.GetIndexCount(0));
        _material.SetBuffer("_LineDataBuffer", _lineDataBuffer);
        _material.SetBuffer("_PositionBuffer", _positionBuffer);
        _material.SetBuffer("_VelocityBuffer", _velocityBuffer);
        _material.SetBuffer("_TangentBuffer", _tangentBuffer);
        _material.SetBuffer("_NormalBuffer", _normalBuffer);
        //_material.SetVector("_MeshScale", _MeshScale);
        _material.SetInt("_InstanceCount", _instanceCount);
        //Debug.Log("instances: " + _instanceCount);
        _material.SetInt("_MeshVertices", MeshVertices);
        //Debug.Log("MeshVertices: " + MeshVertices);
        Graphics.DrawMeshInstancedIndirect(_lineMesh, 0, _material, new Bounds(_boundCenter, _boundSize), _GPUInstancingArgsBuffer);
    }
    
    void OnDestroy()
    {
        if (_lineDataBuffer != null)
        {
            _lineDataBuffer.Release();
            _lineDataBuffer = null;
        }
        if (_positionBuffer != null)
        {
            _positionBuffer.Release();
            _positionBuffer = null;
        }
        if (_velocityBuffer != null)
        {
            _velocityBuffer.Release();
            _velocityBuffer = null;
        }
        if (_tangentBuffer != null)
        {
            _tangentBuffer.Release();
            _tangentBuffer = null;
        }
        if (_normalBuffer != null)
        {
            _normalBuffer.Release();
            _normalBuffer = null;
        }
        if (_GPUInstancingArgsBuffer != null)
        {
            _GPUInstancingArgsBuffer.Release();
            _GPUInstancingArgsBuffer = null;
        }
        
    }

    #endregion // MonoBehaviour Method
}
