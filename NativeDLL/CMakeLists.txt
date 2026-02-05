cmake_minimum_required(VERSION 3.10)
project(MedicalImageProcessing)

set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_library(MedicalImageProcessing SHARED
    MedicalImageProcessing.cpp
    MedicalImageProcessing.h
)

target_compile_definitions(MedicalImageProcessing PRIVATE MEDICAL_EXPORTS)

# Put output DLL where Unity expects it (Windows)
set(UNITY_PLUGIN_DIR "${CMAKE_SOURCE_DIR}/../Assets/Plugins/x86_64")

set_target_properties(MedicalImageProcessing PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${UNITY_PLUGIN_DIR}"
    LIBRARY_OUTPUT_DIRECTORY "${UNITY_PLUGIN_DIR}"
    ARCHIVE_OUTPUT_DIRECTORY "${UNITY_PLUGIN_DIR}"
)
