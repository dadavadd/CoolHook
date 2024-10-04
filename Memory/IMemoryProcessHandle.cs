using System.Runtime.InteropServices;

namespace CoolHook.Memory
{
    public interface IMemoryProcessHandle
    {
        SafeHandle ProcessHandle { get; }
    }
}
