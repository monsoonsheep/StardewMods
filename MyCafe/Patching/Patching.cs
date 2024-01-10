using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

// ReSharper disable UnusedParameter.Local
// ReSharper disable InconsistentNaming

namespace MyCafe.Patching;
internal class Patch
{
    internal MethodInfo _targetMethod;
    internal Delegate _prefixMethod;
    internal Delegate _postfixMethod;
    internal Delegate _transpilerMethod;

    public Patch(Type targetType,
        string targetMethodName,
        Type[] arguments,
        Delegate prefix = null,
        Delegate postfix = null,
        Delegate transpiler = null)
    {
        _targetMethod = AccessTools.Method(targetType, targetMethodName, arguments);
        _prefixMethod = prefix;
        _postfixMethod = postfix;
        _transpilerMethod = transpiler;
    }
}

internal abstract class PatchCollection
{
    internal List<Patch> Patches;
    internal virtual void ApplyAll(Harmony harmony)
    {
        foreach (Patch patch in Patches)
        {
            harmony.Patch(
                original: patch._targetMethod,
                prefix: patch._prefixMethod == null ? null : new HarmonyMethod(GetType(), patch._prefixMethod.Method.Name),
                postfix: patch._postfixMethod == null ? null : new HarmonyMethod(GetType(), patch._postfixMethod.Method.Name),
                transpiler: patch._transpilerMethod == null ? null : new HarmonyMethod(GetType(), patch._transpilerMethod.Method.Name)
            );
        }
    }
}