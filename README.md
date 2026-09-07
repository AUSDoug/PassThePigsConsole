# Pass the Pigs

Program name: 	Pass the Pigs
Author: 		Douglas Spangenberg
Version: 		1.3
Date: 			7th September 2026
Licenses: 		GNU General Public License v3.

What is it:
---------------------------
A C# implementation of Hasbro's Pass the Pigs. It runs as a console application
for logging, with small WPF windows for choosing the game mode and, in Human vs
AI, for actually playing.

Game modes:
---------------------------
On start-up a pop-up asks which mode to play.

Mode 1: AI vs AI
 - Pits two sets of decision-making rules against each other.
 - A second pop-up lets you pick each CPU's ruleset, the number of games, and the
   logging mode. It is pre-filled from Settings.ini; pressing 'Start' overrides
   those values for the session.
 - Plays out to the console/log with no delay between rolls.

Mode 2: Human vs AI
 - A game window shows the turn number, both players' scores (the active player in
   bold, with the in-progress turn score in brackets), the two pigs from the last
   roll, and **Roll** / **Pass** buttons. Pass is disabled until you have rolled at
   least once. In this mode the console is only used for logging.
 - The AI opponent pauses briefly between its rolls so you can watch them land.
 - The opponent's ruleset, the number of games, and the logging mode are read from
   Settings.ini (the opponent uses the **CPU 1 AI** value).

AI rulesets:
---------------------------
| id | name       | summary |
|----|------------|---------|
| 0  | Basic      | General-purpose heuristics, falling back on "stop at 23". |
| 1  | Random     | Coin flip each roll. |
| 2  | Aggressive | Rolls hard; banks at 50 per turn. |
| 3  | Expert     | "Stop at 23" plus endgame play. |
| 4  | EV         | Pure "stop at 23", nothing else. |

- **Basic** — After checking for obvious reasons to roll (haven't rolled yet this
  turn) or not to roll (already won, sitting on a big turn score, protecting a
  near-winning lead), and working through a series of "how is this turn going" and
  "what is my opponent on" questions, it falls back on the "stop at 23" rule from
  Gorman's *Analytics, Pedagogy and the Pass the Pigs Game*. Well rounded and
  consistent; it won't throw a game away on a rash decision.

- **Random** — Exactly what it says on the tin. Checks for the obvious cases (as
  Basic) and otherwise picks a random integer: odd = pass, even = roll.

- **Aggressive** — Can end games very quickly. Won't roll once it has won, and
  stops at 50 points in a turn unless the opponent is winning by a big margin.
  Otherwise it rolls.

- **Expert** — Gorman's marginal analysis: each roll risks a ~21% pig out for a
  constant ~4.7 expected points, so rolling is worth it while the accumulated turn
  score is below ~23. That rule maximises the *expected score* of a turn but not
  the *chance of winning*, so near the end of the game Expert switches to playing
  the probability of winning: it presses on for the win when victory is within a
  turn, and gambles for the win when the opponent is one good turn away from 100.
  (An earlier version also slid its target up/down by relative score; benchmarking
  showed that hurt against the other rulesets, so it was removed.)

- **EV** — The pure textbook heuristic: roll until the turn score reaches 23, then
  stop. It ignores the opponent completely and has no endgame logic. It is the
  clean baseline the Expert tree is built on; it does about as well as Expert
  against the weaker bots, but Expert beats it head-to-head via the endgame play.

Headless benchmarking:
---------------------------
`PassThePigsConsole.exe bench games=<n> seed=<n> p1=<spec> p2=<spec>` runs a match
with no windows and prints a single result line to stdout. `<spec>` is
`basic | random | aggressive | ev | expert`, or `expert:<n>` to set Expert's stop
threshold (e.g. `expert:26`). The first turn alternates between the two sides so
first-mover advantage is split evenly.

`tools/hillclimb.py` uses this to sweep Expert's stop threshold head-to-head
against a champion and report the win rate at each value.

Logging:
---------------------------
Logging uses Serilog. Two rolling files are written to a `logs/` folder next to
the executable, plus the console:

| output | contents |
|--------|----------|
| console + `logs/passthepigs-*.log` | game flow (turns, scores, rolls, results) |
| `logs/ai-commentary-*.log`         | every AI decision and the reason for it, always captured |

`Log Mode` / the verbose checkbox raises the console + main log to include the
roll-by-roll detail and echo the AI commentary; the AI-commentary file is written
regardless.

Requirements:
---------------------------
 - .NET Framework 4.8
 - NuGet packages (restored automatically on build): WeightedRandomizer, Serilog,
   Serilog.Sinks.Console, Serilog.Sinks.File

Settings.ini:
---------------------------
| key        | meaning |
|------------|---------|
| Games      | Number of games to play. |
| CPU 0 AI   | Ruleset id for player 1 (AI vs AI only). |
| CPU 1 AI   | Ruleset id for player 2 / the Human's opponent. |
| Log Mode   | 1 = verbose (logs every action and the reason for it), 0 = basic info. |

In AI vs AI these are just the defaults for the setup pop-up. If the file is
missing it is regenerated with default values on the next run.

ChangeLog:
---------------------------
7th September 2026 - 1.3 Update
	- Logging moved from System.Diagnostics.Trace to Serilog: levels instead of the
	  hand-rolled verbose flag, a separate "AI commentary" log, rolling files.
	- Project converted to the SDK-style csproj / PackageReference (builds with
	  `dotnet build`); fixed a stray "\n" that stopped Settings.ini being read.
6th September 2026 - 1.2 Update
	- Retargeted to .NET Framework 4.8; project "dev restart".
	- New AI rulesets: 'Expert' (3) and 'EV' (4), derived from Gorman's paper.
	- AI vs AI: setup pop-up for rulesets / number of games / logging.
	- Human vs AI: full game window - turn counter, live scores, pig sprites, and
	  Roll / Pass buttons; the AI is paced so its rolls are visible; the console is
	  demoted to logging only.
	- Pig sprites added under PassThePigsConsole/Resources/.
	- Headless 'bench' mode and tools/hillclimb.py for tuning the AI.
26th September 2019 - 1.1 Update
	- Documentation changes. Start of framework to support Custom AI Rules.
1st December - 1.0 Release

Notes:
-----------------------------
- If you lose the Settings.ini file, a new one will be generated at next use with
  default values.

Resources:
-----------------------------
Gorman, Michael F. 'Analytics, Pedagogy and the Pass the Pigs Game', INFORMS Transactions on Education, Vol. 13-1 September 2012
BlueRaja - Weighted Randomizer for C Sharp - https://github.com/BlueRaja/Weighted-Item-Randomizer-for-C-Sharp
