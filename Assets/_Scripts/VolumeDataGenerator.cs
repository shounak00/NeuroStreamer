using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;

public class VolumeDataGenerator : MonoBehaviour
{
    [Header("Data Source")]
    public DataSourceType dataSource = DataSourceType.GenerateTest;
    
    public enum DataSourceType
    {
        GenerateTest,      // Generate simulated medical data
        ImportRawFile,     // Import .raw binary file
        ImportDICOMFolder, // Import DICOM series (requires external library)
        ImportNIfTI        // Import .nii/.nii.gz files
    }
    
    [Header("Import Settings (when using Import modes)")]
    public string importFilePath = ""; // Drag file here or specify path
    public TextAsset rawDataAsset;     // Alternative: drag file into Unity
    
    [Header("Volume Dimensions (for raw import)")]
    public int volumeWidth = 128;
    public int volumeHeight = 128;
    public int volumeDepth = 128;
    
    [Header("Noise Parameters (for test generation)")]
    [Range(0.1f, 10f)]
    public float noiseScale = 2.0f;
    [Range(1, 8)]
    public int octaves = 4;
    [Range(0.1f, 0.9f)]
    public float persistence = 0.5f;
    
    [Header("Processing")]
    [Range(0f, 5f)]
    public float blurSigma = 1.5f;
    public bool normalizeData = true;
    
    [Header("Data Remapping")]
    public bool useCustomRange = false;
    public float inputMinValue = 0f;
    public float inputMaxValue = 1f;
    
    private Texture3D volumeTexture;
    private float[] volumeData;
    
    void Start()
    {
        GenerateVolumeData();
    }
    
    public void GenerateVolumeData()
    {
        switch (dataSource)
        {
            case DataSourceType.GenerateTest:
                GenerateTestData();
                break;
                
            case DataSourceType.ImportRawFile:
                ImportRawData();
                break;
                
            case DataSourceType.ImportDICOMFolder:
                ImportDICOMData();
                break;
                
            case DataSourceType.ImportNIfTI:
                ImportNIfTIData();
                break;
        }
        
        // Common post-processing
        if (volumeData != null && volumeData.Length > 0)
        {
            if (normalizeData)
                NormalizeVolumeData();
            
            if (blurSigma > 0.01f)
                ApplyBlur();
            
            CreateTexture3D();
            Debug.Log($"Volume data loaded successfully! Size: {volumeWidth}x{volumeHeight}x{volumeDepth}");
        }
    }
    
    #region Test Data Generation
    
    void GenerateTestData()
    {
        Debug.Log("Generating test volume data...");
        
        int totalVoxels = volumeWidth * volumeHeight * volumeDepth;
        volumeData = new float[totalVoxels];
        
        // Try to use C++ DLL, fall back to Unity noise if not available
        try
        {
            NativeInterop.GeneratePerlinNoise3D(
                volumeData,
                volumeWidth,
                volumeHeight,
                volumeDepth,
                noiseScale,
                octaves,
                persistence
            );
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"C++ DLL not available, using Unity Perlin noise: {e.Message}");
            GenerateUnityPerlinNoise();
        }
        
