using System.Runtime.InteropServices;
using UnityEngine;

public static class NativeInterop
{
    private const string DllName = "MedicalImageProcessing";
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void GeneratePerlinNoise3D(
        float[] outData,
        int width,
        int height,
        int depth,
        float scale,
        int octaves,
        float persistence
    );
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ApplyGaussianBlur3D(
        float[] inData,
        float[] outData,
        int width,
        int height,
        int depth,
        float sigma
    );
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void CalculateHistogram(
        float[] data,
        int size,
        int[] histogram,
        int bins,
        float minVal,
        float maxVal
    );
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ApplyThreshold(
        float[] inData,
        byte[] outMask,
        int size,
        float minThreshold,
        float maxThreshold
    );
}
