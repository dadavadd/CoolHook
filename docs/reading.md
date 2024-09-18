# Reading Memory

## Overview

This document explains how to read memory from a process using the AobScan library.

## Reading Values

To read a value from a specific memory address, use the `ReadMemory` extension method provided in the `MemoryReaderExtensions` class. This method supports various data types including `int`, `long`, `double`, and custom structures.

Example:

```csharp
var value = process.ReadMemory<int>(/*IntPtr*/address);
Console.WriteLine(value);
```

```csharp
var value = aobscan.ReadMemory<int>(/*IntPtr*/address);
Console.WriteLine(value);
```

# Reading Strings

To read a null-terminated `string` from memory, use the `ReadString` extension method. This method retrieves a `string` of the specified length from the memory address.
```csharp
var str = process.ReadString(readAddress, 256);
Console.WriteLine(str);
```

```csharp
var str = aobscan.ReadString(readAddress, 256);
Console.WriteLine(str);
```
