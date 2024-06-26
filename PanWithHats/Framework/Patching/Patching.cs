using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Reflection;

// ReSharper disable UnusedParameter.Local
// ReSharper disable InconsistentNaming

namespace PanWithHats.Framework.Patching
{
    internal class Patch
    {
        internal MethodInfo? _targetMethod;
        internal string? _prefixMethod;
        internal string? _postfixMethod;
        internal string? _transpilerMethod;

        public Patch(Type targetType,
            string targetMethodName,
            Type[]? arguments,
            string? prefix = null,
            string? postfix = null,
            string? transpiler = null)
        {
            this._targetMethod = AccessTools.Method(targetType, targetMethodName, arguments);
            this._prefixMethod = prefix;
            this._postfixMethod = postfix;
            this._transpilerMethod = transpiler;
        }
    }

    internal abstract class PatchList
    {
        internal List<Patch> Patches = [];
        internal virtual void ApplyAll(Harmony harmony)
        {
            foreach (Patch patch in this.Patches)
            {
                harmony.Patch(
                    original: patch._targetMethod,
                    prefix: patch._prefixMethod == null ? null : new HarmonyMethod(this.GetType(), patch._prefixMethod),
                    postfix: patch._postfixMethod == null ? null : new HarmonyMethod(this.GetType(), patch._postfixMethod),
                    transpiler: patch._transpilerMethod == null ? null : new HarmonyMethod(this.GetType(), patch._transpilerMethod)
                );
            }
        }
    }
}
