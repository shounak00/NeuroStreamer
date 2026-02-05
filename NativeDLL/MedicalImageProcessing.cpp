#include "MedicalImageProcessing.h"
#include <cmath>
#include <algorithm>
#include <vector>

#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

// Simple hash function for Perlin noise
static inline float hash3(int x, int y, int z) {
    int n = x + y * 57 + z * 997;
    n = (n << 13) ^ n;
    return (1.0f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0f);
}

// Interpolation function
static inline float lerp1(float a, float b, float t) {
    return a + t * (b - a);
}

// Smoothstep function
static inline float smoothstep1(float t) {
    return t * t * (3.0f - 2.0f * t);
}

// 3D Perlin noise implementation
static float perlin3D(float x, float y, float z) {
    int xi = (int)floorf(x);
    int yi = (int)floorf(y);
    int zi = (int)floorf(z);

    float xf = x - (float)xi;
    float yf = y - (float)yi;
    float zf = z - (float)zi;

    float u = smoothstep1(xf);
    float v = smoothstep1(yf);
    float w = smoothstep1(zf);

    // Get corner values
    float c000 = hash3(xi,     yi,     zi);
    float c100 = hash3(xi + 1, yi,     zi);
    float c010 = hash3(xi,     yi + 1, zi);
    float c110 = hash3(xi + 1, yi + 1, zi);
    float c001 = hash3(xi,     yi,     zi + 1);
    float c101 = hash3(xi + 1, yi,     zi + 1);
    float c011 = hash3(xi,     yi + 1, zi + 1);
    float c111 = hash3(xi + 1, yi + 1, zi + 1);

    // Interpolate
    float x00 = lerp1(c000, c100, u);
    float x10 = lerp1(c010, c110, u);
    float x01 = lerp1(c001, c101, u);
    float x11 = lerp1(c011, c111, u);

    float y0 = lerp1(x00, x10, v);
    float y1 = lerp1(x01, x11, v);

    return lerp1(y0, y1, w);
}

extern "C" {

void GeneratePerlinNoise3D(
    float* outData,
    int width,
    int height,
    int depth,
    float scale,
    int octaves,
    float persistence
) {
    if (!outData || width <= 0 || height <= 0 || depth <= 0 || octaves <= 0) return;

    for (int z = 0; z < depth; z++) {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {

                float total = 0.0f;
                float frequency = scale;
                float amplitude = 1.0f;
                float maxValue = 0.0f;

                for (int i = 0; i < octaves; i++) {
                    total += perlin3D(
                        (float)x * frequency / (float)width,
                        (float)y * frequency / (float)height,
                        (float)z * frequency / (float)depth
                    ) * amplitude;

                    maxValue += amplitude;
                    amplitude *= persistence;
                    frequency *= 2.0f;
                }

                int index = x + y * width + z * width * height;
                outData[index] = (total / maxValue + 1.0f) * 0.5f; // Normalize to [0,1]
            }
        }
    }
}

void ApplyGaussianBlur3D(
    float* inData,
    float* outData,
    int width,
    int height,
    int depth,
    float sigma
) {
    if (!inData || !outData || width <= 0 || height <= 0 || depth <= 0) return;
    if (sigma <= 0.0f) {
        // If sigma is 0, just copy input to output
        std::copy(inData, inData + (width * height * depth), outData);
        return;
    }

    int kernelSize = (int)(sigma * 3.0f) * 2 + 1;
    if (kernelSize < 3) kernelSize = 3;
    int halfKernel = kernelSize / 2;

    // Create Gaussian kernel
    std::vector<float> kernel(kernelSize);
    float sum = 0.0f;

    for (int i = 0; i < kernelSize; i++) {
        float x = (float)(i - halfKernel);
        kernel[i] = expf(-(x * x) / (2.0f * sigma * sigma));
        sum += kernel[i];
    }

    // Normalize kernel
    for (int i = 0; i < kernelSize; i++) {
        kernel[i] /= sum;
    }

    int total = width * height * depth;
    std::vector<float> tempData(total);
    std::vector<float> tempData2(total);

    // Blur along X axis
    for (int z = 0; z < depth; z++) {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float value = 0.0f;
                for (int k = -halfKernel; k <= halfKernel; k++) {
                    int sx = std::max(0, std::min(width - 1, x + k));
                    int idx = sx + y * width + z * width * height;
                    value += inData[idx] * kernel[k + halfKernel];
                }
                int index = x + y * width + z * width * height;
                tempData[index] = value;
            }
        }
    }

    // Blur along Y axis
    for (int z = 0; z < depth; z++) {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float value = 0.0f;
                for (int k = -halfKernel; k <= halfKernel; k++) {
                    int sy = std::max(0, std::min(height - 1, y + k));
                    int idx = x + sy * width + z * width * height;
                    value += tempData[idx] * kernel[k + halfKernel];
                }
                int index = x + y * width + z * width * height;
                tempData2[index] = value;
            }
        }
    }

    // Blur along Z axis
    for (int z = 0; z < depth; z++) {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float value = 0.0f;
                for (int k = -halfKernel; k <= halfKernel; k++) {
                    int sz = std::max(0, std::min(depth - 1, z + k));
                    int idx = x + y * width + sz * width * height;
                    value += tempData2[idx] * kernel[k + halfKernel];
                }
                int index = x + y * width + z * width * height;
                outData[index] = value;
            }
        }
    }
}

void CalculateHistogram(
    float* data,
    int size,
    int* histogram,
    int bins,
    float minVal,
    float maxVal
) {
    if (!data || !histogram || size <= 0 || bins <= 0) return;

    for (int i = 0; i < bins; i++) histogram[i] = 0;

    float range = maxVal - minVal;
    if (range <= 0.0f) return;

    for (int i = 0; i < size; i++) {
        float value = data[i];
        if (value >= minVal && value <= maxVal) {
            int bin = (int)(((value - minVal) / range) * (bins - 1));
            bin = std::max(0, std::min(bins - 1, bin));
            histogram[bin]++;
        }
    }
}

void ApplyThreshold(
    float* inData,
    unsigned char* outMask,
    int size,
    float minThreshold,
    float maxThreshold
) {
    if (!inData || !outMask || size <= 0) return;

    for (int i = 0; i < size; i++) {
        float value = inData[i];
        outMask[i] = (value >= minThreshold && value <= maxThreshold) ? 255 : 0;
    }
}

} // extern "C"
