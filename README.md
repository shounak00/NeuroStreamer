# Neuro-Streamer: Medical Volume Visualizer

A real-time 3D medical imaging tool built in Unity that visualizes volumetric data like CT and MRI scans. Built to explore volumetric rendering techniques, C++/C# integration, and medical software UI design.

![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black?style=flat&logo=unity)
![C++](https://img.shields.io/badge/C%2B%2B-17-blue)
![C#](https://img.shields.io/badge/C%23-10.0-purple)

---

## What It Does

This tool lets you explore 3D medical scans in real-time. You can:
- Rotate and zoom around the volume
- Slice through it from any angle (axial, coronal, sagittal)
- Filter by tissue density to isolate specific structures
- See simulated brain tissue, skull, and even a tumor

Think of it as a simplified version of software radiologists use, built from scratch to experiment with medical visualization techniques.

---

## Key Features

- **Volume Rendering**: Uses ray-marching shaders for true 3D visualization (not just stacked 2D slices)
- **Interactive Slicing**: Cut through the volume on three different planes
- **Threshold Controls**: Filter tissues by density to highlight bones, soft tissue, or abnormalities
- **C++ Integration**: Performance-critical operations run in native C++ for speed
- **Medical UI**: Clean, professional interface with real-time performance metrics

---

## Tech Stack

- **Unity 6.3 LTS** (or Unity 2021.3+)
- **C++** for compute-intensive operations (noise generation, Gaussian blur)
- **HLSL Shaders** for volume ray-marching
- **C# Scripts** for application logic and UI
- **P/Invoke** for C++/C# communication

---

## Quick Start

### Prerequisites
- Unity 2021.3 LTS or newer (including Unity 6)
- Windows, Mac, or Linux
- GPU that supports DirectX 11 or OpenGL 4.5

### Installation

1. **Clone the repo**:
   ```bash
   git clone https://github.com/shounak00/NeuroStreamer.git
   cd NeuroStreamer
   ```

2. **Open in Unity**:
   - Launch Unity Hub
   - Click "Add" → Select the project folder
   - Open the project

3. **Open the main scene**:
   - Navigate to `Assets/Scenes/MainScene.unity`
   - Double-click to open

4. **Press Play** ▶️

That's it! The volume should generate automatically and you can start exploring.

---

## Controls

### Mouse Controls
- **Right Mouse + Drag**: Rotate camera around the volume
- **Middle Mouse + Drag**: Pan the camera
- **Mouse Wheel**: Zoom in/out

### Keyboard Shortcuts
- **Space**: Auto-rotate the volume
- **R**: Reset camera to default position
- **F**: Focus camera on center
- **1**: Front view
- **2**: Right side view
- **3**: Top view
- **4**: Perspective view

### UI Controls
- **Min/Max Threshold Sliders**: Filter by tissue density (low = soft tissue, high = bone)
- **Slice Position Sliders**: Move the slice planes through the volume
- **Visibility Toggles**: Show/hide the volume and individual slice planes

---

## How It Works

### Volume Rendering
The project uses **ray-marching** to render 3D volumes. For each pixel on screen:
1. Cast a ray from the camera
2. March along the ray in small steps
3. Sample the 3D texture at each step
4. Apply color based on density (transfer function)
5. Accumulate the colors using alpha blending

This creates a translucent view where you can see through layers of tissue.

### C++ Integration
Heavy computations run in a C++ DLL:
- **3D Perlin Noise**: Generates realistic-looking medical data
- **Gaussian Blur**: Smooths the volume for better visualization
- **Histogram Calculation**: Analyzes density distribution

This is about 5x faster than doing it in pure C#.

### Transfer Function
Maps density values to colors to simulate CT/MRI appearance:
- **0.0-0.3**: Dark blue-black (CSF/ventricles)
- **0.3-0.5**: Gray (brain tissue)
- **0.5-0.7**: Light gray (white matter)
- **0.7-0.85**: White/cream (bone)
- **0.85-1.0**: Red-orange (tumor/abnormality)

---

## Project Structure

```
NeuroStreamer/
├── Assets/
│   ├── Scripts/
│   │   ├── VolumeDataGenerator.cs      # Generates or imports volume data
│   │   ├── VolumeRenderer.cs            # Handles rendering
│   │   ├── UIController.cs              # Manages UI
│   │   ├── MedicalCameraController.cs   # Camera controls
│   │   └── NativeInterop.cs             # C++ interface
│   ├── Shaders/
│   │   ├── VolumeRaymarching.shader     # Main volume rendering
│   │   └── SliceView.shader             # 2D slice rendering
│   ├── Materials/
│   ├── Plugins/
│   │   └── x86_64/
│   │       └── MedicalImageProcessing.dll
│   └── Scenes/
└── NativeDLL/
    ├── MedicalImageProcessing.cpp
    └── MedicalImageProcessing.h
```

---

## Building the C++ DLL

### Windows (Visual Studio)

1. Open Visual Studio → New Project → DLL
2. Add `MedicalImageProcessing.cpp` and `.h`
3. Project Properties:
   - Platform: x64
   - Configuration: Release
   - Preprocessor: Add `MEDICAL_EXPORTS`
4. Build (Ctrl+Shift+B)
5. Copy DLL to `Assets/Plugins/x86_64/`

### Alternative: Use CMake

```bash
cd NativeDLL
mkdir build && cd build
cmake ..
cmake --build . --config Release
```

Then copy the compiled DLL to Unity's Plugins folder.

---

## Importing Your Own Data

Want to visualize real medical scans?

1. Export your data as **RAW binary** from:
   - 3D Slicer
   - ImageJ/Fiji
   - MATLAB

2. In Unity:
   - Select `VolumeSystem`
   - Change `Data Source` to `ImportRawFile`
   - Drag your RAW file to `Raw Data Asset`
   - Set the correct dimensions (width × height × depth)
   - Press Play

### Free Medical Datasets
- [The Cancer Imaging Archive](https://www.cancerimagingarchive.net/)
- [BrainWeb](https://brainweb.bic.mni.mcgill.ca/)
- [OASIS Brain Database](https://www.oasis-brains.org/)

---

## Performance

Tested on Intel i7-9700K + RTX 2060:

| Volume Size | FPS | Memory |
|-------------|-----|--------|
| 64³ | 120+ | 18 MB |
| 128³ | 60-80 | 45 MB |
| 256³ | 30-45 | 180 MB |

Tips for better performance:
- Use 64³ or 128³ for testing
- Increase step size (makes it faster but less detailed)
- Disable slice planes when not needed

---

## Troubleshooting

**Volume not showing?**
- Set Min Threshold to 0.0
- Increase Density Multiplier to 2.5
- Check that materials are assigned in VolumeRenderer

**DLL not found?**
- Make sure it's in `Assets/Plugins/x86_64/` (Windows)
- Check the filename is exactly `MedicalImageProcessing.dll`
- Restart Unity

**Low FPS?**
- Reduce volume size to 64³
- Increase step size to 0.02
- Check GPU supports DirectX 11

**Input System errors?**
- Edit → Project Settings → Player
- Active Input Handling → Set to "Both"
- Restart Unity

---

## What I Learned

Building this taught me:
- How medical imaging software actually works
- Ray-marching and volumetric rendering techniques
- C++/C# interoperability for performance optimization
- Medical UI/UX patterns
- Shader programming in HLSL

The hardest part was getting the ray-marching shader right. Medical volumes need special handling because you're looking at translucent data, not solid objects.

---

## Future Ideas

Things I'd add with more time:
- DICOM file import (the medical imaging standard)
- Measurements and annotations
- Window/level controls (like real medical viewers)
- VR support for immersive viewing
- AI-powered tissue segmentation

---

## License

MIT License - feel free to use this for learning or your own projects.

---

## Author

Built by **Shounak Sobahani**

- LinkedIn: [linkedin.com/in/shounak00](https://www.linkedin.com/in/shounak00/)
- GitHub: [@shounak00](https://github.com/shounak00)
- Email: shounak00@gmail.com

---

## Acknowledgments

- Medical imaging community for datasets and reference materials
- Unity Technologies for the engine
- Everyone who provided feedback during development

---

**Made with ☕ while experimenting with volumetric rendering**

If you find this useful, give it a star! ⭐