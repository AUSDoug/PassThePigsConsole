using System;
//To more easily perform some file operations.
using IO = System.IO;
//BlueRaja's Weighted Randomizer package: https://github.com/BlueRaja/Weighted-Item-Randomizer-for-C-Sharp
using Weighted_Randomizer;
//Serilog: see GameLog.cs for the setup.
using Serilog;


#region Info

//Title:  'Pass the Pigs'
//Author: Douglas John Spangenberg
//Date:   26th September 2019

//Description: A C# console-based implementation of Hasbro's 'Pass The Pigs'
//Weighted odds for Pig combinations, three AI rule-sets, minimal or verbose logging
//Easy implementation of custom AI rule-sets to come in the future.
#endregion

namespace PassThePigsConsole
{
    class Program
    {

        //Current location of the Program; Used for the settings file
        static string appPath = IO.Directory.GetCurrentDirectory();

        //Create some Player objects
        internal static Player player1, player2, current;

        //Number of games to be played is controlled by the .ini file.
        //(Defaults to 1 if no .ini found or 0 if invalid value specified)
        static string games;
        static int gamesInt;

        //True if human is playing.
        static Boolean human;

        //Governs which decision making process is used by the AI (ruleset id per CPU).
        static int p1AI, p2AI;

        //Log mode - 0 for basic, 1 for full.
        //Basic keeps track of scores and turns. Full does this, and also logs the rule by which the CPU decided to roll or not.
        internal static Boolean logMode;

        //Signal Game Over
        static Boolean gameOver = false;

        //Sprite window shown during Human vs AI (null in AI vs AI).
        static PigRollWindow pigWindow;

        //Pause between an AI's rolls when a human is watching, so they are visible.
        const int aiRollDelayMs = 750;

        //Various counters; Number of rolls, turns and wins.
        static int p1RollCount, p2RollCount, turnCount, p1Wins = 0, p2Wins = 0;

        //Weighted Randomizer courtesy of BlueRaja
        //https://github.com/BlueRaja/Weighted-Item-Randomizer-for-C-Sharp
        //The reason why there are two lists instead of one, is that this allows for
        //modification of each Pig; One might have a predisposition for Razorback for example.
        static IWeightedRandomizer<string> pigOne = new DynamicWeightedRandomizer<string>();
        static IWeightedRandomizer<string> pigTwo = new DynamicWeightedRandomizer<string>();

        //Store the Pig positions as strings on rolling.
        static String oneString, twoString;

