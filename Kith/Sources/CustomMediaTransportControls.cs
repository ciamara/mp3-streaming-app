using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;

namespace Kith
{
    public sealed class CustomMediaTransportControls : MediaTransportControls
    {
        private AppBarButton previousTrackButton;
        private AppBarButton nextTrackButton;

        private AppBarToggleButton repeatButton;
        private AppBarToggleButton shuffleButton;

        public event EventHandler PreviousTrackClicked;
        public event EventHandler NextTrackClicked;
        public event EventHandler RepeatClicked;
        public event EventHandler ShuffleClicked;

        public CustomMediaTransportControls()
        {
            this.DefaultStyleKey = typeof(CustomMediaTransportControls);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            previousTrackButton = GetTemplateChild("PreviousTrackButton") as AppBarButton;
            nextTrackButton = GetTemplateChild("NextTrackButton") as AppBarButton;

            repeatButton = GetTemplateChild("CustomRepeatButton") as AppBarToggleButton;
            shuffleButton = GetTemplateChild("ShuffleButton") as AppBarToggleButton;

            if (previousTrackButton != null)
            {
                previousTrackButton.Click -= PreviousTrackButton_Click;
                previousTrackButton.Click += PreviousTrackButton_Click;
            }
            if (nextTrackButton != null)
            {
                nextTrackButton.Click -= NextTrackButton_Click;
                nextTrackButton.Click += NextTrackButton_Click;
            }
            if (repeatButton != null)
            {
                repeatButton.Click -= RepeatButton_Click;
                repeatButton.Click += RepeatButton_Click;
            }
            if (shuffleButton != null)
            {
                shuffleButton.Click -= ShuffleButton_Click;
                shuffleButton.Click += ShuffleButton_Click;
            }
        }

        public void SetRepeatState(bool state)
        {
            if (repeatButton != null)
            {
                repeatButton.IsChecked = state;
            }
        }

        public void SetShuffleState(bool state)
        {
            if (shuffleButton != null)
            {
                shuffleButton.IsChecked = state;
            }
        }

        private void PreviousTrackButton_Click(object sender, RoutedEventArgs e)
        {
            PreviousTrackClicked?.Invoke(this, EventArgs.Empty);
        }

        private void NextTrackButton_Click(object sender, RoutedEventArgs e)
        {
            NextTrackClicked?.Invoke(this, EventArgs.Empty);
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            RepeatClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            ShuffleClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}