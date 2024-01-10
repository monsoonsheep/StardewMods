#region Usings

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

#endregion

// ReSharper disable UnusedParameter.Local
// ReSharper disable InconsistentNaming

namespace BusSchedules.Patching;

internal class Patch
{
    internal Delegate _postfixMethod;
    internal Delegate _prefixMethod;
    internal Delegate _transpilerMethod;
    internal MethodInfo _targetMethod;

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
        foreach (var patch in Patches)
            harmony.Patch(
                patch._targetMethod,
                patch._prefixMethod == null ? null : new HarmonyMethod(GetType(), patch._prefixMethod.Method.Name),
                patch._postfixMethod == null ? null : new HarmonyMethod(GetType(), patch._postfixMethod.Method.Name),
                patch._transpilerMethod == null ? null : new HarmonyMethod(GetType(), patch._transpilerMethod.Method.Name)
            );
    }
}