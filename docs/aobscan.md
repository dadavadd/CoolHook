# How AobScan Works

## Overview

The AobScan library is designed to scan memory regions in a process to find byte patterns. It supports multiple scanning methods, including AVX2, SSE2, and a fallback method for pattern matching.

## Scanning Methods

### AVX2ScanMethod

Uses AVX2 SIMD instructions to perform fast and efficient pattern matching. Ideal for modern processors with AVX2 support.

### SSE2ScanMethod

Uses SSE2 SIMD instructions for pattern matching. Suitable for processors that support SSE2 but not AVX2.

### FallbackScanMethod

A basic scanning method that performs pattern matching without SIMD instructions. Used when neither AVX2 nor SSE2 are available.

## Usage

To use the AobScan library, you need to create an instance of `AoBScan` and provide a scan method. Then, call the `AobScan` method to search for a pattern in the memory of the target process.

Example:

```csharp
var process = Process.GetProcessById(1234); // Replace with the actual process ID
var scanMethod = new Avx2ScanMethod(); // Or Sse2ScanMethod, FallbackScanMethod
var aobScanner = new AoBScan(process, scanMethod);

var pattern = "48 89 5C 24 ?? 57 48 83 EC 20 48 8B F9 48 8B 07";
var results = await aobScanner.AobScan(pattern);
