# Reading Memory

## Overview

This document explains how to read memory from a process using the AobScan library.

## Reading Values

To read a value from a specific memory address, use the `ReadMemory` extension method provided in the `MemoryReaderExtensions` class. This method supports various data types including `int`, `long`, `double`, and custom structures.

Example:

```csharp
var value = process.ReadMemory<int>(/*IntPtr*/address);
Console.WriteLine(value);
