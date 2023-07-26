using FarmCafe.Framework.Customers;
using FarmCafe.Framework.Managers;
using HarmonyLib;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Claims;
using xTile.Dimensions;
using xTile.ObjectModel;
using xTile.Tiles;
using Object = StardewValley.Object;
using static FarmCafe.Framework.Utilities.Utility;

// ReSharper disable UnusedParameter.Local
// ReSharper disable InconsistentNaming

namespace FarmCafe.Framework.Patching
{
	internal enum PatchType
	{
		prefix, postfix, transpiler
	}

	internal class PatchItem
	{
		internal MethodInfo _targetMethod;
		internal string _prefixMethod;
		internal string _postfixMethod;
		internal string _transpilerMethod;

		public PatchItem(Type targetType, 
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

    internal class Patching
	{
		internal List<PatchItem> Patches;

        public Patching()
		{
			Patches = new List<PatchItem>()
			{
				new (
					typeof(PathFindController), 
					"moveCharacter",
					null, 
					transpiler: nameof(MoveCharacterTranspiler)),
                new (
                    typeof(Character), 
					"updateEmote", 
					null,  
					transpiler: nameof(UpdateEmoteTranspiler)),
				new (
                    typeof(Tool), 
					"DoFunction", 
					new[] {typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer) },  
					postfix: nameof(ToolDoFunctionPostfix)),
				new (
                    typeof(Game1),
					"warpCharacter",
					new[] { typeof(NPC), typeof(GameLocation), typeof(Vector2) },
                    postfix: nameof(WarpCharacterPostfix)),
				new (
                    typeof(Furniture),
					"placementAction",
					new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) },
					postfix: nameof(FurniturePlacePostfix)
                    ),
                new (
                    typeof(Furniture),
                    "performRemoveAction",
                    new[] { typeof(Vector2), typeof(GameLocation) },
                    postfix: nameof(FurnitureRemovePostfix)
                    ),
                new (
                    typeof(Furniture),
                    "canBeRemoved",
					new[] { typeof(Farmer) },
                    postfix: nameof(CanBeRemovedPostfix)
                    ),
                new (
                    typeof(Furniture),
                    "HasSittingFarmers",
                    null,
                    prefix: nameof(HasSittingFarmersPrefix)
                    ),
                new (
                    typeof(Furniture),
                    "AddSittingFarmer",
					new[] { typeof(Farmer) },
                    postfix: nameof(AddSittingFarmerPostfix)
                    ),
                new (
                    typeof(PathFindController),
                    "GetFarmTileWeight",
                    null,
                    transpiler: nameof(GetFarmTileWeightTranspiler)
                    ),
            };
        }

        internal void Apply(Harmony harmony)
		{
			foreach (PatchItem patch in Patches)
			{
				harmony.Patch(
					original: patch._targetMethod,
					prefix: patch._prefixMethod == null ? null : new HarmonyMethod(GetType(), patch._prefixMethod),
					postfix: patch._postfixMethod == null ? null : new HarmonyMethod(GetType(), patch._postfixMethod),
					transpiler: patch._transpilerMethod == null ? null : new HarmonyMethod(GetType(), patch._transpilerMethod)
					);
			}
			Debug.Log("Patched methods");
		}

		private static IEnumerable<CodeInstruction> UpdateEmoteTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
			Label fadingJump = new Label();
			List<CodeInstruction> codes = instructions.ToList();
			MethodInfo MContainsKey = AccessTools.Method(AccessTools.Field(typeof(StardewValley.Character), "modData").FieldType, "ContainsKey");
			int pointToInsert = 0;

			for (int i = 0; i < codes.Count(); i++)
			{
				if (codes[i].LoadsConstant(1) && codes[i + 1].StoresField(AccessTools.Field(typeof(StardewValley.Character), "emoteFading")))
				{
					fadingJump = generator.DefineLabel();
					codes[i - 1].labels.Add(fadingJump);
					pointToInsert = i - 1;
				}
            }

