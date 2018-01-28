using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class Ribbon : MonoBehaviour {

    // --------------------------------------------------
    #region // Defines

    const int MeshVertices = 100;

    struct LineData
    {
        public bool Active;
        public Vector3 Albedo;
        public float Radius;
        public float Phi;
        public float Time;
        public float LifeTime;
    }

    #endregion // Defines

    // --------------------------------------------------
    #region // Serialize Fields

    [SerializeField]
    int _instanceCount = 100;

    [SerializeField]
    int _emitCount = 5;

    [SerializeField]
    private int _activeCount = 0;

    [SerializeField]
    float _lifeTime = 5;

    [SerializeField]
    Vector3 _airFlow = new Vector3(0, 0, 0);

    //[SerializeField, Range(0,1)]
    [SerializeField]
    bool _ShowAllLines = false;

    [SerializeField, Range(0, 1)]
    float _noiseSpread = 0.5f;

    [SerializeField, Range(0, 10)]
    float _radius = 5;

    [SerializeField, Range(0, 0.5f)]
    float _lineLength = 0.1f;

    [SerializeField, Range(-Mathf.PI, Mathf.PI)]
    float _phi = 0;

    [SerializeField, Range(0, 1)]
    float _meshWidth = 0.5f;

    [SerializeField]
    ComputeShader _computeShader;

    [SerializeField]
    Material _material;

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
    }

    public void EmitParticle()
    {
        if (_lineDataBuffer != null)
        {
            // Invoke the initialization kernel.
            var kernel = _computeShader.FindKernel("Emit");
            _computeShader.SetInt("InstanceCount", _instanceCount);
            _computeShader.SetInt("MeshVertices", MeshVertices);
            _computeShader.SetFloat("LifeTime", _lifeTime);
            _computeShader.SetInt("RandomSeed", (int)(Time.realtimeSinceStartup * 100));
            _computeShader.SetFloat("Radius", _radius);
            _computeShader.SetFloat("LineLength", _lineLength);
            _computeShader.SetFloat("Phi", _phi);
            _computeShader.SetInt("EmitIndex", _activeCount);
            _computeShader.SetInt("EmitCount", _emitCount);
            _computeShader.SetBuffer(kernel, "LineDataBuffer", _lineDataBuffer);
            _computeShader.SetBuffer(kernel, "PositionBuffer", _positionBuffer);
            _computeShader.SetBuffer(kernel, "VelocityBuffer", _velocityBuffer);
            _computeShader.SetBuffer(kernel, "TangentBuffer", _tangentBuffer);
            _computeShader.SetBuffer(kernel, "NormalBuffer", _normalBuffer);
            _computeShader.Dispatch(kernel, ThreadGroupCount, 1, 1);

            _activeCount += _emitCount;
            if (_activeCount >= _instanceCount) _activeCount = 0;
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
        for (var i = 0; i < MeshVertices / 2 - 1; i++)
        {
            indices.Add(i * 2);
            indices.Add(i * 2 + 1);
            indices.Add(i * 2 + 2);

            indices.Add(i * 2 + 1);
            indices.Add(i * 2 + 3);
            indices.Add(i * 2 + 2);
        }

        _lineMesh.Clear();
        _lineMesh.SetVertices(vertices);
        _lineMesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
        _lineMesh.UploadMeshData(true);

    }

    void Update()
    {
        //if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        //{
        //    EmitParticle();
        //}

        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space))
        {
            EmitParticle();
        }

        var totalVertices = _instanceCount * MeshVertices;

        // Invoke the initialization kernel.
        var kernel = _computeShader.FindKernel("Update");
        _computeShader.SetInt("InstanceCount", _instanceCount);
        _computeShader.SetInt("MeshVertices", MeshVertices);
        _computeShader.SetFloat("DeltaTime", Time.deltaTime / 0.5f);
        _computeShader.SetVector("AirFlow", _airFlow);
        _computeShader.SetFloat("NoiseSpread", _noiseSpread);
        _computeShader.SetFloat("Radius", _radius);
        _computeShader.SetFloat("LineLength", _lineLength);
        _computeShader.SetFloat("MeshWidth", _meshWidth);
        _computeShader.SetFloat("Phi", _phi);
        _computeShader.SetBuffer(kernel, "LineDataBuffer", _lineDataBuffer);
        _computeShader.SetBuffer(kernel, "PositionBuffer", _positionBuffer);
        _computeShader.SetBuffer(kernel, "VelocityBuffer", _velocityBuffer);
        _computeShader.SetBuffer(kernel, "TangentBuffer", _tangentBuffer);
        _computeShader.SetBuffer(kernel, "NormalBuffer", _normalBuffer);
        _computeShader.Dispatch(kernel, ThreadGroupCount, 1, 1);

        // GPU Instaicing
        _GPUInstancingArgs[0] = (_lineMesh != null) ? _lineMesh.GetIndexCount(0) : 0;
        _GPUInstancingArgs[1] = (uint)_instanceCount;
        _GPUInstancingArgsBuffer.SetData(_GPUInstancingArgs);
        _material.SetBuffer("_LineDataBuffer", _lineDataBuffer);
        _material.SetBuffer("_PositionBuffer", _positionBuffer);
        _material.SetBuffer("_VelocityBuffer", _velocityBuffer);
        _material.SetBuffer("_TangentBuffer", _tangentBuffer);
        _material.SetBuffer("_NormalBuffer", _normalBuffer);
        _material.SetInt("_InstanceCount", _instanceCount);
        _material.SetInt("_MeshVertices", MeshVertices);
        _material.SetInt("_ShowAllLines", _ShowAllLines ? 1 : 0);
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
