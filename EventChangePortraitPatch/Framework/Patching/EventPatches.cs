using HarmonyLib;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Microsoft.Xna.Framework;

namespace EventChangePortraitPatch.Framework.Patching
{
    internal class EventPatches : PatchList
    {
        public EventPatches()
        {
            Patches = new List<Patch>
            {
                new (
                    typeof(Event),
                    "command_changePortrait",
                    new[] { typeof(GameLocation), typeof(GameTime), typeof(string[]) },
                    transpiler: nameof(CommandChangePortraitTranspiler)),
            };
        }

        private static IEnumerable<CodeInstruction> CommandChangePortraitTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();
            int point1 = -1;
            int point2 = -1;
            Label? jump1 = null;
            Label? jump2 = null;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Is(OpCodes.Ldstr, "Portraits\\") && codes[i + 3].opcode.Equals(OpCodes.Ldelem_Ref))
                {
                    point1 = i + 4;
                }

                if (point1 != -1 && codes[i].LoadsConstant(2) && codes[i+1].opcode.Equals(OpCodes.Ldelem_Ref) && codes[i+2].opcode == OpCodes.Call)
                {
                    jump2 = generator.DefineLabel();
                    jump1 = generator.DefineLabel();
                    codes[i + 2] = CodeInstruction.Call(typeof(string), "Concat", new [] {typeof(string), typeof(string), typeof(string)});
                    codes[i + 2].labels.Add( (Label) jump1);
                    List<CodeInstruction> a = new List<CodeInstruction>()
                    {
                        CodeInstruction.Call(typeof(string), "Concat", new [] {typeof(string), typeof(string)}),
                        new (OpCodes.Br_S, jump1),
                        new (OpCodes.Ldstr, "")
                    };
                    a[2].labels.Add((Label) jump2);
                    codes.InsertRange(i + 2, a);
                }
            }

            
            
            LambdaExpression countExpression = CreateCountLambda();
            List<CodeInstruction> added = new List<CodeInstruction>()
            {
                new (OpCodes.Ldarg_3),
                new (OpCodes.Ldlen),
                new (OpCodes.Conv_I4),
                new (OpCodes.Ldc_I4_2),
                new (OpCodes.Beq_S, jump2)
            };
            codes.InsertRange(point1, added);
            return codes;
        }
        private static LambdaExpression CreateCountLambda()
        {
            // Parameter representing the string[]
            ParameterExpression arrParameter = Expression.Parameter(typeof(string[]), "source");

            // Call .Count() on the string[]
            MethodCallExpression countCall = Expression.Call(
                typeof(Enumerable),
                "Count",
                new[] { typeof(string) },
                arrParameter
            );

            // Create a LambdaExpression
            LambdaExpression countExpression = Expression.Lambda(countCall, arrParameter);

            return countExpression;
        }
    }
}
