using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.System.Memory;

using static Windows.Win32.PInvoke;

namespace CoolHook
{
#pragma warning disable CA1416 // Проверка совместимости платформы
    public unsafe class Hook
    {
        public MethodBase BaseMethod { get; set; }

        public IntPtr BaseMethodPointer { get; set; }
        public IntPtr HookedMethodPointer { get; set; }

        private byte[] _origInstr;

#if WIN64
        private static readonly byte[] _hookInstr =
        {
            0x49, 0xBA,                                            // mov r10, [QWORD]
            0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA,        // placeholder for address
            0x41, 0xFF, 0xE2                                       // jmp r10
        };
#else
        private static readonly byte[] _hookInstr =
        {
            0xB8, 0xAA, 0xAA, 0xAA, 0xAA,                         // mov eax, [DWORD]
            0xFF, 0xE0                                            // jmp eax
        };
#endif

        public Hook(MethodBase baseMethod, MethodBase hookedMethod)
        {
            if (baseMethod == null | hookedMethod == null)
                throw new ArgumentException("One of the methods was null.");

            BaseMethod = baseMethod;

            RuntimeHelpers.PrepareMethod(baseMethod.MethodHandle);
            RuntimeHelpers.PrepareMethod(hookedMethod.MethodHandle);

            BaseMethodPointer = baseMethod.MethodHandle.GetFunctionPointer();
            HookedMethodPointer = hookedMethod.MethodHandle.GetFunctionPointer();

            _origInstr = new byte[_hookInstr.Length];

            SetHook();
        }

        public Hook((MethodBase, MethodBase) methodsToHook)
        {
            if (methodsToHook.Item1 == null | methodsToHook.Item2 == null)
                throw new ArgumentException("One of the methods was null.");

            BaseMethod = methodsToHook.Item1;

            RuntimeHelpers.PrepareMethod(methodsToHook.Item1.MethodHandle);
            RuntimeHelpers.PrepareMethod(methodsToHook.Item2.MethodHandle);

            BaseMethodPointer = methodsToHook.Item1.MethodHandle.GetFunctionPointer();
            HookedMethodPointer = methodsToHook.Item2.MethodHandle.GetFunctionPointer();

            _origInstr = new byte[_hookInstr.Length];

            SetHook();
        }

        public Hook(Type baseType, string baseMethodName, Type hookedType, string hookedMethodName, BindingFlags bindingFlags)
        {
            var baseMethod = baseType.GetMethod(baseMethodName, bindingFlags);
            var hookedMethod = hookedType.GetMethod(hookedMethodName, bindingFlags);

            if (baseMethod == null || hookedMethod == null)
                throw new ArgumentException("One or both methods were not found with the specified binding flags.");

            RuntimeHelpers.PrepareMethod(baseMethod.MethodHandle);
            RuntimeHelpers.PrepareMethod(hookedMethod.MethodHandle);

            BaseMethodPointer = baseMethod.MethodHandle.GetFunctionPointer();
            HookedMethodPointer = hookedMethod.MethodHandle.GetFunctionPointer();

            _origInstr = new byte[_hookInstr.Length];

            SetHook();
        }

        public Hook(IntPtr baseMethodPtr, IntPtr hookedMethodPtr)
        {
            if (baseMethodPtr == IntPtr.Zero | hookedMethodPtr == IntPtr.Zero)
                throw new ArgumentException("One of the methods was IntPtr.Zero.");

            var handle = RuntimeMethodHandle.FromIntPtr(baseMethodPtr);

            BaseMethod = MethodBase.GetMethodFromHandle(handle);

            BaseMethodPointer = baseMethodPtr;
            HookedMethodPointer = hookedMethodPtr;

            _origInstr = new byte[_hookInstr.Length];

            SetHook();
        }


        private void SetHook()
        {
            Marshal.Copy(BaseMethodPointer, _origInstr, 0, _hookInstr.Length);

            var hookInstructions = (byte[])_hookInstr.Clone();

#if WIN64
            Buffer.BlockCopy(BitConverter.GetBytes(HookedMethodPointer.ToInt64()), 0, hookInstructions, 2, 8);
#else
            Buffer.BlockCopy(BitConverter.GetBytes(HookedMethodPointer.ToInt32()), 0, hookInstructions, 1, 4);
#endif

            VirtualProtect(BaseMethodPointer.ToPointer(), (nuint)hookInstructions.Length, PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE, out var oldProtect);
            Marshal.Copy(hookInstructions, 0, BaseMethodPointer, hookInstructions.Length);
            VirtualProtect(BaseMethodPointer.ToPointer(), (nuint)hookInstructions.Length, oldProtect, out _);
        }


        public void RemoveHook()
        {
            VirtualProtect(BaseMethodPointer.ToPointer(), (nuint)_origInstr.Length, PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE, out var oldProtect);
            Marshal.Copy(_origInstr, 0, BaseMethodPointer, _origInstr.Length);
            VirtualProtect(BaseMethodPointer.ToPointer(), (nuint)_origInstr.Length, oldProtect, out _);
        }

    }

#pragma warning restore CA1416 // Проверка совместимости платформы
}
