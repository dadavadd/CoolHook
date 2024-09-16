using System.Reflection;

namespace CoolHook
{
    public class HookManager
    {
        private static readonly List<Hook> _hooks = [];
        private static readonly Dictionary<string, Hook> _namedHooks = [];
        public int EnabledHooks => _hooks.Count;

        public Hook CreateHook(MethodBase methodBase, MethodBase hookedMethod, string name = null)
        {
            var hook = new Hook(methodBase, hookedMethod);
            _hooks.Add(hook);

            if (name != null)
                _namedHooks[name] = hook;

            return hook;
        }

        public Hook CreateHook(IntPtr methodBase, IntPtr hookedMethod, string name = null)
        {
            var hook = new Hook(methodBase, hookedMethod);
            _hooks.Add(hook);

            if (name != null)
                _namedHooks[name] = hook;

            return hook;
        }

        public Hook CreateHook((MethodBase, MethodBase) methods, string name = null)
        {
            var hook = new Hook(methods);
            _hooks.Add(hook);

            if (name != null)
                _namedHooks[name] = hook;

            return hook;
        }

        public Hook CreateHook(Type baseType, string baseMethodName, Type hookedType, string hookedMethodName, BindingFlags bindingFlags, string name = null)
        {
            var hook = new Hook(baseType, baseMethodName, hookedType, hookedMethodName, bindingFlags);
            _hooks.Add(hook);

            if (name != null)
                _namedHooks[name] = hook;

            return hook;
        }


        public void RemoveAllHooks()
        {
            foreach (var hook in _hooks)
                hook.RemoveHook();

            _hooks.Clear();
            _namedHooks.Clear();
        }

        public void RemoveHook(Hook hook)
        {
            if (_hooks.Contains(hook))
            {
                hook.RemoveHook();
                _hooks.Remove(hook);

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

        public Hook GetHookByName(string name)
        {
            _namedHooks.TryGetValue(name, out var hook);
            return hook;
        }

        public void RemoveHookByName(string name)
        {
            if (_namedHooks.TryGetValue(name, out var hook))
                RemoveHook(hook);
        }

        public bool HasHook(MethodBase methodBase)
        {
            var methodPtr = methodBase.MethodHandle.GetFunctionPointer();
            return _hooks.Exists(h => h.BaseMethodPointer == methodPtr);
        }

        public List<Hook> GetHooksForMethodType(Type methodType)
        {
            return _hooks.FindAll(h => methodType.IsAssignableFrom(h.BaseMethod.DeclaringType));
        }
    }
}
