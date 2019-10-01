using System;
//To more easily perform some file operations.
using IO = System.IO;
//BlueRaja's Weighted Randomizer package: https://github.com/BlueRaja/Weighted-Item-Randomizer-for-C-Sharp
using Weighted_Randomizer;
using System.Diagnostics;
using IronPython.Hosting;


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
        static Player player1, player2, current;

        //Number of games to be played is controlled by the .ini file.
        //(Defaults to 1 if no .ini found or 0 if invalid value specified)
        static string games;
        static int gamesInt;

        //True if human is playing.
        static Boolean human;

        //Each AI player has an Int assigned to them.
        //The value of the Int governs which ruleset is used
        static int p1AI, p2AI;

        //Log mode - 0 for basic, 1 for full.
        //Basic keeps track of scores and turns. Full does this, and also logs the rule by which the CPU decided to roll or not.
        static int logMode;

        //Signal Game Over
        static Boolean gameOver = false;

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

        //Entry method for app. Pass a '1' to the program to play Human vs CPU, else CPU v CPU
        static void Main(string[] args)
        {
            //Check for commandline argument
            if (args.Length > 0 && args[0].Equals("1"))
            {
                human = true;
                Trace.WriteLine("Human Mode detected");
            }
            //Setup logging of the console
            consoleLogger();         
            //Run the .ini loader
            iniLoader();
            //Initialise pigs, player names.
            pigInitialiser();

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
                                roll = AIRules.basic(logMode, player1.name, player1.totalScore, player1.turnScore, player2.totalScore);
                            }
                            else if (p1AI == 1)
                            {
                                roll = rollDecisionRandom();
                            }
                            else if (p1AI == 2)
                            {
                                roll = rollAggressive();
                            }
                            else
                            {
                                roll = AIRules.basic(logMode, player1.name, player1.totalScore, player1.turnScore, player2.totalScore);
                            }
                            //If we roll
                            if (roll)
                            {
                                //Increment roll count
                                p1RollCount++;
                                if (logMode == 1)
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
                                if (logMode == 1)
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
                            roll = AIRules.basic(logMode, player2.name, player2.totalScore, player2.turnScore, player1.totalScore);
                        }
                        else if (p2AI == 1)
                        {
                            roll = rollDecisionRandom();
                        }
                        else if (p2AI == 2)
                        {
                            roll = rollAggressive();
                        }
                        else
                        {
                            roll = AIRules.basic(logMode, player1.name, player1.totalScore, player1.turnScore, player2.totalScore);
                        }
                        //If we roll
                        if (roll)
                        {
                            //Increment roll count
                            p2RollCount++;
                            if (logMode == 1)
                                Trace.WriteLine(current.name + ": CPU 1 is Rolling\n");
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
            #endregion
        }

        public static void pigInitialiser()
        {
            //Create our players
            //If a Human is playing, he/she will be P1
            if (human)
            {
                player1 = new Player("Human");
            }
            else
            {
                player1 = new Player("CPU 0", p1AI);
            }
            player2 = new Player("CPU 1", p2AI);

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
                int.TryParse(INIFile.ReadValue("Settings", "Log Mode", appPath + "/Settings.ini"), out logMode);
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
                logMode = 0;
                Trace.WriteLine("Options set to default values\n");

            }
            Trace.WriteLine("---INI DATA END---\n");
        }


        //Rolls pigs, keeps track of turn score
        private static void rollControl()
        {
            int x;
            x = roller();
            if (logMode == 1)
                Trace.WriteLine(current.name + ": Rolled " + oneString + " & " + twoString + ", resulting in a score of: " + x+ "\n");

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

        
        //'Basic' method; Well-rounded, and the most consistent.
        //Falls back on the 'stop at 23 rule', as per Gorman's paper 'Analytics, Pedagogy and the Pass the Pigs Game'.
        
        //lolSoRandom
        public static Boolean rollDecisionRandom()
        {
            //My total, my turn total
            int cpuTotal, cpuTurn;

            //Depending on which CPU is in control
            if (current == player1)
            {
                cpuTotal = current.totalScore;
                cpuTurn = current.turnScore;
            }
            else
            {
                cpuTotal = current.totalScore;
                cpuTurn = current.turnScore;
            }

            //If I have reached or exceeded 100, no need to roll.
            if (cpuTotal + cpuTurn >= 100)
            {
                if (logMode == 1)
                {
                    Trace.WriteLine(current.name + ": Not rolling because I've won.\n");
                }
                return false;
            }
            int x = randomNumber(0, 1000);
            if (isOdd(x))
            {
                Trace.WriteLine(current.name + ": Not rolling because " + x + "(Random)\n");
                return false;
            }
            else
            {
                Trace.WriteLine(current.name + ": Am rolling because " + x + "(Random)\n");
                return true;
            }
        }

        public static Boolean rollDecisionExternal()
        {
            Boolean result=false;
            var py = Python.CreateRuntime();
            try
            {
               dynamic rule = py.UseFile("AI_Test.py");
               result=rule.testRule();
               Trace.WriteLine("Ran python script - Resulting in " + result + "\n");
            }
            catch(Exception e)
            {
                Trace.WriteLine("Exception error when firing custom AI Rule");
                Trace.WriteLine(e.ToString());
                Trace.WriteLine("Ending game");
                gameOver = true;
            }
            return result;
        }

        //Agressive thought process that loves to roll
        public static Boolean rollAggressive()
        {
            //My total, my turn total
            int cpuTotal, cpuTurn, opponentTotal;

            //Depending on which CPU is in control
            if (current == player1)
            {
                cpuTotal = current.totalScore;
                cpuTurn = current.turnScore;
                opponentTotal = player2.totalScore;
            }
            else
            {
                cpuTotal = current.totalScore;
                cpuTurn = current.turnScore;
                opponentTotal = player1.totalScore;
            }

            //If I have reached or exceeded 100, no need to roll.
            if (cpuTotal + cpuTurn >= 100)
            {
                if (logMode == 1)
                {
                    Trace.WriteLine(current.name + ": Not rolling because I've won.\n");
                }
                return false;
            }
            //Aggressive but not stupid; unless the opponent is winning big, be cool with fifty.
            else if (cpuTurn >= 50 && opponentTotal <= 85)
            {
                Trace.WriteLine(current.name + ": Not rolling because 50 points is enough in one turn\n");
                return false;
            }
            //Otherwise roll.
            else
            {
                Trace.WriteLine(current.name + ": Rolling because I'm aggressive\n");
                return true;
            }
        }

        //'New' methods; "What are the odds on my roll improving my position; how much; is it worth it?"


        //Roll method
        private static int roller()
        {
            //Get two pigs
            oneString = pigOne.NextWithReplacement();
            twoString = pigTwo.NextWithReplacement();

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
        private static int randomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }

        //Tells us if an int 'x' is odd
        private static Boolean isOdd(int x)
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
