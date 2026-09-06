using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

//File:   PigRollWindow.cs
//The game window for Human vs AI. It runs on its own STA thread with its own
//Dispatcher so the synchronous game loop on the main thread never freezes it.
//It shows the turn number, both players' scores, the two pigs from the latest
//roll, and Roll / Pass buttons that drive the human's turn - in this mode the
//console is only used for logging. AI vs AI never creates this window.

namespace PassThePigsConsole
{
    internal sealed class PigRollWindow
    {
        private readonly ManualResetEventSlim ready = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim closed = new ManualResetEventSlim(false);
        private readonly BlockingCollection<bool> humanInput = new BlockingCollection<bool>(1);
        private readonly Random rng = new Random();

        private Dispatcher dispatcher;
        private Window window;
        private Image pig1;
        private Image pig2;
        private TextBlock turnText;
        private TextBlock p1Text;
        private TextBlock p2Text;
        private TextBlock caption;
        private Button rollButton;
        private Button passButton;

        //Live game state, supplied by Program.
        private Func<int> getTurn;
        private Player p1;
        private Player p2;
        private Func<bool> humanIsActive;

        private volatile bool waitingForInput;
        private volatile bool gameOver;

        //Spawns the UI thread and blocks until the window is up.
        public void Open(Func<int> turnGetter, Player player1, Player player2, Func<bool> humanActiveGetter)
        {
            getTurn = turnGetter;
            p1 = player1;
            p2 = player2;
            humanIsActive = humanActiveGetter;

            Thread ui = new Thread(() =>
            {
                dispatcher = Dispatcher.CurrentDispatcher;
                Build();
                window.Show();

                DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher)
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
                timer.Tick += (s, e) => RefreshLabels();
                timer.Start();

                ready.Set();
                Dispatcher.Run();
            });
            ui.SetApartmentState(ApartmentState.STA);
            ui.IsBackground = true;
            ui.Start();
            ready.Wait();
        }

        private void Build()
        {
            turnText = Centered("");
            turnText.FontSize = 18;
            turnText.FontWeight = FontWeights.Bold;
            turnText.Margin = new Thickness(0, 0, 0, 4);

            p1Text = Centered("");
            p2Text = Centered("");
            p1Text.FontSize = 15;
            p2Text.FontSize = 15;
            p1Text.Margin = new Thickness(0, 0, 24, 0);

            StackPanel scores = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            scores.Children.Add(p1Text);
            scores.Children.Add(p2Text);

            pig1 = new Image { Width = 185, Height = 185, Margin = new Thickness(4) };
            pig2 = new Image { Width = 185, Height = 185, Margin = new Thickness(4) };
            StackPanel pigs = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            pigs.Children.Add(pig1);
            pigs.Children.Add(pig2);

            caption = Centered("Waiting for the first roll...");
            caption.Margin = new Thickness(0, 2, 0, 10);

            rollButton = new Button { Content = "Roll", Width = 110, Height = 32, Margin = new Thickness(6, 0, 6, 0), IsEnabled = false };
            passButton = new Button { Content = "Pass", Width = 110, Height = 32, Margin = new Thickness(6, 0, 6, 0), IsEnabled = false };
            rollButton.Click += (s, e) => { if (!gameOver) Submit(true); };
            passButton.Click += (s, e) => { if (gameOver) window.Close(); else Submit(false); };

            StackPanel buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            buttons.Children.Add(rollButton);
            buttons.Children.Add(passButton);

            StackPanel root = new StackPanel { Margin = new Thickness(12) };
            root.Children.Add(turnText);
            root.Children.Add(scores);
            root.Children.Add(pigs);
            root.Children.Add(caption);
            root.Children.Add(buttons);

            window = new Window
            {
                Title = "Pass The Pigs",
                Width = 460,
                Height = 420,
                ResizeMode = ResizeMode.CanMinimize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = Brushes.White,
                Content = root
            };
            window.Closed += (s, e) =>
            {
                closed.Set();
                //Dismissed mid-game: the console loop is blocked waiting on us, so just exit.
                if (!gameOver) Environment.Exit(0);
            };
        }

        private static TextBlock Centered(string text)
        {
            return new TextBlock
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
        }

        //--- called from the game (main) thread ---

        //Enables the buttons and blocks until the human presses one. true = roll.
        public bool GetHumanDecision()
        {
            waitingForInput = true;
            return humanInput.Take();
        }

        //Updates the pig images and caption after any roll (human or AI).
        public void ShowRoll(string playerName, string position1, string position2, int score)
        {
            if (dispatcher == null) return;

            ImageSource img1 = PigSprites.Image(position1, rng);
            ImageSource img2 = PigSprites.Image(position2, rng);
            string text = playerName + ":  " + position1 + "  +  " + position2
                + (score == 0 ? "   →   PIG OUT" : "   →   " + score);

            dispatcher.BeginInvoke(new Action(() =>
            {
                pig1.Source = img1;
                pig2.Source = img2;
                caption.Text = text;
            }));
        }

        //Switches the window into its end-of-session state; the Pass button becomes Close.
        public void ShowGameOver(string message)
        {
            gameOver = true;
            if (dispatcher == null) return;

            dispatcher.BeginInvoke(new Action(() =>
            {
                turnText.Text = "Game Over";
                caption.Text = message;
                rollButton.IsEnabled = false;
                passButton.Content = "Close";
                passButton.IsEnabled = true;
            }));
        }

        //Blocks until the window is closed by the user.
        public void WaitForClose()
        {
            closed.Wait();
        }

        //--- UI thread ---

        private void Submit(bool roll)
        {
            if (!waitingForInput) return;
            waitingForInput = false;
            rollButton.IsEnabled = false;
            passButton.IsEnabled = false;
            humanInput.Add(roll);
        }

        private void RefreshLabels()
        {
            if (gameOver) return;

            turnText.Text = "Turn " + getTurn();

            bool humanTurn = humanIsActive();
            p1Text.Text = p1.name + ": " + p1.totalScore + (p1.turnScore > 0 ? "  (+" + p1.turnScore + ")" : "");
            p2Text.Text = p2.name + ": " + p2.totalScore + (p2.turnScore > 0 ? "  (+" + p2.turnScore + ")" : "");
            p1Text.FontWeight = humanTurn ? FontWeights.Bold : FontWeights.Normal;
            p2Text.FontWeight = humanTurn ? FontWeights.Normal : FontWeights.Bold;

            if (waitingForInput)
            {
                rollButton.IsEnabled = true;
                passButton.IsEnabled = p1.turnScore > 0;   //can't pass before rolling
            }
        }
    }
}