        // Add anatomical features
        AddAnatomicalFeatures();
    }
    
    void GenerateUnityPerlinNoise()
    {
        for (int z = 0; z < volumeDepth; z++)
        {
            for (int y = 0; y < volumeHeight; y++)
            {
                for (int x = 0; x < volumeWidth; x++)
                {
                    float xCoord = (float)x / volumeWidth * noiseScale;
                    float yCoord = (float)y / volumeHeight * noiseScale;
                    float zCoord = (float)z / volumeDepth * noiseScale;
                    
                    // 3D noise approximation
                    float noise1 = Mathf.PerlinNoise(xCoord, yCoord);
                    float noise2 = Mathf.PerlinNoise(yCoord, zCoord);
                    float noise3 = Mathf.PerlinNoise(zCoord, xCoord);
                    
                    float value = (noise1 + noise2 + noise3) / 3.0f;
                    
                    int index = x + y * volumeWidth + z * volumeWidth * volumeHeight;
                    volumeData[index] = value;
                }
            }
        }
    }
    
    void AddAnatomicalFeatures()
    {
        // Add a "skull" (high-density sphere)
        AddSphere(volumeWidth / 2, volumeHeight / 2, volumeDepth / 2, volumeWidth * 0.35f, 0.9f);
        
        // Add "brain tissue" (medium-density inner sphere)
        AddSphere(volumeWidth / 2, volumeHeight / 2, volumeDepth / 2, volumeWidth * 0.3f, 0.6f);
        
        // Add some "ventricles" (low-density regions)
        AddSphere(volumeWidth / 2 - volumeWidth * 0.1f, volumeHeight / 2, volumeDepth / 2, volumeWidth * 0.08f, 0.2f);
        AddSphere(volumeWidth / 2 + volumeWidth * 0.1f, volumeHeight / 2, volumeDepth / 2, volumeWidth * 0.08f, 0.2f);
        
        // Add "tumor" (irregular high-density region)
        AddIrregularRegion(volumeWidth / 2 + volumeWidth * 0.15f, volumeHeight / 2 + volumeHeight * 0.08f, volumeDepth / 2, volumeWidth * 0.1f, 0.85f);
    }
    
    #endregion
    
    #region Data Import Methods
    
    void ImportRawData()
    {
        Debug.Log("Importing RAW volume data...");
        
        byte[] rawBytes = null;
        
        // Try loading from TextAsset first (file dragged into Unity)
        if (rawDataAsset != null)
        {
            rawBytes = rawDataAsset.bytes;
            Debug.Log($"Loaded from TextAsset: {rawBytes.Length} bytes");
        }
        // Try loading from file path
        else if (!string.IsNullOrEmpty(importFilePath) && File.Exists(importFilePath))
        {
            rawBytes = File.ReadAllBytes(importFilePath);
            Debug.Log($"Loaded from file: {rawBytes.Length} bytes");
        }
        else
        {
            Debug.LogError("No raw data file specified! Set importFilePath or drag file to rawDataAsset.");
            return;
        }
        
        int totalVoxels = volumeWidth * volumeHeight * volumeDepth;
        volumeData = new float[totalVoxels];
        
        // Determine data type based on file size
        int bytesPerVoxel = rawBytes.Length / totalVoxels;
        
        Debug.Log($"Detected {bytesPerVoxel} bytes per voxel");
        
        switch (bytesPerVoxel)
        {
            case 1: // 8-bit unsigned
                for (int i = 0; i < totalVoxels && i < rawBytes.Length; i++)
                {
                    volumeData[i] = rawBytes[i] / 255.0f;
                }
                break;
                
            case 2: // 16-bit signed (common for CT/MRI)
                for (int i = 0; i < totalVoxels && i * 2 + 1 < rawBytes.Length; i++)
                {
                    short value = System.BitConverter.ToInt16(rawBytes, i * 2);
                    volumeData[i] = value;
                }
                break;
                
            case 4: // 32-bit float
                for (int i = 0; i < totalVoxels && i * 4 + 3 < rawBytes.Length; i++)
                {
                    volumeData[i] = System.BitConverter.ToSingle(rawBytes, i * 4);
                }
                break;
                
            default:
                Debug.LogError($"Unsupported data format: {bytesPerVoxel} bytes per voxel");
                return;
        }
    }
    
    void ImportDICOMData()
    {
        Debug.LogWarning("DICOM import requires external library (fo-dicom or similar).");
        Debug.LogWarning("For now, export DICOM as RAW format and use ImportRawFile mode.");
        
        // Placeholder for DICOM import
        // You would need to integrate a DICOM library like:
        // - fo-dicom: https://github.com/fo-dicom/fo-dicom
        // - ITK: https://itk.org/
        
        // Example workflow:
        // 1. Install fo-dicom via NuGet
        // 2. Load DICOM series from folder
        // 3. Extract pixel data
        // 4. Convert to volumeData array
    }
    
    void ImportNIfTIData()
    {
        Debug.LogWarning("NIfTI import requires custom parser or external library.");
        Debug.LogWarning("For now, use tools like 3D Slicer to convert NIfTI to RAW format.");
        
        // Placeholder for NIfTI import
        // You could implement a basic NIfTI reader or use:
        // - SimpleITK
        // - Custom C# NIfTI parser
        
        if (!string.IsNullOrEmpty(importFilePath) && File.Exists(importFilePath))
        {
            // Basic NIfTI header is 348 bytes
            // Data follows immediately after
            byte[] fileBytes = File.ReadAllBytes(importFilePath);
            
            // This is simplified - real NIfTI parsing is more complex
            Debug.Log("NIfTI file loaded, but parsing not fully implemented.");
            Debug.Log("Consider using RAW export from medical imaging software.");
        }
    }
    
    #endregion
    
    #region Processing Methods
    
    void NormalizeVolumeData()
    {
        if (volumeData == null || volumeData.Length == 0) return;
        
        float minVal = float.MaxValue;
        float maxVal = float.MinValue;
        
        // Find min/max
        foreach (float value in volumeData)
        {
            if (value < minVal) minVal = value;
            if (value > maxVal) maxVal = value;
        }
        
        Debug.Log($"Data range before normalization: [{minVal}, {maxVal}]");
        
        // Apply custom range if specified
        if (useCustomRange)
        {
            minVal = inputMinValue;
            maxVal = inputMaxValue;
        }
        
        // Normalize to [0, 1]
        float range = maxVal - minVal;
        if (range > 0.0001f)
        {
            for (int i = 0; i < volumeData.Length; i++)
            {
                volumeData[i] = Mathf.Clamp01((volumeData[i] - minVal) / range);
            }
        }
        
        Debug.Log("Data normalized to [0, 1]");
    }
    
    void ApplyBlur()
    {
        try
        {
            float[] blurredData = new float[volumeData.Length];
            NativeInterop.ApplyGaussianBlur3D(
                volumeData,
                blurredData,
                volumeWidth,
                volumeHeight,
                volumeDepth,
                blurSigma
            );
            volumeData = blurredData;
            Debug.Log("Gaussian blur applied");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not apply blur (C++ DLL not available): {e.Message}");
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    void AddSphere(float cx, float cy, float cz, float radius, float density)
    {
        int minX = Mathf.Max(0, (int)(cx - radius));
        int maxX = Mathf.Min(volumeWidth - 1, (int)(cx + radius));
        int minY = Mathf.Max(0, (int)(cy - radius));
        int maxY = Mathf.Min(volumeHeight - 1, (int)(cy + radius));
        int minZ = Mathf.Max(0, (int)(cz - radius));
        int maxZ = Mathf.Min(volumeDepth - 1, (int)(cz + radius));
        
        for (int z = minZ; z <= maxZ; z++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dz = z - cz;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
                    
                    if (dist < radius)
                    {
                        int index = x + y * volumeWidth + z * volumeWidth * volumeHeight;
                        float falloff = 1.0f - (dist / radius);
                        volumeData[index] = Mathf.Max(volumeData[index], density * falloff);
                    }
                }
            }
        }
    }
    
    void AddIrregularRegion(float cx, float cy, float cz, float radius, float density)
    {
        for (int i = 0; i < 5; i++)
        {
            float offsetX = Random.Range(-radius * 0.3f, radius * 0.3f);
            float offsetY = Random.Range(-radius * 0.3f, radius * 0.3f);
            float offsetZ = Random.Range(-radius * 0.3f, radius * 0.3f);
            float subRadius = Random.Range(radius * 0.5f, radius * 0.8f);
            
            AddSphere(cx + offsetX, cy + offsetY, cz + offsetZ, subRadius, density);
        }
    }
    
    void CreateTexture3D()
    {
        volumeTexture = new Texture3D(volumeWidth, volumeHeight, volumeDepth, TextureFormat.RFloat, false);
        volumeTexture.wrapMode = TextureWrapMode.Clamp;
        volumeTexture.filterMode = FilterMode.Trilinear;
        
        Color[] colors = new Color[volumeData.Length];
        for (int i = 0; i < volumeData.Length; i++)
        {
            float value = volumeData[i];
            colors[i] = new Color(value, value, value, value);
        }
        
        volumeTexture.SetPixels(colors);
        volumeTexture.Apply();
        
        VolumeRenderer renderer = GetComponent<VolumeRenderer>();
        if (renderer != null)
        {
            renderer.SetVolumeTexture(volumeTexture);
        }
    }
    
    #endregion
    
    #region Public API
    
    public Texture3D GetVolumeTexture()
    {
        return volumeTexture;
    }
    
    public float[] GetVolumeData()
    {
        return volumeData;
    }
    
    public Vector3Int GetVolumeDimensions()
    {
        return new Vector3Int(volumeWidth, volumeHeight, volumeDepth);
    }
    
    // Manual regeneration (for UI button)
    public void RegenerateData()
    {
        GenerateVolumeData();
    }
    
    #endregion
}