			var addcodes = new List<CodeInstruction>()
			{
				new (OpCodes.Ldarg_0),
				new (OpCodes.Isinst, typeof(Customer)),
				new (OpCodes.Brfalse, fadingJump),

                new (OpCodes.Ldarg_0),
				CodeInstruction.LoadField(typeof(Customer), "emoteLoop"),
				new (OpCodes.Brfalse, fadingJump),

                new (OpCodes.Ldarg_0),
				new (OpCodes.Ldarg_0),
				CodeInstruction.LoadField(typeof(StardewValley.Character), "currentEmote"),
				CodeInstruction.StoreField(typeof(StardewValley.Character), "currentEmoteFrame"),
				new (OpCodes.Ret),
			};

			codes.InsertRange(pointToInsert, addcodes);
            return codes.AsEnumerable();

        }

        private static IEnumerable<CodeInstruction> MoveCharacterTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var FPathfindercharacter = AccessTools.Field(typeof(PathFindController), "character");
			var MObjectIsPassableMethod = AccessTools.Method(typeof(Object), "isPassable");

			var codeList = instructions.ToList();
			int start_pos = -1;

			for (int i = 0; i < codeList.Count(); i++)
			{
				if (codeList[i].Calls(MObjectIsPassableMethod))
				{
					start_pos = i + 1;
					break;
				}
			}

			if (codeList[start_pos].Branches(out var jumpLabel) && jumpLabel != null)
			{
				var patchCodes = new List<CodeInstruction>
				{
					new (OpCodes.Ldarg_0), // this
			        new (OpCodes.Ldfld, FPathfindercharacter), // this.character
			        CodeInstruction.Call(typeof(Character), "GetType"), // this.character.GetType()
			        new (OpCodes.Ldstr, "Seat"), // this.character.GetType(), "seat"
			        CodeInstruction.Call("System.Type:GetField", new[] { typeof(string) }), // FieldInfo this.character.GetType().GetField("seat")
			        new (OpCodes.Brtrue, jumpLabel) // branch to the same one found earlier

		        };

				codeList.InsertRange(start_pos + 1, patchCodes);
			}
			else
				Debug.Log("Couldn't find the break after isPassable check", LogLevel.Error);

			return codeList.AsEnumerable();
		}


		private static void WarpCharacterPostfix(NPC character, GameLocation targetLocation, Vector2 position)
		{
			if (character is Customer customer)
			{
				CustomerManager.HandleWarp(customer, targetLocation, position);
			}
		}

		private static void ToolDoFunctionPostfix(Tool __instance, GameLocation location, int x, int y, int power, Farmer who)
		{
			switch (__instance)
			{
				case Axe:
					//RepathCustomer(x, y);
					break;
				case Pickaxe:
					//RepositionCustomer(x, y);
					break;
				case Hoe:
					{
						Debug.Log($"{x}, {y}: {GetTileProperties(location.Map.GetLayer("Back").PickTile(new Location(x, y), Game1.viewport.Size))}");

						//foreach (var tile in location.Map.GetLayer("Buildings").Tiles.Array)
						//{
						//}
						//   string a = location.doesTileHaveProperty(x, y, "Passable", "Buildings");
						//   Debug.Log(a);
						//var tile = location.Map.GetLayer("Back").Tiles[x, y];
						//if (tile == null) break;
						//foreach (var prop in tile.Properties)
						//    Debug.Log($"Tile {x}, {y} has: {prop.Key} = {prop.Value}");
						break;
					}
				case FishingRod:
					break;
				case WateringCan:
					break;
			}
		}

		private static void CanBeRemovedPostfix(Furniture __instance, Farmer who, ref bool __result)
		{
			if (!__result) return;

            string val;
            if ((__instance.modData.TryGetValue("FarmCafeTableIsReserved", out val) && val == "T")
				|| (__instance.modData.TryGetValue("FarmCafeChairIsReserved", out val) && val == "T"))
            {
				Debug.Log("Can't remove furniture. Is reserved!");
                __result = false;
            }
        }

        private static void FurnitureRemovePostfix(Furniture __instance, Vector2 tileLocation, GameLocation environment)
		{
			if (IsChair(__instance))
			{
				__instance.modData.TryGetValue("FarmCafeChairTable", out string pos1);
				int[] pos2 = pos1.Split(' ').Select(x => int.Parse(x)).ToArray();
				Furniture table = environment.GetFurnitureAt(new Vector2(pos2[0], pos2[1]));
				if (table != null)
				{
                    TableManager.TryRemoveChairFromTable(__instance, table);
                }
            }
			else if (IsTable(__instance))
			{
				if (TableManager.TrackedTables.ContainsKey(__instance))
				{
                    TableManager.TryRemoveTable(__instance);
                }
            }
		}

		private static void FurniturePlacePostfix(Furniture __instance, GameLocation location, int x, int y, Farmer who)
		{
			Debug.Log("Furniture type = " + __instance.furniture_type.Value);
			if (IsChair(__instance))
			{
                // Get position of chair
                Vector2 tablePos = __instance.TileLocation;

                // Get position of table in front of the chair
                tablePos += DirectionIntToDirectionVector(__instance.currentRotation.Value) * new Vector2(1, -1);

                // Get table Furniture object
                Furniture table = location.GetFurnitureAt(tablePos);

                // Get Table object
                if (table == null || !IsTable(table))
                {
                    return;
                }

                if (!TableManager.TrackedTables.ContainsKey(table))
                {
                    TableManager.AddTable(table, location);
                }
				else
				{
					TableManager.AddChairToTable(__instance, table) ;
                }
            }
			else if (IsTable(__instance))
			{
				TableManager.AddTable(__instance, location);
			}
		}

		// Drawing a chair's front texture requires that HasSittingFarmers returns true
		private static bool HasSittingFarmersPrefix(Furniture __instance, ref bool __result)
		{
			if (__instance.modData.ContainsKey("FarmCafeChairIsReserved") && __instance.modData["FarmCafeChairIsReserved"] == "T")
			{
                __result = true;
                return false;
            }
			return true;
		}


		private static void AddSittingFarmerPostfix(Furniture __instance, Farmer who, ref Vector2? __result)
		{
			if (__result == null) return;
			foreach (var customer in CustomerManager.CurrentCustomers)
			{
				if (customer.Seat?.TileLocation != __result) continue;

				__result = null;
				return;
			}
		}

		private static IEnumerable<CodeInstruction> GetFarmTileWeightTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			int stage = 0;
			foreach (var code in instructions)
			{
				if (stage == 0 && code.Is(OpCodes.Isinst, typeof(StardewValley.TerrainFeatures.Flooring)))
				{
					stage++;
				}

				if (stage == 1 && code.LoadsConstant())
				{
					stage++;
                    code.operand = 150;
				}

				yield return code;
			}
		}


		internal static string GetTileProperties(Tile tile)
		{
			string s = "";
			if (tile == null) return "None";
			foreach (KeyValuePair<string, PropertyValue> prop in tile.Properties)
			{
				s += $"{prop.Key}: {prop.Value}, ";
			}
			foreach (KeyValuePair<string, PropertyValue> prop in tile.TileIndexProperties)
			{
				s += $"{prop.Key}: {prop.Value}, ";
			}

			return s;
		}


		private static void DebugRepositionCustomer(int x, int y)
		{
			if (CustomerManager.CurrentCustomers.Any())
			{
				Customer c = CustomerManager.CurrentCustomers.First();
				c.Position = new Vector2(x, y);
			}

		}
		private static void DebugRepathCustomer(int x, int y)
		{
			if (!CustomerManager.CurrentCustomers.Any()) return;

			Customer customer = CustomerManager.CurrentCustomers.First();

			Point target = Game1.MasterPlayer.FacingDirection switch
			{
				0 => new Point(x / 64, y / 64 - 1),
				1 => new Point(x / 64 + 1, y / 64),
				2 => new Point(x / 64, y / 64 + 1),
				3 => new Point(x / 64 - 1, y / 64),
				_ => new Point(x, y)
			};

			customer.UpdatePathingTarget(target);
		}

	}
}
