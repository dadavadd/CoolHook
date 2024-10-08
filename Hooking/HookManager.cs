﻿using CoolHook.Logger;
using System.Reflection;

namespace CoolHook.Hooking
{
    /// <summary>
    /// Manages the creation, removal, and retrieval of method hooks.
    /// </summary>
    public class HookManager : IHookManager
    {
        private static readonly List<IHook> _hooks = new List<IHook>(); // List to store all hooks
        private static readonly Dictionary<string, IHook> _namedHooks = new Dictionary<string, IHook>(); // Dictionary to store named hooks

        /// <summary>
        /// Gets the count of currently enabled hooks.
        /// </summary>
        public int EnabledHooks => _hooks.Count;

        /// <summary>
        /// Creates a new hook for the specified methods.
        /// </summary>
        /// <param name="methodBase">The method to be hooked.</param>
        /// <param name="hookedMethod">The method to hook to.</param>
        /// <param name="name">Optional name for the hook.</param>
        /// <returns>The created hook.</returns>
        public IHook CreateHook(MethodBase methodBase, MethodBase hookedMethod, string name = null, ILogger logger = null)
        {
            var hook = new Hook(methodBase, hookedMethod, logger);
            _hooks.Add(hook);

            if (name != null)
                _namedHooks[name] = hook;

            return hook;
        }

        /// <summary>
        /// Removes all hooks and restores original methods.
        /// </summary>
        public void RemoveAllHooks()
        {
            foreach (var hook in _hooks)
                hook.RemoveHook();

            _hooks.Clear();
            _namedHooks.Clear();
        }

        /// <summary>
        /// Removes a specific hook.
        /// </summary>
        /// <param name="hook">The hook to remove.</param>
        public void RemoveHook(IHook hook)
        {
            if (_hooks.Contains(hook))
            {
                hook.RemoveHook();
                _hooks.Remove(hook);

                // Remove the hook from the named hooks dictionary
                foreach (var pair in _namedHooks)
                {
                    if (pair.Value == hook)
                    {
                        _namedHooks.Remove(pair.Key);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a hook by its name.
        /// </summary>
        /// <param name="name">The name of the hook.</param>
        /// <returns>The hook if found; otherwise, null.</returns>
        public IHook GetHookByName(string name)
        {
            _namedHooks.TryGetValue(name, out var hook);
            return hook;
        }

        /// <summary>
        /// Removes a hook by its name.
        /// </summary>
        /// <param name="name">The name of the hook.</param>
        public void RemoveHookByName(string name)
        {
            if (_namedHooks.TryGetValue(name, out var hook))
                RemoveHook(hook);
        }

        /// <summary>
        /// Checks if a hook exists for the specified method.
        /// </summary>
        /// <param name="methodBase">The method to check.</param>
        /// <returns>True if a hook exists for the method; otherwise, false.</returns>
        public bool HasHook(MethodBase methodBase)
        {
            var methodPtr = methodBase.MethodHandle.GetFunctionPointer();
            return _hooks.Exists(h => h.BaseMethodPointer == methodPtr);
        }

        /// <summary>
        /// Check if a hook exists for the specified method.
        /// </summary>
        /// <param name="methodBasePtr">The method pointer to check</param>
        /// <returns>True if a hook exists for the method; otherwise, false.</returns>

        public bool HasHook(nint methodBasePtr)
            => _hooks.Exists(h => h.BaseMethodPointer == methodBasePtr);

        /// <summary>
        /// Retrieves all hooks for methods of a specific type.
        /// </summary>
        /// <param name="methodType">The type of methods to retrieve hooks for.</param>
        /// <returns>A list of hooks associated with the specified method type.</returns>
        public List<IHook> GetHooksForMethodType(Type methodType)
            => _hooks.FindAll(h => methodType.IsAssignableFrom(h.BaseMethod.DeclaringType));
    }
}
