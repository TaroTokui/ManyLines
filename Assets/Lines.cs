using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class Lines : MonoBehaviour
{
    // --------------------------------------------------
    #region // Defines
        
    const int MeshVertices = 10;

    struct LineData
    {
        public bool Active;
        public Vector3 BasePosition;
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector3 Color;
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

    /// GPU Instancingの為の引数
    uint[] _GPUInstancingArgs = new uint[5] { 0, 0, 0, 0, 0 };

    /// GPU Instancingの為の引数バッファ
    ComputeBuffer _GPUInstancingArgsBuffer;

    // point for particle
    Mesh _lineMesh;

    #endregion // Private Fields

    #region Compute configurations

    const int kThreadCount = 256;
    int ThreadGroupCount { get { return _instanceCount / kThreadCount; } }
    int InstanceCount { get { return kThreadCount * ThreadGroupCount; } }
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
            _computeShader.SetInt("_InstanceCount", InstanceCount);
            _computeShader.SetInt("_MeshVertices", MeshVertices);
            _computeShader.SetBuffer(kernel, "_LineDataBuffer", _lineDataBuffer);
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
        _lineDataBuffer = new ComputeBuffer(totalVertices, Marshal.SizeOf(typeof(LineData)));
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
        for (var i = 0; i < MeshVertices; i++)
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
        var totalVertices = _instanceCount * MeshVertices;

        // ComputeShader
        int kernelId = _computeShader.FindKernel("Update");
        _computeShader.SetFloat("_time", Time.time / 5.0f);
        _computeShader.SetInt("_instanceCount", _instanceCount);
        _computeShader.SetInt("_meshVertices", MeshVertices);
        _computeShader.SetBuffer(kernelId, "_lineDataBuffer", _lineDataBuffer);
        _computeShader.Dispatch(kernelId, ThreadGroupCount, 1, 1);

        var particleDataArr = new LineData[totalVertices];
        _lineDataBuffer.GetData(particleDataArr);

        // GPU Instaicing
        _GPUInstancingArgs[0] = (_lineMesh != null) ? _lineMesh.GetIndexCount(0) : 0;
        _GPUInstancingArgs[1] = (uint)_instanceCount;
        _GPUInstancingArgsBuffer.SetData(_GPUInstancingArgs);
        _material.SetBuffer("_lineDataBuffer", _lineDataBuffer);
        //_material.SetVector("_MeshScale", _MeshScale);
        _material.SetInt("_instanceCount", _instanceCount);
        _material.SetInt("_meshVertices", MeshVertices);
        Graphics.DrawMeshInstancedIndirect(_lineMesh, 0, _material, new Bounds(_boundCenter, _boundSize), _GPUInstancingArgsBuffer);
    }

    void OnDestroy()
    {
        if (_lineDataBuffer != null)
        {
            _lineDataBuffer.Release();
            _lineDataBuffer = null;
        }
        if (_GPUInstancingArgsBuffer != null)
        {
            _GPUInstancingArgsBuffer.Release();
            _GPUInstancingArgsBuffer = null;
        }
    }

    #endregion // MonoBehaviour Method
}
