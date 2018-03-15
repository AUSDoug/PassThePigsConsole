# Pass the Pigs

Program name: 	Pass the Pigs  
Author: 		Douglas Spangenberg 
Version: 		1.0   
Date: 			1st December March 2016  
Build Time (Version): 	~ 10.0 Hours  
Build Time (Total): 	~ 10.0 Hours  
Licenses:               GNU General Public License v3.  

What is it:
---------------------------
 - A Console Application implementation of Hasbro's Pass the Pigs.
 - A project I started the best part of a year ago, but left idle for the last 9 months or so.

What it does: 
---------------------------
Mode 1: 'AI' vs. 'AI'
 - Essentially pits two sets of decision making rules against each other.
 - The game currently has 3 sets of rules implemented: 'Basic', 'Random' & 'Aggressive'.
	- Basic: After checking for obvious reasons to roll (haven't rolled yet this turn) or to not roll (has won),
     and, after working through various 'how am I going this turn', 'what is opponent on' questions, falls back on
	 the 'stop at 23 Rule' as described in Gorman's 'Analytics, Pedagogy and the Pass the Pigs Game'. 
	 A well rounded and consistent method that won't throw games away with rash decisions.
	- Random: Exactly what it says on the tin. Checks for obvious cases (same as 'Basic') and then, if applicable,
	 picks a random integer. Odd = No Roll, Even = Roll.
	- Aggresive: Can end games very quickly. Will not roll if it has won, and will stop at 50 points per turn unless the
	  opponent is winning by a big margin. Otherwise, will roll.
Mode 2: Human vs. 'AI'
 - Allows a human player to play against their chosen AI ruleset.
 - Simple 'Roll' or 'Pass The Pigs' commands.


Requirements:
-----------------------------
 - .NET Framework 4.5.2

Usage - AI vs AI:
-----------------------------
1)     Modify the Setting.ini, so that:  
1.a)   The number of games is that which you wish the AI to play.  
1.b)   The AI assigned to each 'Player' is that which you wish the AI to play: Basic=0, Random=1, Aggressive=2.  
1.c)   The Logging mode is that which you wish to use. (1 = Very verbose, Logs every action and reason. Good for testing new rule-set. 0 = Only basic info)  

Usage - Human vs AI:
-----------------------------
1)     Modify the Setting.ini, so that:  
1.a)   The number of games is that which you wish to play.  
1.b)   The AI assigned to 'CPU 0' is that which you wish to play against: Basic=0, Random=1, Aggressive=2.  
1.c)   The Logging mode is that which you wish to use. (1 = Very verbose, Logs every action and reason. Good for testing new rule-set. 0 = Only basic info)  
2)     Start the game with '1' as a a command-line argument.  


ChangeLog:
-----------------------------
1st December - 1.0 Release


Notes:
-----------------------------
- If you lose the Settings.ini file, a new one will be generated at next use with default values.
