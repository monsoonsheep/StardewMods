<Query Kind="Statements">
  <RuntimeVersion>6.0</RuntimeVersion>
</Query>

int x = 650, y = 650;

string[] commands =
[
    $"CloudCountry",
    $"13 18",
    $"farmer 13 20 0 Gus 13 18 2",
	$"skippable",
    $"showFrame farmer 113",
	
    $"pause 100",
	
	$"move Gus -1 0 2",
	$"animate Gus false false 500 18 19 20",
	$"pause 1500",
	$"move Gus 1 0 2",
	$"pause 250",
	
    $"speak Gus \"It's nice to see you take some time off and have a drink or two.\"",
	$"emote farmer 32",
    $"speak Gus \"Tending to the farm must be challenging.\"",
	$"pause 1",
	$"quickQuestion #It has its perks.#It's a lot of hard work. (break)" + 
		$"speak Gus \"I don't think I could handle the physical toll alone.$h\" (break)" +
		$"speak Gus \"You could really use a change of pace, then.\" (break)",
	
	$"pause 500",
	
    $"speak Gus \"You know, running this saloon has been gratifying work.\"",
    $"speak Gus \"Lately, there's been more visitors from Zuzu and other towns.#$b#I'm sorta stretched thin in here. Sometimes I wish there were more places for visitors and tourists.\"",
	$"pause 1",
	$"quickQuestion #How about another restaurant?#Food work seems too difficult. (break)" +
		$"speak Gus \"Are you saying you want to start one?\" (break)" +
		$"speak Gus \"Oh it's not too bad.#$b#You could open your own saloon or restaurant if you wanted.\"",
	
	$"pause 100",
	$"emote farmer 28",
	$"pause 1000",
	
	$"speak Gus \"You know, you've become an essential part of this community.#$b#I wouldn't be surprised if you were to expand into other work.\"",
	$"speak Gus \"With the increased traffic into the valley, it could be good for both of us!\"",
    
	$"pause 400",
    $"move Gus 1 0 2",
	$"animate Gus false false 500 18 19 20",
	$"emote farmer 40",
	$"pause 1000",
	$"move Gus -1 0 2",
	
	$"speak Gus \"Besides, a little healthy competition never hurt anyone.$h\"",
	$"speak Gus \"You probably can't beat my cooking, but you're welcome to try.\"",
	$"emote farmer 60",
	$"pause 500",
	
	$"move Gus 1 0 0",
	$"move Gus 0 -1 0",
	$"pause 500",
	
	$"fade",
	$"pause 10",
	$"viewport -100 -100",
	$"message \"You can open your own restaurant.\"",
	
	$"end"
];
 
System.Console.WriteLine(string.Join('/', commands).Replace("\"", "\\\"").Replace("{", @"{{").Replace("}", @"}}"));
