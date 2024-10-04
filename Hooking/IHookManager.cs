using System.Reflection;


namespace CoolHook.Hooking
{
    public interface IHookManager
    {
        int EnabledHooks { get; }
        IHook CreateHook(MethodBase methodBase, MethodBase hookedMethod, string name = null);
        void RemoveAllHooks();
        void RemoveHook(IHook hook);
        IHook GetHookByName(string name);
        void RemoveHookByName(string name);
        bool HasHook(MethodBase methodBase);
        bool HasHook(nint methodBasePtr);
        List<IHook> GetHooksForMethodType(Type methodType);
    }
}