        //Entry method for app. Displays a pop-up at start to choose Human vs AI, or AI vs AI.
        [STAThread]
        static void Main(string[] args)
        {
            //Headless benchmark mode for tuning the AI, e.g.:
            //  PassThePigsConsole.exe bench games=20000 seed=1 p1=expert:23,20,20,35,16 p2=expert:24,20,20,35,16
            //Bypasses the WPF pop-ups and prints a single machine-readable result line.
            if (args.Length > 0 && args[0] == "bench")
            {
                Benchmark.Run(args);
                return;
            }

            //Prompt the user to select the game mode via pop-up window.
            human = GameModeSelector.PromptForHumanMode();

            //Set up logging (console + rolling files under <exe>/logs).
            GameLog.Configure(IO.Path.Combine(AppContext.BaseDirectory, "logs"));
            Log.Information("Session started {Time:u}", DateTime.Now);
            Log.Information(human ? "Mode: Human vs AI" : "Mode: AI vs AI");

            //Run the .ini loader
            iniLoader();
            //If AI vs AI, let the user configure the match via a second pop-up.
            //The .ini values are used as the defaults; pressing 'Start' overrides them.
            if (!human)
            {
                AiGameConfig aiConfig = GameModeSelector.PromptForAiConfig(p1AI, p2AI, gamesInt, logMode);
                if (aiConfig != null)
                {
                    p1AI = aiConfig.Player1AI;
                    p2AI = aiConfig.Player2AI;
                    gamesInt = aiConfig.Games;
                    logMode = aiConfig.LogMode;
                    Log.Information("AI vs AI setup — CPU0 AI={Cpu0}, CPU1 AI={Cpu1}, games={Games}, verbose={Verbose}",
                        p1AI, p2AI, gamesInt, logMode);
                }
            }
            //Apply the chosen verbosity now that Log Mode is known.
            GameLog.SetVerbose(logMode);

            //Initialise pigs, player names.
            pigInitialiser();

            //Open the game window for Human vs AI. It becomes the primary interface:
            //the human rolls/passes with its buttons and the console just logs.
            if (human)
            {
                pigWindow = new PigRollWindow();
                pigWindow.Open(() => turnCount, player1, player2, () => current == player1);
            }

            #region gameLogic
            //Game logic begin.
            //While there are games to be played
            while (gamesInt > 0)
            {
                //Initialise count to 0
                turnCount = 0;
                Log.Information("=== Game start ===");
                //While the game is running.
                while (!gameOver)
                {
                    //Increment turn count
                    turnCount++;
                    //Display game info
                    Log.Information("--- Turn {Turn} --- {P1} {P1Score} ({P1Rolls} rolls), {P2} {P2Score} ({P2Rolls} rolls)",
                        turnCount, player1.name, player1.totalScore, p1RollCount,
                        player2.name, player2.totalScore, p2RollCount);

                    //While CPU0 is in control
                    #region CPU0
                    if (!human)
                    {
                        while (current == player1)
                        {
                            Boolean roll;

                            //Decide to roll or not.
                            if (p1AI == 0)
                            {
                                //roll = AIRules.basic(logMode, player1.totalScore, player1.turnScore, player2.totalScore);
                                roll = AIDecisionTree.rollDecisionBasic();
                            }
                            else if (p1AI == 1)
                            {
                                roll = AIDecisionTree.rollDecisionRandom();
                            }
                            else if (p1AI == 2)
                            {
                                roll = AIDecisionTree.rollAggressive();
                            }
                            else if (p1AI == 3)
                            {
                                roll = AIDecisionTree.rollDecisionExpert();
                            }
                            else if (p1AI == 4)
                            {
                                roll = AIDecisionTree.rollDecisionEV();
                            }
                            else
                            {
                                roll = AIDecisionTree.rollDecisionBasic();
                            }
                            //If we roll
                            if (roll)
                            {
                                //Increment roll count
                                p1RollCount++;
                                rollControl();
                            }
                            //If they're not rolling
                            else
                            {
                                Log.Information("{Player} holds on {TurnScore}", current.name, current.turnScore);
                                //Pass The Pigs
                                turnEnd();

                                //Check for endgame
                                if (!endChecker())
                                {
                                    current = player2;
                                }
                                else
                                {
                                    gameOver = true;
                                    Log.Information("Game over — {Winner} wins", current.name);
                                    p1Wins++;
                                    break;
                                }
                            }

                        }
                        #endregion
                    }
                    #region human
                    else
                    {
                        while (current == player1) {
                            //Roll / Pass comes from the game window's buttons.
                            Boolean roll = pigWindow.GetHumanDecision();
                            //If we roll
                            if (roll)
                            {
                                //Increment roll count
                                p1RollCount++;
                                rollControl();
                            }
                            //If they're not rolling
                            else
                            {
                                Log.Information("{Player} holds on {TurnScore}", current.name, current.turnScore);
                                //Pass The Pigs
                                turnEnd();

                                //Check for endgame
                                if (!endChecker())
                                {
                                    current = player2;
                                }
                                else
                                {
                                    gameOver = true;
                                    Log.Information("Game over — {Winner} wins", current.name);
                                    p1Wins++;
                                    break;
                                }
                            }
                        }
                    }
                    #endregion

                    #region CPU1
                    //While player is in control
                    while (current == player2)
                    {
                        //Slow the AI down so a watching human can see each roll.
                        if (human)
                            System.Threading.Thread.Sleep(aiRollDelayMs);
                        Boolean roll;

                        //Decide to roll or not.
                        if (p2AI == 0)
                        {
                            roll = AIDecisionTree.rollDecisionBasic();
                        }
                        else if (p2AI == 1)
                        {
                            roll = AIDecisionTree.rollDecisionRandom();
                        }
                        else if (p2AI == 2)
                        {
                            roll = AIDecisionTree.rollAggressive();
                        }
                        else if (p2AI == 3)
                        {
                            roll = AIDecisionTree.rollDecisionExpert();
                        }
                        else if (p2AI == 4)
                        {
                            roll = AIDecisionTree.rollDecisionEV();
                        }
                        else
                        {
                            roll = AIDecisionTree.rollDecisionBasic();
                        }
                        //If we roll
                        if (roll)
                        {
                            //Increment roll count
                            p2RollCount++;
                            rollControl();
                        }
                        //If they're not rolling
                        else
                        {
                            Log.Information("{Player} holds on {TurnScore}", current.name, current.turnScore);
                            //Pass The Pigs
                            turnEnd();

                            //Check for endgame
                            if (!endChecker())
                            {
                                current = player1;
                            }
                            else
                            {
                                gameOver = true;
                                Log.Information("Game over — {Winner} wins", current.name);
                                p2Wins++;
                                break;
                            }
                        }

                    }
                    #endregion
                }
                //Summary of the game once it is over
                Log.Information("Game summary — {Turns} turns; {P1} {P1Score} ({P1Rolls} rolls), {P2} {P2Score} ({P2Rolls} rolls)",
                    turnCount, player1.name, player1.totalScore, p1RollCount,
                    player2.name, player2.totalScore, p2RollCount);
                //Number of games remaining
                gamesInt = gamesInt - 1;
                //Cleanup
                cleaner();
            }
            //Once all games have been played
            Log.Information("Session complete — {P1} {P1Wins}, {P2} {P2Wins}",
                player1.name, p1Wins, player2.name, p2Wins);

            //Show the result in the game window and wait for the human to close it.
            if (pigWindow != null)
            {
                string winner = p1Wins > p2Wins ? player1.name
                              : p2Wins > p1Wins ? player2.name
                              : "Nobody";
                pigWindow.ShowGameOver(winner + " wins!   ("
                    + player1.name + " " + p1Wins + " - " + p2Wins + " " + player2.name + ")");
                pigWindow.WaitForClose();
            }

            Log.Information("Session ended {Time:u}", DateTime.Now);
            GameLog.Shutdown();
            #endregion
        }

