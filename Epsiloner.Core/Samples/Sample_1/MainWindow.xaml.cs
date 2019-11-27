using System;
using System.Windows;
using Epsiloner.Cooldowns;

namespace Sample_1
{
    public partial class MainWindow
    {
        private readonly EventCooldown _cooldown;

        public MainWindow()
        {
            InitializeComponent();

            _cooldown = new EventCooldown(
                TimeSpan.FromSeconds(2), 
                Action,
                TimeSpan.FromSeconds(5));
        }

        private void Action()
        {
            MessageBox.Show("Accumulated", "Test");
        }

        private void btnAccumulate_OnClick(object sender, RoutedEventArgs e)
        {
            _cooldown.Accumulate();
        }

        private void btnNow_OnClick(object sender, RoutedEventArgs e)
        {
            _cooldown.Now();
        }
    }
}
