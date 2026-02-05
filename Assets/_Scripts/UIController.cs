using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class UIController : MonoBehaviour
{
    [Header("References")]
    public VolumeRenderer volumeRenderer;
    public VolumeDataGenerator dataGenerator;
    
    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject performancePanel;
    public GameObject histogramPanel;
    
    [Header("Sliders")]
    public Slider minThresholdSlider;
    public Slider maxThresholdSlider;
    public Slider axialSliceSlider;
    public Slider coronalSliceSlider;
    public Slider sagittalSliceSlider;
    public Slider stepSizeSlider;
    public Slider densitySlider;
    
    [Header("Toggles")]
    public Toggle volumeToggle;
    public Toggle axialToggle;
    public Toggle coronalToggle;
    public Toggle sagittalToggle;
    
    [Header("Text Elements")]
    public TextMeshProUGUI fpsText;
    public TextMeshProUGUI memoryText;
    public TextMeshProUGUI coordinatesText;
    public TextMeshProUGUI thresholdText;
    public TextMeshProUGUI voxelInfoText;
    
    [Header("Crosshair")]
    public RectTransform crosshairH;
    public RectTransform crosshairV;
    
    private float deltaTime;
    private Camera mainCamera;
    private StringBuilder stringBuilder = new StringBuilder();
    
    void Start()
    {
        mainCamera = Camera.main;
        
        // Setup slider listeners
        if (minThresholdSlider != null)
        {
            minThresholdSlider.onValueChanged.AddListener(OnMinThresholdChanged);
            minThresholdSlider.value = volumeRenderer.minThreshold;
        }
        
        if (maxThresholdSlider != null)
        {
            maxThresholdSlider.onValueChanged.AddListener(OnMaxThresholdChanged);
            maxThresholdSlider.value = volumeRenderer.maxThreshold;
        }
        
        if (axialSliceSlider != null)
        {
            axialSliceSlider.onValueChanged.AddListener(OnAxialSliceChanged);
            axialSliceSlider.value = volumeRenderer.axialSlicePosition;
        }
        
        if (coronalSliceSlider != null)
        {
            coronalSliceSlider.onValueChanged.AddListener(OnCoronalSliceChanged);
            coronalSliceSlider.value = volumeRenderer.coronalSlicePosition;
        }
        
        if (sagittalSliceSlider != null)
        {
            sagittalSliceSlider.onValueChanged.AddListener(OnSagittalSliceChanged);
            sagittalSliceSlider.value = volumeRenderer.sagittalSlicePosition;
        }
        
        if (stepSizeSlider != null)
        {
            stepSizeSlider.onValueChanged.AddListener(OnStepSizeChanged);
            stepSizeSlider.value = volumeRenderer.stepSize;
        }
        
        if (densitySlider != null)
        {
            densitySlider.onValueChanged.AddListener(OnDensityChanged);
            densitySlider.value = volumeRenderer.densityMultiplier;
        }
        
        // Setup toggle listeners
        if (volumeToggle != null)
        {
            volumeToggle.onValueChanged.AddListener(OnVolumeToggleChanged);
            volumeToggle.isOn = volumeRenderer.showVolume;
        }
        
        if (axialToggle != null)
        {
            axialToggle.onValueChanged.AddListener(OnAxialToggleChanged);
            axialToggle.isOn = volumeRenderer.showAxialSlice;
        }
        
        if (coronalToggle != null)
        {
            coronalToggle.onValueChanged.AddListener(OnCoronalToggleChanged);
            coronalToggle.isOn = volumeRenderer.showCoronalSlice;
        }
        
        if (sagittalToggle != null)
        {
            sagittalToggle.onValueChanged.AddListener(OnSagittalToggleChanged);
            sagittalToggle.isOn = volumeRenderer.showSagittalSlice;
        }
    }
    
    void Update()
    {
        // Calculate FPS
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        
        UpdatePerformanceDisplay();
        UpdateCrosshairInfo();
        UpdateThresholdDisplay();
    }
    
    void UpdatePerformanceDisplay()
    {
        if (fpsText != null)
        {
            float fps = 1.0f / deltaTime;
            stringBuilder.Clear();
            stringBuilder.Append("FPS: ");
            stringBuilder.Append(fps.ToString("F1"));
            fpsText.text = stringBuilder.ToString();
            
            // Color code based on performance
            if (fps >= 60)
                fpsText.color = Color.green;
            else if (fps >= 30)
                fpsText.color = Color.yellow;
            else
                fpsText.color = Color.red;
        }
        
        if (memoryText != null)
        {
            long totalMemory = System.GC.GetTotalMemory(false);
            float memoryMB = totalMemory / (1024f * 1024f);
            
            stringBuilder.Clear();
            stringBuilder.Append("Memory: ");
            stringBuilder.Append(memoryMB.ToString("F1"));
            stringBuilder.Append(" MB");
            memoryText.text = stringBuilder.ToString();
        }
    }
    
    void UpdateCrosshairInfo()
    {
        if (coordinatesText == null) return;
        
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 100f))
        {
            Vector3 localPoint = volumeRenderer.transform.InverseTransformPoint(hit.point);
            Vector3 normalizedPoint = (localPoint + Vector3.one * 0.5f);
            
            Vector3Int dimensions = dataGenerator.GetVolumeDimensions();
            Vector3Int voxelCoord = new Vector3Int(
                Mathf.FloorToInt(normalizedPoint.x * dimensions.x),
                Mathf.FloorToInt(normalizedPoint.y * dimensions.y),
                Mathf.FloorToInt(normalizedPoint.z * dimensions.z)
            );
            
            voxelCoord.x = Mathf.Clamp(voxelCoord.x, 0, dimensions.x - 1);
            voxelCoord.y = Mathf.Clamp(voxelCoord.y, 0, dimensions.y - 1);
            voxelCoord.z = Mathf.Clamp(voxelCoord.z, 0, dimensions.z - 1);
            
            stringBuilder.Clear();
            stringBuilder.Append("Position: (");
            stringBuilder.Append(voxelCoord.x);
            stringBuilder.Append(", ");
            stringBuilder.Append(voxelCoord.y);
            stringBuilder.Append(", ");
            stringBuilder.Append(voxelCoord.z);
            stringBuilder.Append(")");
            coordinatesText.text = stringBuilder.ToString();
            
            // Get voxel value
            float[] volumeData = dataGenerator.GetVolumeData();
            if (volumeData != null && volumeData.Length > 0)
            {
                int index = voxelCoord.x + voxelCoord.y * dimensions.x + voxelCoord.z * dimensions.x * dimensions.y;
                if (index >= 0 && index < volumeData.Length)
                {
                    float value = volumeData[index];
                    
                    if (voxelInfoText != null)
                    {
                        stringBuilder.Clear();
                        stringBuilder.Append("Density: ");
                        stringBuilder.Append((value * 100).ToString("F1"));
                        stringBuilder.Append("%");
                        voxelInfoText.text = stringBuilder.ToString();
                    }
                }
            }
        }
        else
        {
            coordinatesText.text = "Position: --";
            if (voxelInfoText != null)
                voxelInfoText.text = "Density: --";
        }
    }
    
    void UpdateThresholdDisplay()
    {
        if (thresholdText != null)
        {
            stringBuilder.Clear();
            stringBuilder.Append("Threshold Range: ");
            stringBuilder.Append((volumeRenderer.minThreshold * 100).ToString("F0"));
            stringBuilder.Append("% - ");
            stringBuilder.Append((volumeRenderer.maxThreshold * 100).ToString("F0"));
            stringBuilder.Append("%");
            thresholdText.text = stringBuilder.ToString();
        }
    }
    
    // Slider callbacks
    void OnMinThresholdChanged(float value)
    {
        volumeRenderer.SetMinThreshold(value);
        if (value > volumeRenderer.maxThreshold)
        {
            volumeRenderer.SetMaxThreshold(value);
            if (maxThresholdSlider != null)
                maxThresholdSlider.value = value;
        }
    }
    
    void OnMaxThresholdChanged(float value)
    {
        volumeRenderer.SetMaxThreshold(value);
        if (value < volumeRenderer.minThreshold)
        {
            volumeRenderer.SetMinThreshold(value);
            if (minThresholdSlider != null)
                minThresholdSlider.value = value;
        }
    }
    
    void OnAxialSliceChanged(float value) => volumeRenderer.SetAxialSlicePosition(value);
    void OnCoronalSliceChanged(float value) => volumeRenderer.SetCoronalSlicePosition(value);
    void OnSagittalSliceChanged(float value) => volumeRenderer.SetSagittalSlicePosition(value);
    
    void OnStepSizeChanged(float value) => volumeRenderer.stepSize = value;
    void OnDensityChanged(float value) => volumeRenderer.densityMultiplier = value;
    
    // Toggle callbacks
    void OnVolumeToggleChanged(bool value) => volumeRenderer.showVolume = value;
    void OnAxialToggleChanged(bool value) => volumeRenderer.showAxialSlice = value;
    void OnCoronalToggleChanged(bool value) => volumeRenderer.showCoronalSlice = value;
    void OnSagittalToggleChanged(bool value) => volumeRenderer.showSagittalSlice = value;
    
    // Button callbacks
    public void OnRegenerateData() => dataGenerator.GenerateVolumeData();
    
    public void OnResetThresholds()
    {
        volumeRenderer.SetMinThreshold(0.3f);
        volumeRenderer.SetMaxThreshold(1.0f);
        
        if (minThresholdSlider != null) minThresholdSlider.value = 0.3f;
        if (maxThresholdSlider != null) maxThresholdSlider.value = 1.0f;
    }
    
    public void OnResetSlices()
    {
        volumeRenderer.SetAxialSlicePosition(0.5f);
        volumeRenderer.SetCoronalSlicePosition(0.5f);
        volumeRenderer.SetSagittalSlicePosition(0.5f);
        
        if (axialSliceSlider != null) axialSliceSlider.value = 0.5f;
        if (coronalSliceSlider != null) coronalSliceSlider.value = 0.5f;
        if (sagittalSliceSlider != null) sagittalSliceSlider.value = 0.5f;
    }
}
