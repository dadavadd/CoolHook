using CoolHook.Logger;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.System.Memory;

using static Windows.Win32.PInvoke;

namespace CoolHook.Hooking
{
#pragma warning disable CA1416 // Checks for platform compatibility
    /// <summary>
    /// Represents a single method hook.
    /// </summary>
    public unsafe class Hook : IHook
    {
        public MethodBase BaseMethod { get; internal set; } // The method being hooked
        public IntPtr BaseMethodPointer { get; internal set; } // Pointer to the base method
        public IntPtr HookMethodPointer { get; internal set; } // Pointer to the hooked method

        private byte[] _origInstr; // Stores the original instructions of the base method

        private readonly ILogger _logger;

        // Hook instructions for different platforms
#if WIN64
        private static readonly byte[] _hookInstr =
        {
            0x49, 0xBA,                                            // mov r10, [QWORD]
            0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA,        // Placeholder for address
            0x41, 0xFF, 0xE2                                       // jmp r10
        };
#else
        private static readonly byte[] _hookInstr =
        {
            0xB8, 0xAA, 0xAA, 0xAA, 0xAA,                         // mov eax, [DWORD]
            0xFF, 0xE0                                            // jmp eax
        };
#endif

        /// <summary>
        /// Constructs a hook from base and hooked methods.
        /// </summary>
        /// <param name="baseMethod">The method to be hooked.</param>
        /// <param name="hookedMethod">The method to hook to.</param>
        /// <exception cref="ArgumentException">Thrown when either method is null.</exception>
        public Hook(MethodBase baseMethod, MethodBase hookedMethod, ILogger logger = null)
        {
            _logger = logger;
            _logger?.Log("Creating hook for base method: " + baseMethod.Name);

            if (baseMethod == null || hookedMethod == null)
            {
                _logger?.LogError("One of the methods is null.");
                throw new ArgumentException("One of the methods was null.");
            }

            BaseMethod = baseMethod;

            var baseMethodHandle = baseMethod.MethodHandle;
            var hookedMethodHandle = hookedMethod.MethodHandle;

            RuntimeHelpers.PrepareMethod(baseMethodHandle);
            RuntimeHelpers.PrepareMethod(hookedMethodHandle);

            BaseMethodPointer = baseMethodHandle.GetFunctionPointer();
            HookMethodPointer = hookedMethodHandle.GetFunctionPointer();

            _origInstr = new byte[_hookInstr.Length];

            SetHook();
        }

        /// <summary>
        /// Constructs a hook from a tuple of methods.
        /// </summary>
        /// <param name="methodsToHook">Tuple containing the base and hooked methods.</param>
        /// <exception cref="ArgumentException">Thrown when either method is null.</exception>
        public Hook((MethodBase, MethodBase) methodsToHook, ILogger logger = null)
            : this(methodsToHook.Item1, methodsToHook.Item2, logger)
        {
        }

        /// <summary>
        /// Constructs a hook using method names and types.
        /// </summary>
        /// <param name="baseType">The type containing the base method.</param>
        /// <param name="baseMethodName">The name of the base method.</param>
        /// <param name="hookedType">The type containing the hooked method.</param>
        /// <param name="hookedMethodName">The name of the hooked method.</param>
        /// <param name="bindingFlags">Binding flags used to find the methods.</param>
        /// <exception cref="ArgumentException">Thrown when methods are not found with the specified binding flags.</exception>
        public Hook(Type baseType, string baseMethodName, Type hookedType, string hookedMethodName, BindingFlags bindingFlags, ILogger logger = null)
            : this(
                baseType.GetMethod(baseMethodName, bindingFlags) ?? throw new ArgumentException("Base method not found with the specified binding flags."),
                hookedType.GetMethod(hookedMethodName, bindingFlags) ?? throw new ArgumentException("Hooked method not found with the specified binding flags."),
                logger)
        {
        }

        /// <summary>
        /// Constructs a hook using method pointers.
        /// </summary>
        /// <param name="baseMethodPtr">Pointer to the base method.</param>
        /// <param name="hookedMethodPtr">Pointer to the hooked method.</param>
        /// <exception cref="ArgumentException">Thrown when pointers are IntPtr.Zero.</exception>

        public Hook(IntPtr baseMethodPtr, IntPtr hookedMethodPtr, ILogger logger = null)
        {
            _logger = logger;
            _logger?.Log("Creating hook using method pointers.");

            if (baseMethodPtr == IntPtr.Zero || hookedMethodPtr == IntPtr.Zero)
            {
                _logger?.LogError("One of the method pointers is IntPtr.Zero.");
                throw new ArgumentException("One of the methods was IntPtr.Zero.");
            }

            BaseMethod = MethodBase.GetMethodFromHandle(RuntimeMethodHandle.FromIntPtr(baseMethodPtr));

            BaseMethodPointer = baseMethodPtr;
            HookMethodPointer = hookedMethodPtr;

            _origInstr = new byte[_hookInstr.Length];

            SetHook();
        }

        /// <summary>
        /// Applies the hook by modifying the base method's instructions.
        /// </summary>
        public void SetHook()
        {
            _logger?.Log("Setting hook.");
            Marshal.Copy(BaseMethodPointer, _origInstr, 0, _hookInstr.Length);

            var hookInstructions = (byte[])_hookInstr.Clone();

#if WIN64
            Buffer.BlockCopy(BitConverter.GetBytes(HookMethodPointer.ToInt64()), 0, hookInstructions, 2, 8);
#else
            Buffer.BlockCopy(BitConverter.GetBytes(HookMethodPointer.ToInt32()), 0, hookInstructions, 1, 4);
#endif

            VirtualProtect(BaseMethodPointer.ToPointer(), (nuint)hookInstructions.Length, PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE, out var oldProtect);
            Marshal.Copy(hookInstructions, 0, BaseMethodPointer, hookInstructions.Length);
            VirtualProtect(BaseMethodPointer.ToPointer(), (nuint)hookInstructions.Length, oldProtect, out _);

            _logger?.Log("Hook set successfully.");
        }

        /// <summary>
        /// Removes the hook and restores the original method instructions.
        /// </summary>
        public void RemoveHook()
        {
            _logger?.Log("Removing hook.");
            VirtualProtect(BaseMethodPointer.ToPointer(), (nuint)_origInstr.Length, PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE, out var oldProtect);
            Marshal.Copy(_origInstr, 0, BaseMethodPointer, _origInstr.Length);
            VirtualProtect(BaseMethodPointer.ToPointer(), (nuint)_origInstr.Length, oldProtect, out _);

            _logger?.Log("Hook removed successfully.");
        }

        /// <summary>
        /// Overriding the ToString() method.
        /// </summary>
        /// <returns>Show the addresses of the original methods and hooked ones as string type</returns>
        public override string ToString()
        {
            _logger?.Log("Converting hook to string.");
            return $"Original method pointer: {BaseMethodPointer:X}\nHook method pointer: {HookMethodPointer:X}";
        }
    }
#pragma warning restore CA1416 // Checks for platform compatibility
}
