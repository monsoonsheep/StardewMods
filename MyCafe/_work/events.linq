<Query Kind="Statements">
  <Reference>&lt;ProgramFilesX86&gt;\Steam\steamapps\common\Stardew Valley\MonoGame.Framework.dll</Reference>
  <Namespace>Microsoft.Xna.Framework</Namespace>
  <RuntimeVersion>6.0</RuntimeVersion>
</Query>

Point signboardTile = new Point(650, 650);
string[] commands =
[
    $"continue",
    $"{signboardTile.X} {signboardTile.Y + 2}",
    $"farmer {signboardTile.X} {signboardTile.Y + 1} 0 Gus {signboardTile.X + 1} {signboardTile.Y} 3 Robin {signboardTile.X - 1} {signboardTile.Y} 1",
    $"ignoreEventTileOffset",
	$"skippable",
    $"ignoreCollisions Gus",
    $"ignoreCollisions farmer",
    $"ignoreCollisions Robin",
	$"makeInvisible {signboardTile.X - 1} {signboardTile.Y - 1} 3 3",

    $"pause 1000",
    $"emote farmer 40",

    $"animate Robin true false 200 24 25 26",
    $"pause 400",
    $"playSound axe",

    $"faceDirection Gus 2",
    $"move Gus 0 1 3",
    $"faceDirection farmer 1",

    $"pause 200",
    $"speak Gus \"{{i18n:event-CafeIntroduction.01}}\"",
    $"faceDirection Robin 2",
    $"speak Gus \"{{i18n:event-CafeIntroduction.02}}\"",
	$"pause 200",
    $"speak Gus \"{{i18n:event-CafeIntroduction.03}}\"",
    $"pause 100",

    $"emote farmer 32",

    $"move Robin 0 1 1",
    $"faceDirection farmer 3",
    $"speak Robin \"{{i18n:event-CafeIntroduction.04}}\"",
    $"pause 1",
    $"quickQuestion #{{i18n:event-CafeIntroduction.04.answer1}}#{{i18n:event-CafeIntroduction.04.answer2}}#{{i18n:event-CafeIntroduction.04.answer3}}#{{i18n:event-CafeIntroduction.04.answer4}} (break)" +
        $"speak Robin \"{{i18n:event-CafeIntroduction.04.fork1.01}}\" (break)" +
        $"speak Robin \"{{i18n:event-CafeIntroduction.04.fork2.01}}\" (break)" +
        $"speak Robin \"{{i18n:event-CafeIntroduction.04.fork3.01}}\" (break)" +
        $"speak Robin \"{{i18n:event-CafeIntroduction.04.fork4.01}}\"",

    $"faceDirection farmer 1",
    $"pause 300",
    $"faceDirection farmer 3",
    $"pause 300",
    $"faceDirection farmer 0",
    $"pause 300",
	$"addMailReceived MonsoonSheep.MyCafe_HasSeenCafeIntroductionEvent",
    $"end"
];
 
System.Console.WriteLine(string.Join('/', commands).Replace("\"", "\\\"").Replace("{", @"{{").Replace("}", @"}}"));