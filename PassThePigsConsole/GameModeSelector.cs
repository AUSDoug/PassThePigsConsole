using System;
using System.Windows;
using System.Windows.Controls;

//File:   GameModeSelector.cs
//Small WPF pop-ups used at program start:
//  1. PromptForHumanMode  - choose 'Human vs AI' or 'AI vs AI'.
//  2. PromptForAiConfig   - (AI vs AI only) pick each CPU's ruleset, the number
//                           of games to play, and whether logging is verbose.

namespace PassThePigsConsole
{
    //The choices made on the AI vs AI configuration screen.
    internal sealed class AiGameConfig
    {
        public int Player1AI;
        public int Player2AI;
        public int Games;
        public bool LogMode;
    }

    internal static class GameModeSelector
    {
        //The AI rulesets, in id order (index == the id used by Program / the .ini).
        private static readonly string[] AiRulesets = { "Basic", "Random", "Aggressive", "Expert", "EV (stop at 23)" };

        //Displays a pop-up window with two buttons.
        //Returns true if 'Human vs AI' was selected, false if 'AI vs AI' was selected.
        public static bool PromptForHumanMode()
        {
            bool humanSelected = false;

            Window window = new Window
            {
                Title = "Pass The Pigs - Select Game Mode",
                Width = 300,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Topmost = true
            };

            StackPanel panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBlock prompt = new TextBlock
            {
                Text = "Choose a game mode:",
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Button humanVsAiButton = new Button
            {
                Content = "Human vs AI",
                Width = 150,
                Margin = new Thickness(0, 0, 0, 10)
            };
            humanVsAiButton.Click += (sender, e) =>
            {
                humanSelected = true;
                window.Close();
            };

            Button aiVsAiButton = new Button
            {
                Content = "AI vs AI",
                Width = 150
            };
            aiVsAiButton.Click += (sender, e) =>
            {
                humanSelected = false;
                window.Close();
            };

            panel.Children.Add(prompt);
            panel.Children.Add(humanVsAiButton);
            panel.Children.Add(aiVsAiButton);

            window.Content = panel;
            window.ShowDialog();

            return humanSelected;
        }

        //Displays the AI vs AI configuration screen, pre-populated with the supplied
        //defaults (typically the values just read from Settings.ini).
        //Returns the chosen configuration, or null if the window was closed without
        //pressing 'Start' (in which case the caller should keep its defaults).
        public static AiGameConfig PromptForAiConfig(int player1Default, int player2Default, int gamesDefault, bool logDefault)
        {
            AiGameConfig result = null;

            Window window = new Window
            {
                Title = "Pass The Pigs - AI vs AI Setup",
                Width = 320,
                Height = 320,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Topmost = true
            };

            StackPanel panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(15)
            };

            ComboBox cpu0Combo = BuildRulesetCombo(player1Default);
            ComboBox cpu1Combo = BuildRulesetCombo(player2Default);

            TextBox gamesBox = new TextBox
            {
                Text = (gamesDefault > 0 ? gamesDefault : 1).ToString(),
                Margin = new Thickness(0, 0, 0, 10)
            };

            CheckBox logCheck = new CheckBox
            {
                Content = "Verbose logging",
                IsChecked = logDefault,
                Margin = new Thickness(0, 0, 0, 15)
            };

            Button startButton = new Button
            {
                Content = "Start",
                Width = 150,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            startButton.Click += (sender, e) =>
            {
                int games;
                if (!int.TryParse(gamesBox.Text, out games) || games < 1)
                {
                    games = 1;
                }

                result = new AiGameConfig
                {
                    Player1AI = cpu0Combo.SelectedIndex,
                    Player2AI = cpu1Combo.SelectedIndex,
                    Games = games,
                    LogMode = logCheck.IsChecked == true
                };
                window.Close();
            };

            panel.Children.Add(new TextBlock { Text = "CPU 0 AI:", Margin = new Thickness(0, 0, 0, 3) });
            panel.Children.Add(cpu0Combo);
            panel.Children.Add(new TextBlock { Text = "CPU 1 AI:", Margin = new Thickness(0, 8, 0, 3) });
            panel.Children.Add(cpu1Combo);
            panel.Children.Add(new TextBlock { Text = "Number of games:", Margin = new Thickness(0, 8, 0, 3) });
            panel.Children.Add(gamesBox);
            panel.Children.Add(logCheck);
            panel.Children.Add(startButton);

            window.Content = panel;
            window.ShowDialog();

            return result;
        }

        //Builds a ruleset drop-down with the given id pre-selected.
        private static ComboBox BuildRulesetCombo(int selectedId)
        {
            ComboBox combo = new ComboBox { Margin = new Thickness(0, 0, 0, 5) };
            foreach (string name in AiRulesets)
            {
                combo.Items.Add(name);
            }
            combo.SelectedIndex = (selectedId >= 0 && selectedId < AiRulesets.Length) ? selectedId : 0;
            return combo;
        }
    }
}
