# Midway
Midway Campaign as Blazor Web App

This solution has two projects.  Midway.cs is a rewrite of the original midway campaign keeping some of the same array structures.  Some odds and behaviors are adjusted to more closely match historical reality, and I've added some battle logic to the transport group's escort carrier which can occasionally become a factor in an otherwise close battle or in cases where transport fleet is under air attacks.

Rather than a loop with line input and outputs, the midway.cs has a process command function to take entry from the entry line in the blazor app, and it fires events to show updates.

For those interested in the original app but frustrated by its spaghetti structure (which was likely intentional), you can see a lot of helpful guides at top of midway.cs as to what each array member meant.  Note that I have some new array members for some new logic -- for example, I have CAP that have to spend a brief period recovering to be ready for a subsequent wave.  In general, the original application used a myriad of backwards if/then's with the then being a goto to the next line, and the else condition on following line.  It also has numerous loops not with for/next, but even more with variables in +=1 and while < type loops made even more complex by goto's in the flow and the reuse of the same variables as local values and then the same value as global for use in a gosub.  For things like displaying carrier names, it would say set k to 1 for akagi then gosub ####.  This was likely to obfuscate the code.  On top of that, the arrays are populated from data lists that are intentionally not grouped.

This was just a labor of love because I love the strategy of the game, but I plan to add some more features.  Contributors are welcome.

I plan on adding soon a factor where SBD's on first run or two lose bombs dude to the arming system that bombers quickly learned not to use after the first day.

I am going to replace the letters for ships and groups with graphic icons and courses.

I am going to replace the text entry with a graphical menu.

I am going to going to add last known contact type system (except during times PBY's manage to stay overhead -- which is limited by fuel and range).

I am going to allow launching strikes in a direction of choice... as in the real battle, that is how it went down.  On top of that, scouting strikes will be guided back by returning strikes -- another important factor in the actual battle.

I am going to be adding in the B-17's which generally have trouble hitting carriers (though they came close -- the Japanese are lucky I wasn't designing their bombs as they would have had more randomized fins making them less predictable)... but in any case, they would delay Japanese from launching strikes.

I am planning to modify strike launching so you must turn at enough speed into the wind to launch a strike.

I will add in more real arming and launching times for strikes -- and if you are attacked while launching a strike, you can continue launching the rest of the strike or abandon that for evasive maneuvers.  Bomb / torpedo accuracy will be more affected by speed and evasion.  For example, a carrier at 80% damage is unlikely to be moving fast if at all, and a bomber or torpedo bomber should have a good shot at hitting it.

At some point I might remove the turn based nature and make this a second by second simulator.

At some point I am going to be adding either a mobile friendly format of the web screen, or I might just make another project in Xamarin or now more likely MAUI and run it there so it can be an Android/iOS app.

If you nerd out over this stuff like I do, contributors are welcome, or if you are working on your own projects based on the original, I'm happy to provide more pointers as helpful.
