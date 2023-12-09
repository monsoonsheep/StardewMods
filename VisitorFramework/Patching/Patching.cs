#region Usings

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

#endregion

// ReSharper disable UnusedParameter.Local
// ReSharper disable InconsistentNaming

namespace VisitorFramework.Patching;

internal class Patch
{
    internal string _postfixMethod;
    internal string _prefixMethod;
    internal MethodInfo _targetMethod;
    internal string _transpilerMethod;

    public Patch(Type targetType,
        string targetMethodName,
        Type[] arguments,
        string prefix = null,
        string postfix = null,
        string transpiler = null)
    {
        _targetMethod = AccessTools.Method(targetType, targetMethodName, arguments);
        _prefixMethod = prefix;
        _postfixMethod = postfix;
        _transpilerMethod = transpiler;
    }
}

internal abstract class PatchList
{
    internal List<Patch> Patches;

    internal virtual void ApplyAll(Harmony harmony)
    {
        foreach (var patch in Patches)
            harmony.Patch(
                patch._targetMethod,
                patch._prefixMethod == null ? null : new HarmonyMethod(GetType(), patch._prefixMethod),
                patch._postfixMethod == null ? null : new HarmonyMethod(GetType(), patch._postfixMethod),
                patch._transpilerMethod == null ? null : new HarmonyMethod(GetType(), patch._transpilerMethod)
            );
    }
}