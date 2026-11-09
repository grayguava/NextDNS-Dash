using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

// Windows Hello through the WinRT UserConsentVerifier. The project builds with
// the C# 5 compiler, which cannot reference Windows.winmd, so the projected
// type and the AsTask helper are resolved at run time instead.
static class WindowsHello
{
    static readonly object gate = new object();
    static bool resolved;
    static Type verifier;
    static Type extensions;
    static bool available;
    static string availability = "Unavailable";

    public static string Availability { get { return availability; } }

    public static void Recheck()
    {
        lock (gate)
        {
            resolved = false;
            verifier = null;
            extensions = null;
            available = false;
            availability = "Unavailable";
        }
    }

    public static bool IsAvailable
    {
        get
        {
            lock (gate)
            {
                Resolve();
                return available;
            }
        }
    }

    public static bool Verify(string message)
    {
        Type type;
        Type ext;

        lock (gate)
        {
            Resolve();
            if (!available) return false;
            type = verifier;
            ext = extensions;
        }

        try
        {
            MethodInfo method = type.GetMethod("RequestVerificationAsync");
            object operation = method.Invoke(null, new object[] { message });
            return Await(ext, operation, method.ReturnType).ToString() == "Verified";
        }
        catch (Exception)
        {
            return false;
        }
    }

    static void Resolve()
    {
        if (resolved) return;
        resolved = true;

        try
        {
            string runtime = Path.Combine(
                RuntimeEnvironment.GetRuntimeDirectory(),
                "System.Runtime.WindowsRuntime.dll");
            Assembly assembly = Assembly.LoadFrom(runtime);
            extensions = assembly.GetType("System.WindowsRuntimeSystemExtensions");
            verifier = Type.GetType(
                "Windows.Security.Credentials.UI.UserConsentVerifier, Windows, ContentType=WindowsRuntime");

            if (extensions == null || verifier == null) return;

            MethodInfo method = verifier.GetMethod("CheckAvailabilityAsync");
            availability = Await(extensions, method.Invoke(null, null), method.ReturnType).ToString();
            available = availability == "Available";
        }
        catch (Exception)
        {
            availability = "Unavailable";
            available = false;
        }
    }

    static object Await(Type ext, object operation, Type operationType)
    {
        Type resultType = operationType.GetGenericArguments()[0];
        MethodInfo asTask = null;

        foreach (MethodInfo method in ext.GetMethods())
        {
            if (method.Name != "AsTask" || !method.IsGenericMethodDefinition) continue;
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType.IsGenericType &&
                parameters[0].ParameterType.GetGenericTypeDefinition().FullName ==
                    "Windows.Foundation.IAsyncOperation`1")
            {
                asTask = method.MakeGenericMethod(resultType);
                break;
            }
        }

        if (asTask == null) throw new InvalidOperationException("AsTask is missing");

        object task = asTask.Invoke(null, new object[] { operation });
        ((Task)task).Wait();
        return task.GetType().GetProperty("Result").GetValue(task, null);
    }
}
