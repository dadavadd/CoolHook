### DLL Inject Documentation
This section describes how to use the DllInjector class to inject a DLL into a target process.

The following example demonstrates how to inject a DLL into a running process using the DllInjector.InjectDLL method:

```csharp
using ProcessHandler;
using CoolHook.Memory.Injector;

Process process = new Process("process.exe");
DllInjector.InjectDLL(process, "dll path");
```