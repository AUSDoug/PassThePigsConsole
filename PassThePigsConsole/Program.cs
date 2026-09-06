using System;
//To more easily perform some file operations.
using IO = System.IO;
//BlueRaja's Weighted Randomizer package: https://github.com/BlueRaja/Weighted-Item-Randomizer-for-C-Sharp
using Weighted_Randomizer;
using System.Diagnostics;


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

        //Governs which decision making process is used by the AI
        //String for describing the logic used
        static int p1AI, p2AI;
        static string aiDesc;

        //Log mode - 0 for basic, 1 for full.
        //Basic keeps track of scores and turns. Full does this, and also logs the rule by which the CPU decided to roll or not.
        internal static Boolean logMode;

        //Signal Game Over
        static Boolean gameOver = false;

        //Sprite window shown during Human vs AI (null in AI vs AI).
        static PigRollWindow pigWindow;

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
            if (human)
            {
                Trace.WriteLine("Human Mode detected");
            }
            //Setup logging of the console
            consoleLogger();         
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
                    Trace.WriteLine("AI vs AI setup: CPU 0 AI=" + p1AI + ", CPU 1 AI=" + p2AI
                        + ", Games=" + gamesInt + ", Log Mode=" + logMode + "\n");
                }
            }
            //Initialise pigs, player names.
            pigInitialiser();

            //Open the pig sprite window for Human vs AI.
            if (human)
            {
                pigWindow = new PigRollWindow();
                pigWindow.Open();
            }

            #region gameLogic
            //Game logic begin.
            //While there are games to be played
            while (gamesInt > 0)
            {
                //Initialise count to 0
                turnCount = 0;
                Trace.WriteLine("===Game Start===\n");
                //While the game is running.
                while (!gameOver)
                {
                    //Increment turn count
                    turnCount++;
                    //Display game info
                    Trace.WriteLine("       ##### Beginning Turn " + turnCount + " #####\n");
                    Trace.WriteLine("##" + player1.name + ": Total Score is " + player1.totalScore + " after " + p1RollCount + " rolls ##\n");
                    Trace.WriteLine("##" + player2.name + ": Total Score is " + player2.totalScore + " after " + p2RollCount + " rolls ##\n");

                    //While CPU0 is in control
                    #region CPU0
                    if (!human)
                    {
                        Trace.WriteLine("\n");
                        while (current == player1)
                        {
                            Boolean roll;
                            //roll = rollDecisionBasic();

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
                                if (logMode == true)
                                    Trace.WriteLine(current.name + ": CPU 0 is Rolling\n");
                                //Roll
                                rollControl();
                            }
                            //If they're not rolling
                            else
                            {
                                Trace.WriteLine(current.name + " has passed the pigs\n");
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
                                    Trace.WriteLine("Game Over:" + current.name + " has won!\n");
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
                        Trace.WriteLine("\n");
                        while (current == player1) {
                            Boolean roll;
                            //roll = rollDecisionBasic();
                            Trace.WriteLine("Total Score = " + player1.totalScore);
                            Trace.WriteLine("Turn Score = " + player1.turnScore);
                            Trace.WriteLine("'1' to Roll, anything else to Pass the Pigs\n");
                            string line = Console.ReadLine();
                            if (line == "1")
                            {
                                roll = true;
                            }
                            else
                                roll = false;
                            //If we roll
                            if (roll)
                            {
                                //Increment roll count
                                p1RollCount++;
                                if (logMode == true)
                                    Trace.WriteLine(current.name + ": is Rolling\n");
                                //Roll
                                rollControl();
                            }
                            //If they're not rolling
                            else
                            {
                                Trace.WriteLine(current.name + " has passed the pigs\n");
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
                                    Trace.WriteLine("Game Over:" + current.name + " has won!\n");
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
                        Trace.WriteLine("\n");
                        Boolean roll;
                        //roll = rollDecisionBasic();

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
                            if (logMode == true)
                                Trace.WriteLine(current.name + ": CPU 0 is Rolling\n");
                            //Roll
                            rollControl();
                        }
                        //If they're not rolling
                        else
                        {
                            Trace.WriteLine(current.name + " has passed the pigs\n");
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
                                Trace.WriteLine("Game Over:" + current.name + " has won!\n");
                                p2Wins++;
                                break;
                            }
                        }

                    }
                    #endregion
                }
                //Print summary of game once it is over
                Trace.WriteLine("------ Game Summary ------\n");
                Trace.WriteLine("Turns Started: " + turnCount + "\n");
                Trace.WriteLine(player1.name + " Score " + player1.totalScore + "\n");
                Trace.WriteLine(player2.name + " Score " + player2.totalScore + "\n");
                Trace.WriteLine(player1.name + " Rolls " + p1RollCount + "\n");
                Trace.WriteLine(player2.name + " Rolls " + p2RollCount + "\n");
                //Number of games remaining
                gamesInt = gamesInt - 1;
                //Cleanup
                cleaner();
            }
            //Once all games have been played
            Trace.WriteLine("------ Session Summary ------\n");
            Trace.WriteLine("Wins for " + player1.name + ": " + p1Wins + "\n");
            Trace.WriteLine("Wins for " + player2.name + ": " + p2Wins + "\n");
            Trace.WriteLine("-----Ending Session at " + DateTime.Now+ "-----\n");

            //Keep the sprite window up until the human dismisses it.
            if (pigWindow != null)
            {
                Trace.WriteLine("Press Enter to close.\n");
                Console.ReadLine();
                pigWindow.Close();
            }
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
            Trace.WriteLine("---INI DATA BEGIN:---\n");
            if (IO.File.Exists(appPath + "/Settings.ini\n"))
            {
                Trace.WriteLine("INI File Found\n");
                games = INIFile.ReadValue("Settings", "Games", appPath + "/Settings.ini");
                Trace.WriteLine("Number of games read as " + games + "\n");
                int.TryParse(games, out gamesInt);
                Trace.WriteLine("Number of games initialised to " + gamesInt+"\n");
                int.TryParse(INIFile.ReadValue("Settings", "CPU 0 AI", appPath + "/Settings.ini"), out p1AI);
                Trace.WriteLine("CPU 0 AI initialised to " + p1AI + "\n");
                int.TryParse(INIFile.ReadValue("Settings", "CPU 1 AI", appPath + "/Settings.ini"), out p2AI);
                Trace.WriteLine("CPU 1 AI initialised to " + p2AI + "\n");
                bool.TryParse(INIFile.ReadValue("Settings", "Log Mode", appPath + "/Settings.ini"), out logMode);
                Trace.WriteLine("Log Mode initialised to " + logMode+ "\n");
            }
            else
            {
                Trace.WriteLine("No INI File Found\n");
                INIFile.WriteValue("Settings", "Games", "1", appPath + "/Settings.ini");
                INIFile.WriteValue("Settings", "CPU 0 AI", "0", appPath + "/Settings.ini");
                INIFile.WriteValue("Settings", "CPU 1 AI", "0", appPath + "/Settings.ini");
                INIFile.WriteValue("Settings", "Log Mode", "0", appPath + "/Settings.ini");
                gamesInt = 1;
                p1AI = 0;
                p2AI = 0;
                logMode = false;
                Trace.WriteLine("Options set to default values\n");

            }
            Trace.WriteLine("---INI DATA END---\n");
        }


        //Rolls pigs, keeps track of turn score
        private static void rollControl()
        {
            oneString = pigOne.NextWithReplacement();
            twoString = pigTwo.NextWithReplacement();
            int x = Score(oneString, twoString);
            if (logMode == true)
                Trace.WriteLine(current.name + ": Rolled " + oneString + " & " + twoString + ", resulting in a score of: " + x+ "\n");

            //Show the roll in the sprite window (Human vs AI only).
            if (pigWindow != null)
                pigWindow.ShowRoll(current.name, oneString, twoString, x);

            //Pig Out, turn score to 0
            if (x == 0)
            {
                Trace.WriteLine(current.name + ": Pig Out!\n");
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

        private static void consoleLogger()
        {
                Trace.Listeners.Clear();

                TextWriterTraceListener twtl = new TextWriterTraceListener(appPath + "/PassThePigs.log", AppDomain.CurrentDomain.FriendlyName);
                twtl.Name = "TextLogger";
                twtl.TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime;

                ConsoleTraceListener ctl = new ConsoleTraceListener(false);
                ctl.TraceOutputOptions = TraceOptions.DateTime;

                Trace.Listeners.Add(twtl);
                Trace.Listeners.Add(ctl);
                Trace.AutoFlush = true;

            Trace.WriteLine("-----Starting Session at " + DateTime.Now + "-----\n");

        }
    }
}
