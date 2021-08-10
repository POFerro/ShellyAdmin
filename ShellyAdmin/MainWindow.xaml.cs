using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ShellyAdmin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel => (MainViewModel)this.DataContext;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new MainViewModel();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.ShellyManager.ScheduleShelliesRefresh();

        }

        private void Host_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // for .NET Core you need to add UseShellExecute = true
            // see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void ReleaseNotes_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // for .NET Core you need to add UseShellExecute = true
            // see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            var shelly = ((Button)e.Source).DataContext as POF.Shelly.ShellyInfo;
            try
            {
                await shelly.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating shelly: {shelly.Name}->{ex}");
            }
            e.Handled = true;
        }

        private async void UpdateAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await this.ViewModel.ShellyManager.UpdateAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating shellies->{ex}");
            }
            e.Handled = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            this.ViewModel.Dispose();

            base.OnClosed(e);
        }
    }
}
