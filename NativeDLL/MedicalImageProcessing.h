#pragma once

#ifdef _WIN32
    #ifdef MEDICAL_EXPORTS
        #define MEDICAL_API __declspec(dllexport)
    #else
        #define MEDICAL_API __declspec(dllimport)
    #endif
#else
    #define MEDICAL_API
#endif

extern "C" {

    // Generate 3D Perlin noise
    MEDICAL_API void GeneratePerlinNoise3D(
        float* outData,
        int width,
        int height,
        int depth,
        float scale,
        int octaves,
        float persistence
    );

    // Apply Gaussian blur to 3D volume data
    MEDICAL_API void ApplyGaussianBlur3D(
        float* inData,
        float* outData,
        int width,
        int height,
        int depth,
        float sigma
    );

    // Calculate histogram for thresholding
    MEDICAL_API void CalculateHistogram(
        float* data,
        int size,
        int* histogram,
        int bins,
        float minVal,
        float maxVal
    );

    // Apply threshold and generate binary mask
    MEDICAL_API void ApplyThreshold(
        float* inData,
        unsigned char* outMask,
        int size,
        float minThreshold,
        float maxThreshold
    );

} // extern "C"
