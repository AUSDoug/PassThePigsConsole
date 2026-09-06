using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

//File:   PigRollWindow.cs
//A small window kept open during Human vs AI that shows the two pigs from the
//most recent roll. It runs on its own STA thread with its own Dispatcher so the
//console game loop (which blocks on Console.ReadLine) never freezes it.
//AI vs AI play never creates this window.

namespace PassThePigsConsole
{
    internal sealed class PigRollWindow
    {
        private readonly ManualResetEventSlim ready = new ManualResetEventSlim(false);
        private readonly Random rng = new Random();

        private Dispatcher dispatcher;
        private Window window;
        private Image pig1;
        private Image pig2;
        private TextBlock caption;

        //Spawns the UI thread and blocks until the window is up.
        public void Open()
        {
            Thread ui = new Thread(() =>
            {
                dispatcher = Dispatcher.CurrentDispatcher;
                Build();
                window.Show();
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
            pig1 = new Image { Width = 210, Height = 210, Margin = new Thickness(4) };
            pig2 = new Image { Width = 210, Height = 210, Margin = new Thickness(4) };

            caption = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 8),
                FontSize = 14,
                Text = "Waiting for the first roll..."
            };

            StackPanel pigs = new StackPanel { Orientation = Orientation.Horizontal };
            pigs.Children.Add(pig1);
            pigs.Children.Add(pig2);

            StackPanel root = new StackPanel { Margin = new Thickness(10) };
            root.Children.Add(caption);
            root.Children.Add(pigs);

            window = new Window
            {
                Title = "Pass The Pigs",
                Width = 470,
                Height = 300,
                ResizeMode = ResizeMode.CanMinimize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = Brushes.White,
                Content = root
            };
        }

        //Called from the game thread after every roll.
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

        public void Close()
        {
            if (dispatcher == null) return;
            dispatcher.BeginInvoke(new Action(() =>
            {
                window.Close();
                dispatcher.InvokeShutdown();
            }));
        }
    }
}
