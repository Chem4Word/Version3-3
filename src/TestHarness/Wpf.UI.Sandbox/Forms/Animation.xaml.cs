using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace Wpf.UI.Sandbox.Forms
{
    /// <summary>
    /// Interaction logic for Animation.xaml
    /// </summary>
    public partial class Animation : Window
    {
        public Animation()
        {
            InitializeComponent();
        }

        private void OnClick_ButtonBase(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                switch (button.Tag)
                {
                    case "Start":
                        StartAnimation();
                        break;

                    case "Stop":
                        StopAnimation();
                        break;
                }
            }
        }

        private void StartAnimation()
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(5));
            DoubleAnimation animation = new DoubleAnimation
            {
                Duration = duration,
                From = 0,
                To = 100
            };
            Storyboard.SetTarget(animation, ProgressBar);
            Storyboard.SetTargetProperty(animation, new PropertyPath(RangeBase.ValueProperty));
            Storyboard storyboard = new Storyboard
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        private void StopAnimation()
        {
            ProgressBar.BeginAnimation(RangeBase.ValueProperty, null);
        }
    }
}
