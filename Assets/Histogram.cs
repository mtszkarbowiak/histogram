using System;
using UnityEngine;

[ExecuteAlways]
public class Histogram : MonoBehaviour
{
    [SerializeField] private Material histogramMaterial;
    [SerializeField] private ComputeShader histogramGatherer;
    [SerializeField] private Vector4 scalar;
    
    private int4[] _histogramBufferValues;
    private RenderTexture _inputBuffer;
    private ComputeBuffer _histogramBufferController;
    
    private static readonly int HistogramValues = Shader.PropertyToID("_HistogramValues");
    private static readonly int HistogramScalar = Shader.PropertyToID("_HistogramScalar");
    private static readonly int Size = Shader.PropertyToID("_Size");
    private static readonly int Minimum = Shader.PropertyToID("_Minimum");
    
    [Serializable]
    public struct int4
    {
        public int x, y, z, w;
        
        public int4(int x, int y, int z, int w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }



    private void OnDestroy()
    {
        if(_histogramBufferController != null)
        {
            _histogramBufferController.Release();
            _histogramBufferController = null;
        }
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        RegenerateInputBuffer(src);

        RegenerateHistogramBuffer();
        
        _histogramBufferController.GetData(_histogramBufferValues);

        var maxChannels = new int4(0, 0, 0, 0);
        for (var i = 1; i < _histogramBufferValues.Length - 1; i++) // Pominięcie pierwszej i ostatniej
        {
            var compared = _histogramBufferValues[i];

            maxChannels = new int4(
                x: Mathf.Max(compared.x, maxChannels.x),
                y: Mathf.Max(compared.y, maxChannels.y),
                z: Mathf.Max(compared.z, maxChannels.z),
                w: Mathf.Max(compared.w, maxChannels.w)
            );
        }

        var maxTotal = Mathf.Max(maxChannels.x, maxChannels.y, maxChannels.z);
        var scalar2 = scalar * (1.0f / maxTotal);
        
        histogramMaterial.SetVector(HistogramScalar, scalar2);
        histogramGatherer.SetTexture(0, "InputTexture", _inputBuffer);
        histogramGatherer.SetBuffer(0, "HistogramBuffer", _histogramBufferController);
        histogramGatherer.SetBuffer(1, "HistogramBuffer", _histogramBufferController);

        var threadGroupsX = (src.width + 7) / 8;
        var threadGroupsY = (src.height + 7) / 8;
        
        // Zerowanie wartości
        histogramGatherer.Dispatch(1, threadGroupsX, threadGroupsY, 1);
        // Zbieranie histogramu
        histogramGatherer.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        
        histogramMaterial.SetBuffer(HistogramValues, _histogramBufferController);

        Graphics.Blit(src, dest, histogramMaterial);
    }

    private void RegenerateHistogramBuffer()
    {
        if (_histogramBufferController == null)
        {
            _histogramBufferController = new ComputeBuffer(256, 4 * 4);
        }

        if (_histogramBufferValues == null)
        {
            _histogramBufferValues = new int4[256];
        }
    }

    private void RegenerateInputBuffer(RenderTexture src)
    {
        if (_inputBuffer == null)
        {
            _inputBuffer = new RenderTexture(src.width, src.height, src.depth)
            {
                enableRandomWrite = true
            };
            _inputBuffer.Create();
            
            RegenerateInputBuffer(src);
        }
        else if(_inputBuffer.IsCreated() == false)
        {
            _inputBuffer = null;
            
            RegenerateInputBuffer(src);
        }
        else if (_inputBuffer.width != src.width || _inputBuffer.height != src.height)
        {
            _inputBuffer.Release();
            _inputBuffer = null;
            
            RegenerateInputBuffer(src);
        }
        else
        {
            Graphics.Blit(src, _inputBuffer);
        }
    }

    
    
    public float RedScaler { set => scalar.x = value; }
    public float GreenScaler { set => scalar.y = value; }
    public float BlueScaler { set => scalar.z = value; }
    
    public float FadeSize { set => histogramMaterial.SetFloat(Size, value); }
    public float MinimalFill { set => histogramMaterial.SetFloat(Minimum, value); }
}
