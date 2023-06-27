﻿using FarmCafe.Framework.Customers;
using FarmCafe.Framework.Managers;
using FarmCafe.Framework.Objects;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using xTile.Dimensions;
using xTile.ObjectModel;
using xTile.Tiles;
using Object = StardewValley.Object;
using Utility = FarmCafe.Framework.Utilities.Utility;

// ReSharper disable UnusedParameter.Local
// ReSharper disable InconsistentNaming

namespace FarmCafe.Framework.Patching
{
	internal class GamePatch
	{
		internal void Apply(Harmony harmony)
		{

			harmony.Patch(
				original: AccessTools.Method(typeof(PathFindController), "moveCharacter"),
				transpiler: new HarmonyMethod(GetType(), nameof(MoveCharacterTranspiler)));

			harmony.Patch(
				original: AccessTools.Method(typeof(Tool), "DoFunction",
					new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer) }),
				postfix: new HarmonyMethod(GetType(), nameof(ToolDoFunctionPostfix)));

			harmony.Patch(
				original: AccessTools.Method(typeof(Game1), "warpCharacter", new[] { typeof(NPC), typeof(GameLocation), typeof(Vector2) }),
				postfix: new HarmonyMethod(GetType(), nameof(WarpCharacterPostfix)));

			harmony.Patch(
				original: AccessTools.Method(typeof(Furniture), "placementAction",
					new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) }),
				postfix: new HarmonyMethod(GetType(), nameof(FurniturePlacePostfix)));

			harmony.Patch(
				original: AccessTools.Method(typeof(Furniture), "performRemoveAction",
					new[] { typeof(Vector2), typeof(GameLocation) }),
				postfix: new HarmonyMethod(GetType(), nameof(FurnitureRemovePostfix)));

			harmony.Patch(
				original: AccessTools.Method(typeof(Furniture), "HasSittingFarmers"),
				prefix: new HarmonyMethod(GetType(), nameof(HasSittingFarmersPrefix)));

			harmony.Patch(
				original: AccessTools.Method(typeof(Furniture), "AddSittingFarmer", new[] { typeof(Farmer) }),
				postfix: new HarmonyMethod(GetType(), nameof(AddSittingFarmerPostfix)));

			harmony.Patch(
				original: AccessTools.Method(typeof(PathFindController), "GetFarmTileWeight"),
				transpiler: new HarmonyMethod(GetType(), nameof(GetFarmTileWeightTranspiler)));

			Debug.Log("Patched methods");
		}

		private static IEnumerable<CodeInstruction> MoveCharacterTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var FPathfindercharacter = AccessTools.Field(typeof(PathFindController), "character");
			var MObjectIsPassableMethod = AccessTools.Method(typeof(Object), "isPassable");

			if (FPathfindercharacter == null || MObjectIsPassableMethod == null)
			{
				Debug.Log("Field or Method not found!", LogLevel.Error);
				throw new Exception("Field or Method not found!");
			}

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
			        new (OpCodes.Ldstr, "seat"), // this.character.GetType(), "seat"
			        CodeInstruction.Call("System.Type:GetField", new[] { typeof(string) }), // FieldInfo this.character.GetType().GetField("seat")
			        new (OpCodes.Brtrue, jumpLabel) // branch to the same one found earlier

		        };

				codeList.InsertRange(start_pos + 1, patchCodes);
				Debug.Log("Transpiler patch done");
				//foreach (var item in codeList)
				//{
				// Debug.Log(item.ToString());
				//}
			}
			else
			{
				Debug.Log("Couldn't find the break after isPassable check");
			}

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

		private static void FurnitureRemovePostfix(Furniture __instance, Vector2 tileLocation, GameLocation environment)
		{
			if (Utility.IsChair(__instance))
			{
				Table table = TableManager.GetTableFromChair(__instance);

				if (table == null)
				{
					Debug.Log("A chair was removed but there was no table assigned to it.", LogLevel.Debug);
					return;
				}

				if (!table.Chairs.Contains(__instance))
				{
					Debug.Log("Chair was removed but the table in front of it didn't recognize it before. Bug alert!", LogLevel.Warn);
					return;
				}

				if (table.isReserved)
				{
					Debug.Log("You shouldn't remove that chair. It's table is reserved!", LogLevel.Warn);
				}

				table.RemoveChair(__instance);
			}
			else if (Utility.IsTable(__instance))
			{
				Table table = TableManager.GetTableFromTableObject(__instance);

				if (table == null)
				{
					Debug.Log("Table removed, but it wasn't a valid table counted in the manager. Bug?", LogLevel.Warn);
					return;
				}

				if (table.isReserved)
				{
					Debug.Log("You shouldn't remove that table. It's reserved!", LogLevel.Warn);
				}


				TableManager.TablesOnFarm.Remove(table);
			}
		}

		private static void FurniturePlacePostfix(Furniture __instance, GameLocation location, int x, int y, Farmer who, ref bool __result)
		{
			if (Utility.IsChair(__instance))
			{
				Table table = TableManager.GetTableFromChair(__instance);
				if (table == null) return;

				if (table.Chairs.Contains(__instance))
				{
					Debug.Log("Table already knows the chair that was placed?", LogLevel.Warn);
					return;
				}

				table.AddChair(__instance);
			}
			else if (Utility.IsTable(__instance))
			{
				Debug.Log("Adding table", LogLevel.Debug);
				TableManager.AddTable(__instance);
			}
		}

		// Drawing a chair's front texture requires that HasSittingFarmers returns true
		private static bool HasSittingFarmersPrefix(Furniture __instance, ref bool __result)
		{
			if (__instance.modData.ContainsKey("FarmCafeSeat") && __instance.modData["FarmCafeSeat"] == "1")
			{
                __result = true;
                return false;
            }
			return false;
			//foreach (var character in Game1.getFarm().characters)
			//{
			//	if (character is not Customer customer || customer.Seat != __instance) continue;

			//	__result = true;
			//	return false;
			//}

			//return true;
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
				if (!code.Is(OpCodes.Isinst, typeof(StardewValley.TerrainFeatures.Flooring)) && stage == 0)
				{
					stage++;
					yield return code;
					continue;
				}

				if (!code.LoadsConstant() && stage == 1)
				{
					stage++;
					yield return code;
					continue;
				}

				if (stage == 2)
				{
					code.operand = 150;
					stage++;
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
