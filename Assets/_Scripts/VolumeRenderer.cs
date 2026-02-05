using UnityEngine;
using System.Collections;

[RequireComponent(typeof(VolumeDataGenerator))]
public class VolumeRenderer : MonoBehaviour
{
    [Header("Rendering")]
    public Material volumeMaterial;
    public Material sliceMaterial;
    
    [Header("Slice Planes")]
    public GameObject axialSlicePlane;
    public GameObject coronalSlicePlane;
    public GameObject sagittalSlicePlane;
    
    [Header("Thresholding")]
    [Range(0f, 1f)]
    public float minThreshold = 0.3f;
    [Range(0f, 1f)]
    public float maxThreshold = 1.0f;
    
    [Header("Slice Positions")]
    [Range(0f, 1f)]
    public float axialSlicePosition = 0.5f;
    [Range(0f, 1f)]
    public float coronalSlicePosition = 0.5f;
    [Range(0f, 1f)]
    public float sagittalSlicePosition = 0.5f;
    
    [Header("Visualization")]
    public bool showVolume = true;
    public bool showAxialSlice = true;
    public bool showCoronalSlice = true;
    public bool showSagittalSlice = true;
    [Range(0.01f, 0.2f)]
    public float stepSize = 0.01f;
    [Range(0.1f, 5f)]
    public float densityMultiplier = 1.0f;
    
    private Texture3D volumeTexture;
    private MeshRenderer volumeRenderer;
    private VolumeDataGenerator dataGenerator;
    
    void Start()
    {
        dataGenerator = GetComponent<VolumeDataGenerator>();
        
        // Setup volume cube
        SetupVolumeCube();
        
        // Setup slice planes
        SetupSlicePlanes();
    }
    
    void SetupVolumeCube()
    {
        // Create a cube for volume rendering
        GameObject volumeCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        volumeCube.name = "VolumeCube";
        volumeCube.transform.SetParent(transform);
        volumeCube.transform.localPosition = Vector3.zero;
        volumeCube.transform.localScale = Vector3.one * 5f;
        
        volumeRenderer = volumeCube.GetComponent<MeshRenderer>();
        volumeRenderer.material = volumeMaterial;
        
        // Keep collider for UI raycasting but make it a trigger
        BoxCollider collider = volumeCube.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }
    
    void SetupSlicePlanes()
    {
        // Create slice planes if they don't exist
        if (axialSlicePlane == null)
        {
            axialSlicePlane = CreateSlicePlane("AxialSlice", new Vector3(90, 0, 0));
        }
        
        if (coronalSlicePlane == null)
        {
            coronalSlicePlane = CreateSlicePlane("CoronalSlice", new Vector3(0, 0, 0));
        }
        
        if (sagittalSlicePlane == null)
        {
            sagittalSlicePlane = CreateSlicePlane("SagittalSlice", new Vector3(0, 90, 0));
        }
    }
    
    GameObject CreateSlicePlane(string name, Vector3 rotation)
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plane.name = name;
        plane.transform.SetParent(transform);
        plane.transform.localRotation = Quaternion.Euler(rotation);
        plane.transform.localScale = Vector3.one * 5.5f;
        
        MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
        renderer.material = new Material(sliceMaterial);
        
        Destroy(plane.GetComponent<Collider>());
        
        return plane;
    }
    
    void Update()
    {
        if (volumeMaterial != null && volumeTexture != null)
        {
            // Update volume material properties
            volumeMaterial.SetTexture("_Volume", volumeTexture);
            volumeMaterial.SetFloat("_MinThreshold", minThreshold);
            volumeMaterial.SetFloat("_MaxThreshold", maxThreshold);
            volumeMaterial.SetFloat("_StepSize", stepSize);
            volumeMaterial.SetFloat("_DensityMultiplier", densityMultiplier);
            
            volumeRenderer.enabled = showVolume;
        }
        
        // Update slice positions and visibility
        UpdateSlicePlane(axialSlicePlane, showAxialSlice, axialSlicePosition, Vector3.up);
        UpdateSlicePlane(coronalSlicePlane, showCoronalSlice, coronalSlicePosition, Vector3.forward);
        UpdateSlicePlane(sagittalSlicePlane, showSagittalSlice, sagittalSlicePosition, Vector3.right);
    }
    
    void UpdateSlicePlane(GameObject plane, bool visible, float position, Vector3 direction)
    {
        if (plane == null) return;
        
        plane.SetActive(visible);
        
        if (visible)
        {
            plane.transform.localPosition = direction * (position - 0.5f) * 5f;
            
            Material mat = plane.GetComponent<MeshRenderer>().material;
            if (volumeTexture != null)
            {
                mat.SetTexture("_Volume", volumeTexture);
                mat.SetFloat("_SlicePosition", position);
                mat.SetFloat("_MinThreshold", minThreshold);
                mat.SetFloat("_MaxThreshold", maxThreshold);
                
                // Set slice direction
                if (direction == Vector3.up)
                    mat.SetInt("_SliceAxis", 2); // Z axis (axial)
                else if (direction == Vector3.forward)
                    mat.SetInt("_SliceAxis", 1); // Y axis (coronal)
                else
                    mat.SetInt("_SliceAxis", 0); // X axis (sagittal)
            }
        }
    }
    
    public void SetVolumeTexture(Texture3D texture)
    {
        volumeTexture = texture;
    }
    
    public void SetMinThreshold(float value)
    {
        minThreshold = Mathf.Clamp01(value);
    }
    
    public void SetMaxThreshold(float value)
    {
        maxThreshold = Mathf.Clamp01(value);
    }
    
    public void SetAxialSlicePosition(float value)
    {
        axialSlicePosition = Mathf.Clamp01(value);
    }
    
    public void SetCoronalSlicePosition(float value)
    {
        coronalSlicePosition = Mathf.Clamp01(value);
    }
    
    public void SetSagittalSlicePosition(float value)
    {
        sagittalSlicePosition = Mathf.Clamp01(value);
    }
}