using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

// ReSharper disable UnusedParameter.Local
// ReSharper disable InconsistentNaming

namespace CollapseOnFarmFix.Framework.Patching
{
    internal class Patch
    {
        internal MethodInfo _targetMethod;
        internal string _prefixMethod;
        internal string _postfixMethod;
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
            foreach (Patch patch in Patches)
            {
                harmony.Patch(
                    original: patch._targetMethod,
                    prefix: patch._prefixMethod == null ? null : new HarmonyMethod(GetType(), patch._prefixMethod),
                    postfix: patch._postfixMethod == null ? null : new HarmonyMethod(GetType(), patch._postfixMethod),
                    transpiler: patch._transpilerMethod == null ? null : new HarmonyMethod(GetType(), patch._transpilerMethod)
                );
            }
        }
    }
}
