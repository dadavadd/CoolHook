# ✔CoolHook

`CoolHook` is a library for managing method hooks in .NET applications. This library allows you to dynamically hook methods at runtime and replace them with custom implementations.

## Features

- **Create Hooks**: Dynamically hook methods at runtime.
- **Remove Hooks**: Easily remove hooks and restore original methods.
- **Manage Hooks**: Retrieve and remove hooks by name or method.

## Installation

To use `CoolHook`, add the library to your project via NuGet or by referencing the compiled assembly.

## Usage

### HookManager

The `HookManager` class manages the creation, removal, and retrieval of method hooks.

#### Properties

- **`EnabledHooks`**: Returns the number of currently enabled hooks.

#### Methods

- **`CreateHook(MethodBase methodBase, MethodBase hookedMethod, string name = null)`**: Creates a new hook for the specified methods. Optionally, associate a name with the hook.
- **`RemoveAllHooks()`**: Removes all hooks and restores original methods.
- **`RemoveHook(Hook hook)`**: Removes a specific hook.
- **`GetHookByName(string name)`**: Retrieves a hook by its name.
- **`RemoveHookByName(string name)`**: Removes a hook by its name.
- **`HasHook(MethodBase methodBase)`**: Checks if a hook exists for the specified method.
- **`GetHooksForMethodType(Type methodType)`**: Retrieves all hooks associated with a specific method type.

### Hook

The `Hook` class represents a single method hook.

#### Properties

- **`BaseMethod`**: The method being hooked.
- **`BaseMethodPointer`**: Pointer to the base method.
- **`HookedMethodPointer`**: Pointer to the hooked method.

#### Constructors

- **`Hook(MethodBase baseMethod, MethodBase hookedMethod)`**: Creates a hook for the specified methods.
- **`Hook((MethodBase, MethodBase) methodsToHook)`**: Creates a hook from a tuple of methods.
- **`Hook(Type baseType, string baseMethodName, Type hookedType, string hookedMethodName, BindingFlags bindingFlags)`**: Creates a hook using method names and types.
- **`Hook(IntPtr baseMethodPtr, IntPtr hookedMethodPtr)`**: Creates a hook using method pointers.

#### Methods

- **`SetHook()`**: Applies the hook to the base method.
- **`RemoveHook()`**: Removes the hook and restores the original method.

## Example

Here is an example demonstrating how to use `CoolHook`:

```csharp
using CoolHook;
using System.Reflection;

class Program
{
    static void Main()
    {
        // Create a HookManager instance
        HookManager hookManager = new HookManager();

        // Get methods to hook
        var methods = GetMethodsToHook();

        // Create a hook
        hookManager.CreateHook(methods.baseMethod, methods.hookMethod);

        // Verify hooks
        Console.WriteLine("Method Hooked!");
        Console.WriteLine(hookManager.GetHooksForMethodType(typeof(Program)).Count);

        // Call hooked methods
        BaseMethod(); // Outputs: "it's hook method!"
        HookMethod(); // Outputs: "it's hook method!"

        // Remove all hooks
        hookManager.RemoveAllHooks();
        Console.WriteLine("Removed all method hooks!");

        // Call original methods
        BaseMethod(); // Outputs: "It's original method!"
        HookMethod(); // Outputs: "It's hook method!"
    }

    static (MethodBase baseMethod, MethodBase hookMethod) GetMethodsToHook()
    {
        return (typeof(Program).GetMethod("BaseMethod", BindingFlags.Static | BindingFlags.NonPublic),
                typeof(Program).GetMethod("HookMethod", BindingFlags.Static | BindingFlags.NonPublic));
    }

    static void BaseMethod()
    {
        Console.WriteLine("It's original method!");
    }

    static void HookMethod()
    {
        Console.WriteLine("It's hook method!");
    }
}
