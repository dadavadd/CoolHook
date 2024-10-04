using System.Reflection;

namespace CoolHook.Hooking
{
    public interface IHook
    {
        MethodBase BaseMethod { get; }
        IntPtr BaseMethodPointer { get; }
        IntPtr HookMethodPointer { get; }

        void SetHook();
        void RemoveHook();
    }
}
