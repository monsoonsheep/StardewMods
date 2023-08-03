using FarmCafe.Framework.Managers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FarmCafe.Framework.Characters;
using Netcode;
using Object = StardewValley.Object;
using static FarmCafe.Framework.Utilities.Utility;

// ReSharper disable UnusedParameter.Local
// ReSharper disable InconsistentNaming

namespace FarmCafe.Framework.Patching
{
    internal class Patch
	{
		internal MethodInfo _targetMethod;
		internal string _prefixMethod;
		internal string _postfixMethod;
		internal string _transpilerMethod;
        internal Type patchType;

		public Patch(Type targetType, 
			string targetMethodName, 
			Type[] arguments,
			Type patchType,
			string prefix = null, 
			string postfix = null, 
			string transpiler = null)	
        {
            this.patchType = patchType;
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
                    prefix: patch._prefixMethod == null ? null : new HarmonyMethod(patch.patchType, patch._prefixMethod),
                    postfix: patch._postfixMethod == null ? null : new HarmonyMethod(patch.patchType, patch._postfixMethod),
                    transpiler: patch._transpilerMethod == null ? null : new HarmonyMethod(patch.patchType, patch._transpilerMethod)
                );
            }
        }
    }
}
