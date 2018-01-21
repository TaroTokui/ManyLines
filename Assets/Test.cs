using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class Test : MonoBehaviour {

    // ==============================
    #region // Defines

    //const int ThreadBlockSize = 256;
    const int MeshVertices = 4;
    
    struct LineData
    {
        public int Active;
        public Vector4 BasePosition;
        public Vector4 Position;
        public Vector4 Velocity;
        public Vector4 Normal;
        public Vector4 Tangent;
        public Vector3 Albedo;
        public float Length;
        public float Time;
        public float LifeTime;
    }

    #endregion // Defines

    // --------------------------------------------------
    #region // Serialize Fields

    [SerializeField]
    int _instanceCount = 500000;

    [SerializeField]
    [Range(-Mathf.PI, Mathf.PI)]
    float _phi = Mathf.PI;

    [SerializeField]
    ComputeShader _computeShader;

    [SerializeField]
    Material _material;

    [SerializeField]
    Vector3 _MeshScale = new Vector3(1f, 1f, 1f);

    /// 表示領域の中心座標
    [SerializeField]
    Vector3 _BoundCenter = Vector3.zero;

    /// 表示領域のサイズ
    [SerializeField]
    Vector3 _BoundSize = new Vector3(300f, 300f, 300f);

    #endregion // Serialize Fields

    // --------------------------------------------------
    #region // Private Fields

    ComputeBuffer _LineDataBuffer;

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
    #region // MonoBehaviour Methods

    void Awake()
    {
        Application.targetFrameRate = 90;
        QualitySettings.vSyncCount = 0;
    }

    // --------------------------------------------------
    #region Public Methods

    public void ResetPositions()
    {
        if (_LineDataBuffer != null)
        {
            // Invoke the initialization kernel.
            var kernel = _computeShader.FindKernel("Init");
            _computeShader.SetInt("InstanceCount", InstanceCount);
            _computeShader.SetInt("MeshVertices", MeshVertices);
            //_computeShader.SetFloat("lifeTime", _lifeTime);
            _computeShader.SetBuffer(kernel, "LineDataBuffer", _LineDataBuffer);
            _computeShader.Dispatch(kernel, ThreadGroupCount, 1, 1);
        }

        var totalVertices = _instanceCount * MeshVertices;
        var particleDataArr = new LineData[totalVertices];
        _LineDataBuffer.GetData(particleDataArr);

        Debug.Log("----------");
        for (int i = 0; i < totalVertices; i++)
        {
            Debug.Log(particleDataArr[i].Active);
            Debug.Log(particleDataArr[i].BasePosition);
            Debug.Log(particleDataArr[i].Position);
            Debug.Log(particleDataArr[i].Velocity);
            Debug.Log(particleDataArr[i].Normal);
            Debug.Log(particleDataArr[i].Tangent);
            Debug.Log(particleDataArr[i].Albedo);
            Debug.Log(particleDataArr[i].Length);
            Debug.Log(particleDataArr[i].Time);
            Debug.Log(particleDataArr[i].LifeTime);
        }

    }

    #endregion

    void Start()
    {
        // バッファ生成
        var totalCount = this._instanceCount * MeshVertices;
        this._LineDataBuffer = new ComputeBuffer(totalCount, Marshal.SizeOf(typeof(LineData)));
        this._GPUInstancingArgsBuffer = new ComputeBuffer(1, this._GPUInstancingArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        //var particleDataArr = new LineData[totalCount];

        ResetPositions();

        //// set default position
        //for (int i = 0; i < totalCount; i++)
        //{
        //    particleDataArr[i].BasePosition = new Vector3(Random.Range(-10.0f, 10.0f), Random.Range(-10.0f, 10.0f), Random.Range(-10.0f, 10.0f));
        //    particleDataArr[i].Albedo = new Vector3(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
        //    //particleDataArr[i].rotationSpeed = Random.Range(1.0f, 100.0f);
        //}
        //this._LineDataBuffer.SetData(particleDataArr);
        //particleDataArr = null;

        //// creat point mesh
        //_pointMesh = new Mesh();
        //_pointMesh.vertices = new Vector3[] {
        //    new Vector3 (0, 0, 0),
        //    new Vector3 (1, 0, 0),
        //};
        //_pointMesh.normals = new Vector3[] {
        //    new Vector3 (0, 1, 0),
        //    new Vector3 (0, 1, 0),
        //};
        //_pointMesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, 0);

        // creat line mesh
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
        var totalCount = this._instanceCount * MeshVertices;

        // ComputeShader
        int kernelId = this._computeShader.FindKernel("Update");
        this._computeShader.SetFloat("time", Time.time / 5.0f);
        this._computeShader.SetInt("InstanceCount", _instanceCount);
        this._computeShader.SetInt("MeshVertices", MeshVertices);
        this._computeShader.SetBuffer(kernelId, "LineDataBuffer", this._LineDataBuffer);
        this._computeShader.Dispatch(kernelId, (Mathf.CeilToInt(totalCount / kThreadCount) + 1), 1, 1);

        var particleDataArr = new LineData[totalCount];
        _LineDataBuffer.GetData(particleDataArr);

        //Debug.Log("-------------");
        //for (int i = 0; i < _instanceCount; i++)
        //{
        //    Debug.Log(i.ToString() + " -");
        //    //Debug.Log(particleDataArr[i * 2].BasePosition);
        //    //Debug.Log(particleDataArr[i * 2 + 1].BasePosition);
        //    Debug.Log(particleDataArr[i * 2].Position);
        //    Debug.Log(particleDataArr[i * 2 + 1].Position);
        //}

        // GPU Instaicing
        this._GPUInstancingArgs[0] = (this._lineMesh != null) ? this._lineMesh.GetIndexCount(0) : 0;
        this._GPUInstancingArgs[1] = (uint)this._instanceCount;
        this._GPUInstancingArgsBuffer.SetData(this._GPUInstancingArgs);
        this._material.SetBuffer("_LineDataBuffer", this._LineDataBuffer);
        this._material.SetVector("_MeshScale", this._MeshScale);
        this._material.SetInt("_InstanceCount", _instanceCount);
        this._material.SetInt("_MeshVertices", MeshVertices);
        Graphics.DrawMeshInstancedIndirect(this._lineMesh, 0, this._material, new Bounds(this._BoundCenter, this._BoundSize), this._GPUInstancingArgsBuffer);
    }

    void OnDestroy()
    {
        if (this._LineDataBuffer != null)
        {
            this._LineDataBuffer.Release();
            this._LineDataBuffer = null;
        }
        if (this._GPUInstancingArgsBuffer != null)
        {
            this._GPUInstancingArgsBuffer.Release();
            this._GPUInstancingArgsBuffer = null;
        }
    }

    #endregion // MonoBehaviour Method
}