        public static void pigInitialiser()
        {
            //Create both players
            if (human)
            {
                player1 = new Player("Human");
            }
            else
                player1 = new Player("CPU 0");
            player2 = new Player("CPU 1");

            //cpu0 can go first for this game; Winner should go first for all subsequent games
            current = player1;

            //Create the pig positions
            pigOne.Add("Side (No Dot)", 3490);
            pigOne.Add("Side (Dot)", 3020);
            pigOne.Add("Razorback", 2240);
            pigOne.Add("Trotter", 880);
            pigOne.Add("Snouter", 300);
            pigOne.Add("Leaning Jowler", 70);

            pigTwo.Add("Side (No Dot)", 3490);
            pigTwo.Add("Side (Dot)", 3020);
            pigTwo.Add("Razorback", 2240);
            pigTwo.Add("Trotter", 880);
            pigTwo.Add("Snouter", 300);
            pigTwo.Add("Leaning Jowler", 70);
        }

        //Method to check for - and read from - the settings.ini file
        private static void iniLoader()
        {
            string iniPath = appPath + "/Settings.ini";
            if (IO.File.Exists(iniPath))
            {
                Log.Debug("Reading {IniPath}", iniPath);
                games = INIFile.ReadValue("Settings", "Games", iniPath);
                int.TryParse(games, out gamesInt);
                int.TryParse(INIFile.ReadValue("Settings", "CPU 0 AI", iniPath), out p1AI);
                int.TryParse(INIFile.ReadValue("Settings", "CPU 1 AI", iniPath), out p2AI);
                bool.TryParse(INIFile.ReadValue("Settings", "Log Mode", iniPath), out logMode);
            }
            else
            {
                Log.Debug("No Settings.ini found; writing defaults to {IniPath}", iniPath);
                INIFile.WriteValue("Settings", "Games", "1", iniPath);
                INIFile.WriteValue("Settings", "CPU 0 AI", "0", iniPath);
                INIFile.WriteValue("Settings", "CPU 1 AI", "0", iniPath);
                INIFile.WriteValue("Settings", "Log Mode", "0", iniPath);
                gamesInt = 1;
                p1AI = 0;
                p2AI = 0;
                logMode = false;
            }
            Log.Information("Settings — games={Games}, CPU0 AI={Cpu0}, CPU1 AI={Cpu1}, verbose={Verbose}",
                gamesInt, p1AI, p2AI, logMode);
        }


