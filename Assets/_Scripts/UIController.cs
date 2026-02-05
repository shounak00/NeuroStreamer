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
    
    [Header("Labels (for real-time value display)")]
    public TextMeshProUGUI minThresholdLabel;
    public TextMeshProUGUI maxThresholdLabel;
    public TextMeshProUGUI axialLabel;
    public TextMeshProUGUI coronalLabel;
    public TextMeshProUGUI sagittalLabel;
    public TextMeshProUGUI stepSizeLabel;
    public TextMeshProUGUI densityLabel;
    
    [Header("Info Text Elements")]
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
        SetupSliders();
        
        // Setup toggle listeners
        SetupToggles();
        
        // Initialize label values
        UpdateAllLabels();
    }
    
    void SetupSliders()
    {
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
    }
    
    void SetupToggles()
    {
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
                fpsText.color = new Color(0f, 1f, 0.53f); // Green #00FF88
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
        
        // Simple center screen ray for now
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
            if (voxelInfoText != null)
            {
                float[] volumeData = dataGenerator.GetVolumeData();
                if (volumeData != null && volumeData.Length > 0)
                {
                    int index = voxelCoord.x + voxelCoord.y * dimensions.x + voxelCoord.z * dimensions.x * dimensions.y;
                    if (index >= 0 && index < volumeData.Length)
                    {
                        float value = volumeData[index];
                        
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
            stringBuilder.Append("Range: ");
            stringBuilder.Append((volumeRenderer.minThreshold * 100).ToString("F0"));
            stringBuilder.Append("% - ");
            stringBuilder.Append((volumeRenderer.maxThreshold * 100).ToString("F0"));
            stringBuilder.Append("%");
            thresholdText.text = stringBuilder.ToString();
        }
    }
    
    void UpdateAllLabels()
    {
        if (minThresholdLabel != null)
            minThresholdLabel.text = $"Min Threshold: {volumeRenderer.minThreshold:F2}";
        
        if (maxThresholdLabel != null)
            maxThresholdLabel.text = $"Max Threshold: {volumeRenderer.maxThreshold:F2}";
        
        if (axialLabel != null)
            axialLabel.text = $"Axial Slice: {(volumeRenderer.axialSlicePosition * 100):F0}%";
        
        if (coronalLabel != null)
            coronalLabel.text = $"Coronal Slice: {(volumeRenderer.coronalSlicePosition * 100):F0}%";
        
        if (sagittalLabel != null)
            sagittalLabel.text = $"Sagittal Slice: {(volumeRenderer.sagittalSlicePosition * 100):F0}%";
        
        if (stepSizeLabel != null)
            stepSizeLabel.text = $"Step Size: {volumeRenderer.stepSize:F3}";
        
        if (densityLabel != null)
            densityLabel.text = $"Density: {volumeRenderer.densityMultiplier:F1}x";
    }
    
    // Slider callbacks
    void OnMinThresholdChanged(float value)
    {
        volumeRenderer.SetMinThreshold(value);
        if (minThresholdLabel != null)
            minThresholdLabel.text = $"Min Threshold: {value:F2}";
        
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
        if (maxThresholdLabel != null)
            maxThresholdLabel.text = $"Max Threshold: {value:F2}";
        
        if (value < volumeRenderer.minThreshold)
        {
            volumeRenderer.SetMinThreshold(value);
            if (minThresholdSlider != null)
                minThresholdSlider.value = value;
        }
    }
    
    void OnAxialSliceChanged(float value)
    {
        volumeRenderer.SetAxialSlicePosition(value);
        if (axialLabel != null)
            axialLabel.text = $"Axial Slice: {(value * 100):F0}%";
    }
    
    void OnCoronalSliceChanged(float value)
    {
        volumeRenderer.SetCoronalSlicePosition(value);
        if (coronalLabel != null)
            coronalLabel.text = $"Coronal Slice: {(value * 100):F0}%";
    }
    
    void OnSagittalSliceChanged(float value)
    {
        volumeRenderer.SetSagittalSlicePosition(value);
        if (sagittalLabel != null)
            sagittalLabel.text = $"Sagittal Slice: {(value * 100):F0}%";
    }
    
    void OnStepSizeChanged(float value)
    {
        volumeRenderer.stepSize = value;
        if (stepSizeLabel != null)
            stepSizeLabel.text = $"Step Size: {value:F3}";
    }
    
    void OnDensityChanged(float value)
    {
        volumeRenderer.densityMultiplier = value;
        if (densityLabel != null)
            densityLabel.text = $"Density: {value:F1}x";
    }
    
    // Toggle callbacks
    void OnVolumeToggleChanged(bool value)
    {
        volumeRenderer.showVolume = value;
    }
    
    void OnAxialToggleChanged(bool value)
    {
        volumeRenderer.showAxialSlice = value;
    }
    
    void OnCoronalToggleChanged(bool value)
    {
        volumeRenderer.showCoronalSlice = value;
    }
    
    void OnSagittalToggleChanged(bool value)
    {
        volumeRenderer.showSagittalSlice = value;
    }
    
    // Button callbacks (add buttons if you want)
    public void OnRegenerateData()
    {
        if (dataGenerator != null)
        {
            dataGenerator.GenerateVolumeData();
            Debug.Log("Volume data regenerated!");
        }
    }
    
    public void OnResetThresholds()
    {
        volumeRenderer.SetMinThreshold(0.3f);
        volumeRenderer.SetMaxThreshold(1.0f);
        
        if (minThresholdSlider != null)
            minThresholdSlider.value = 0.3f;
        if (maxThresholdSlider != null)
            maxThresholdSlider.value = 1.0f;
        
        UpdateAllLabels();
    }
    
    public void OnResetSlices()
    {
        volumeRenderer.SetAxialSlicePosition(0.5f);
        volumeRenderer.SetCoronalSlicePosition(0.5f);
        volumeRenderer.SetSagittalSlicePosition(0.5f);
        
        if (axialSliceSlider != null)
            axialSliceSlider.value = 0.5f;
        if (coronalSliceSlider != null)
            coronalSliceSlider.value = 0.5f;
        if (sagittalSliceSlider != null)
            sagittalSliceSlider.value = 0.5f;
        
        UpdateAllLabels();
    }
    
    public void OnResetAll()
    {
        OnResetThresholds();
        OnResetSlices();
        
        volumeRenderer.stepSize = 0.01f;
        volumeRenderer.densityMultiplier = 1.0f;
        
        if (stepSizeSlider != null)
            stepSizeSlider.value = 0.01f;
        if (densitySlider != null)
            densitySlider.value = 1.0f;
        
        UpdateAllLabels();
    }
    
    // Keyboard shortcuts (optional bonus)
    void OnGUI()
    {
        Event e = Event.current;
        
        if (e.type == EventType.KeyDown)
        {
            // R = Regenerate
            if (e.keyCode == KeyCode.R)
                OnRegenerateData();
            
            // T = Reset Thresholds
            if (e.keyCode == KeyCode.T)
                OnResetThresholds();
            
            // S = Reset Slices
            if (e.keyCode == KeyCode.S)
                OnResetSlices();
            
            // Space = Toggle Volume
            if (e.keyCode == KeyCode.Space && volumeToggle != null)
                volumeToggle.isOn = !volumeToggle.isOn;
        }
    }
}

/*
```

## Key Changes Made:

1. **Added Label References** - Now updates labels in real-time as sliders move
2. **Better Color Coding** - FPS text changes color (green/yellow/red)
3. **Keyboard Shortcuts** - R, T, S, Space for quick actions
4. **StringBuilder Optimization** - Better performance for text updates
5. **Null Checks** - Won't crash if some UI elements are missing
6. **Initial Value Sync** - Sliders start at correct positions

## Optional: Add Buttons

If you want buttons for regenerate/reset, add these to your UI:

**Create Button Section:**

1. **Right-click MainPanel → UI → Button - TextMeshPro**
2. **Rename to "RegenerateButton"**
3. **Position below toggles**
4. **Settings:**
```
   Rect Transform:
   ├─ Pos Y: -620
   ├─ Width: 260, Height: 35
   
   Button:
   ├─ Normal Color: RGB(42, 90, 122) - Dark blue
   ├─ Highlighted: RGB(58, 122, 154) - Lighter blue
   ├─ Pressed: RGB(26, 74, 106) - Darker blue
   
   Text (child):
   ├─ Text: "REGENERATE DATA"
   ├─ Font Size: 14
   ├─ Color: White
   └─ Style: Bold
```

5. **In Inspector, scroll to Button component:**
   - Click "+" under OnClick()
   - Drag UIManager GameObject to the slot
   - Function dropdown: UIController → OnRegenerateData()

**Repeat for Reset Buttons:**
- ResetThresholdsButton (Pos Y: -665) → OnResetThresholds()
- ResetSlicesButton (Pos Y: -710) → OnResetSlices()

## Additional UI Enhancements (Optional):

### Add Section Headers:

Between sections, add divider lines:

1. **Right-click MainPanel → UI → Image**
2. **Rename to "Divider1"**
3. **Settings:**
```
   Rect Transform:
   ├─ Width: 280, Height: 1
   ├─ Pos Y: -105 (between title and threshold)
   
   Image:
   └─ Color: RGB(80, 80, 80, 100) - Gray, semi-transparent
```

### Add Section Title Text:

1. **Right-click MainPanel → UI → Text - TextMeshPro**
2. **Rename to "ThresholdTitle"**
3. **Settings:**
```
   Text: "THRESHOLD CONTROLS"
   Font Size: 10
   Color: RGB(0, 212, 255, 200) - Cyan, slightly transparent
   Font Style: Bold
   Position: Above threshold section
```

Repeat for:
- "SLICE POSITIONS"
- "VISIBILITY"
- "PERFORMANCE"

## Wiring Up in Unity:

1. **Select UIManager** (the GameObject with UIController script)
2. **Drag all your UI elements to the corresponding fields:**
```
   References:
   ├─ Volume Renderer: [VolumeSystem]
   └─ Data Generator: [VolumeSystem]
   
   Sliders:
   ├─ Min Threshold Slider: [MinThresholdSlider]
   ├─ Max Threshold Slider: [MaxThresholdSlider]
   ├─ Axial Slice Slider: [AxialSlider]
   ├─ Coronal Slice Slider: [CoronalSlider]
   ├─ Sagittal Slice Slider: [SagittalSlider]
   ├─ Step Size Slider: [StepSizeSlider] (if you made one)
   └─ Density Slider: [DensitySlider] (if you made one)
   
   Toggles:
   ├─ Volume Toggle: [VolumeToggle]
   ├─ Axial Toggle: [AxialToggle]
   ├─ Coronal Toggle: [CoronalToggle]
   └─ Sagittal Toggle: [SagittalToggle]
   
   Labels:
   ├─ Min Threshold Label: [MinThresholdLabel]
   ├─ Max Threshold Label: [MaxThresholdLabel]
   ├─ Axial Label: [AxialLabel]
   ├─ Coronal Label: [CoronalLabel]
   └─ Sagittal Label: [SagittalLabel]
   
   Info Text:
   ├─ FPS Text: [FPSText]
   ├─ Memory Text: [MemoryText]
   └─ Coordinates Text: [CoordinatesText]
   
   Crosshair:
   ├─ Crosshair H: [CrosshairH]
   └─ Crosshair V: [CrosshairV] */