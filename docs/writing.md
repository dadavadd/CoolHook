# Writing Memory

## Overview

This document explains how to write to memory in a process using the AobScan library.

## Writing Values

To write a value to a specific memory address, use the `WriteMemory` extension method provided in the `MemoryPatcherExtensions` class. This method supports various data types including `int`, `long`, `double`, and byte arrays.

Example:

```csharp
process.WriteMemory(writeAddress, 12345); // Write an integer value
```

```csharp
aobscan.WriteMemory(writeAddress, 12345); // Write an integer value
```

# Writing Byte Arrays

To write raw byte data to a memory address, use the same WriteMemory method but pass a byte array as the data.

```csharp
byte[] data = { 0x90, 0x90, 0x90 }; // NOP instructions
process.WriteMemory(writeAddress, data);
```

```csharp
byte[] data = { 0x90, 0x90, 0x90 }; // NOP instructions
aobscan.WriteMemory(writeAddress, data);
```