        //Rolls pigs, keeps track of turn score
        private static void rollControl()
        {
            oneString = pigOne.NextWithReplacement();
            twoString = pigTwo.NextWithReplacement();
            int x = Score(oneString, twoString);
            Log.Debug("{Player} rolls {Pig1} + {Pig2} = {Score}", current.name, oneString, twoString, x);

            //Show the roll in the sprite window (Human vs AI only).
            if (pigWindow != null)
                pigWindow.ShowRoll(current.name, oneString, twoString, x);

            //Pig Out, turn score to 0
            if (x == 0)
            {
                Log.Information("{Player} pigs out", current.name);
                current.turnScore = 0;
                turnEnd();
            }

            //Increment turn score by x
            else
            {
                current.turnScore += x;
            }
        }

        //Series of boolean methods that return true if the CPU should roll, false if they shouldn't.
        //Each is a series of If checks, and will return if, at any stage, the condition in question is satisfied.

        //'New' methods; "What are the odds on my roll improving my position; how much; is it worth it?"


        //Given two pig positions, returns the score for that roll (0 = pig out).
        //Order-sensitive, matching the physical game's scoring table.
        internal static int Score(string oneString, string twoString)
        {
            //Here follow the various combinations of pig, returning the appropriate score as an Int.
            if (((oneString.Equals("Side (No Dot)"))
                && (twoString.Equals("Side (Dot)")))
                || ((oneString.Equals("Side (Dot)"))
                && (twoString.Equals("Side (No Dot)"))))
            {
                return 0;
            }
            else if (oneString.Equals("Side (No Dot)")
            && twoString.Equals("Side (No Dot)"))
            {
                return 1;
            }
            else if (oneString.Equals("Side (No Dot)")
                    && twoString.Equals("Razorback"))
            {
                return 5;
            }
            else if (oneString.Equals("Side (No Dot)")
                    && twoString.Equals("Trotter"))
            {
                return 5;
            }
            else if (oneString.Equals("Side (No Dot)")
                    && twoString.Equals("Snouter"))
            {
                return 10;
            }
            else if (oneString.Equals("Side (No Dot)")
                    && twoString.Equals("Leaning Jowler"))
            {
                return 15;
            }
            else if (oneString.Equals("Side (Dot)")
                    && twoString.Equals("Side (Dot)"))
            {
                return 1;
            }
            else if (oneString.Equals("Side (Dot)")
                    && twoString.Equals("Razorback"))
            {
                return 5;
            }
            else if (oneString.Equals("Side (Dot)") && twoString.Equals("Trotter"))
            {
                return 5;
            }
            else if (oneString.Equals("Side (Dot)") && twoString.Equals("Snouter"))
            {
                return 10;
            }
            else if (oneString.Equals("Side (Dot)")
                    && twoString.Equals("Leaning Jowler"))
            {

                return 15;
            }
            else if (oneString.Equals("Razorback")
                    && twoString.Equals("Side (No Dot)"))
            {
                return 5;
            }
            else if (oneString.Equals("Razorback")
                    && twoString.Equals("Side (Dot)"))
            {
                return 5;
            }
            else if (oneString.Equals("Razorback") && twoString.Equals("Razorback"))
            {
                return 20;
            }
            else if (oneString.Equals("Razorback") && twoString.Equals("Trotter"))
            {
                return 10;
            }
            else if (oneString.Equals("Razorback") && twoString.Equals("Snouter"))
            {
                return 10;
            }
            else if (oneString.Equals("Razorback")
                    && twoString.Equals("Leaning Jowler"))
            {
                return 20;
            }
            else if (oneString.Equals("Trotter")
                    && twoString.Equals("Side (No Dot)"))
            {
                return 5;
            }
            else if (oneString.Equals("Trotter") && twoString.Equals("Side (Dot)"))
            {
                return 5;
            }
            else if (oneString.Equals("Trotter") && twoString.Equals("Razorback"))
            {
                return 10;
            }
            else if (oneString.Equals("Trotter") && twoString.Equals("Trotter"))
            {
                return 20;
            }
            else if (oneString.Equals("Trotter") && twoString.Equals("Snouter"))
            {
                return 15;
            }
            else if (oneString.Equals("Trotter")
                    && twoString.Equals("Leaning Jowler"))
            {
                return 20;
            }
            else if (oneString.Equals("Snouter")
                    && twoString.Equals("Side (No Dot)"))
            {
                return 10;
            }
            else if (oneString.Equals("Snouter") && twoString.Equals("Side (Dot)"))
            {
                return 10;
            }
            else if (oneString.Equals("Snouter") && twoString.Equals("Razorback"))
            {
                return 15;
            }
            else if (oneString.Equals("Snouter") && twoString.Equals("Trotter"))
            {
                return 15;
            }
            else if (oneString.Equals("Snouter") && twoString.Equals("Snouter"))
            {
                return 40;
            }
            else if (oneString.Equals("Snouter")
                    && twoString.Equals("Leaning Jowler"))
            {
                return 25;
            }
            else if (oneString.Equals("Leaning Jowler")
                    && twoString.Equals("Side (No Dot)"))
            {
                return 15;
            }
            else if (oneString.Equals("Leaning Jowler")
                    && twoString.Equals("Side (Dot)"))
            {
                return 15;
            }
            else if (oneString.Equals("Leaning Jowler")
                    && twoString.Equals("Razorback"))
            {
                return 20;
            }
            else if (oneString.Equals("Leaning Jowler")
                    && twoString.Equals("Trotter"))
            {
                return 20;
            }
            else if (oneString.Equals("Leaning Jowler")
                    && twoString.Equals("Snouter"))
            {
                return 25;
            }
            else if (oneString.Equals("Leaning Jowler")
                    && twoString.Equals("Leaning Jowler"))
            {
                return 60;
            }
            else if (twoString.Equals("Side (No Dot)")
                    && oneString.Equals("Side (No Dot)"))
            {
                return 1;
            }
            else if (twoString.Equals("Side (No Dot)")
                    && oneString.Equals("Razorback"))
            {
                return 5;
            }
            else if (twoString.Equals("Side (No Dot)")
                    && oneString.Equals("Trotter"))
            {
                return 5;
            }
            else if (twoString.Equals("Side (No Dot)")
                    && oneString.Equals("Snouter"))
            {
                return 10;
            }
            else if (twoString.Equals("Side (No Dot)")
                    && oneString.Equals("Leaning Jowler"))
            {
                return 15;
            }
            else if (twoString.Equals("Side (Dot)")
                    && oneString.Equals("Side (No Dot)"))
            {
                return 1;
            }
            else if (twoString.Equals("Side (Dot)")
                    && oneString.Equals("Razorback"))
            {
                return 5;
            }
            else if (twoString.Equals("Side (Dot)") && oneString.Equals("Trotter"))
            {
                return 5;
            }
            else if (twoString.Equals("Side (Dot)") && oneString.Equals("Snouter"))
            {
                return 10;
            }
            else if (twoString.Equals("Side (Dot)")
                    && oneString.Equals("Leaning Jowler"))
            {
                return 15;
            }
            else if (twoString.Equals("Razorback")
                    && oneString.Equals("Side (No Dot)"))
            {
                return 5;
            }
            else if (twoString.Equals("Razorback")
                    && oneString.Equals("Side (Dot)"))
            {
                return 5;
            }
            else if (twoString.Equals("Razorback") && oneString.Equals("Razorback"))
            {
                return 20;
            }
            else if (twoString.Equals("Razorback") && oneString.Equals("Trotter"))
            {
                return 10;
            }
            else if (twoString.Equals("Razorback") && oneString.Equals("Snouter"))
            {
                return 10;
            }
            else if (twoString.Equals("Razorback")
                    && oneString.Equals("Leaning Jowler"))
            {
                return 20;
            }
            else if (twoString.Equals("Trotter")
                    && oneString.Equals("Side (No Dot)"))
            {
                return 5;
            }
            else if (twoString.Equals("Trotter") && oneString.Equals("Side (Dot)"))
            {
                return 5;
            }
            else if (twoString.Equals("Trotter") && oneString.Equals("Razorback"))
            {
                return 10;
            }
            else if (twoString.Equals("Trotter") && oneString.Equals("Trotter"))
            {
                return 20;
            }
            else if (twoString.Equals("Trotter") && oneString.Equals("Snouter"))
            {
                return 15;
            }
            else if (twoString.Equals("Trotter")
                    && oneString.Equals("Leaning Jowler"))
            {
                return 20;
            }
            else if (twoString.Equals("Snouter")
                    && oneString.Equals("Side (No Dot)"))
            {
                return 10;
            }
            else if (twoString.Equals("Snouter") && oneString.Equals("Side (Dot)"))
            {
                return 10;
            }
            else if (twoString.Equals("Snouter") && oneString.Equals("Razorback"))
            {
                return 15;
            }
            else if (twoString.Equals("Snouter") && oneString.Equals("Trotter"))
            {
                return 15;
            }
            else if (twoString.Equals("Snouter") && oneString.Equals("Snouter"))
            {
                return 40;
            }
            else if (twoString.Equals("Snouter")
                    && oneString.Equals("Leaning Jowler"))
            {
                return 25;
            }
            else if (twoString.Equals("Leaning Jowler")
                    && oneString.Equals("Side (No Dot)"))
            {
                return 15;
            }
            else if (twoString.Equals("Leaning Jowler")
                    && oneString.Equals("Side (Dot)"))
            {
                return 15;
            }
            else if (twoString.Equals("Leaning Jowler")
                    && oneString.Equals("Razorback"))
            {
                return 20;
            }
            else if (twoString.Equals("Leaning Jowler")
                    && oneString.Equals("Trotter"))
            {
                return 20;
            }
            else if (twoString.Equals("Leaning Jowler")
                    && oneString.Equals("Snouter"))
            {
                return 25;
            }
            else if (twoString.Equals("Leaning Jowler")
                    && oneString.Equals("Leaning Jowler"))
            {
                return 60;
            }
            else
                return 1;
        }

        //Called at the end of each turn
        private static void turnEnd()
        {
            current.totalScore += current.turnScore;
            current.turnScore = 0;

            if (current == player1)
            {

                player1.totalScore = current.totalScore;
                player1.turnScore = 0;
            }

            if (current == player2)
            {
                player2.totalScore = current.totalScore;
                player2.turnScore = 0;
            }

            if (!endChecker())
            {
                if (current == player1)
                    current = player2;
                else
                    current = player1;
            }

        }

        //End checker
        private static Boolean endChecker()
        {
            if (player1.totalScore >= 100 || player2.totalScore >= 100)
            {
                return true;
            }
            else
                return false;
        }

        //Method to clean up after each game, in preparation for the next.
        private static void cleaner()
        {
            player1.totalScore = 0;
            player1.turnScore = 0;
            player2.totalScore = 0;
            player2.turnScore = 0;
            p1RollCount = 0;
            p2RollCount = 0;
            gameOver = false;
            current = player1;
        }

        //Generates a random number between min and max
        internal static int randomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }

        //Tells us if an int 'x' is odd
        internal static Boolean isOdd(int x)
        {
            return x % 2 != 0;
        }
    }
}
