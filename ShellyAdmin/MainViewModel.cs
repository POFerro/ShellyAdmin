using POF.Common;
using POF.Shelly;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Zeroconf;

namespace ShellyAdmin
{
    public class MainViewModel : BindableBase, IDisposable
    {
        public MainViewModel()
        {
            this.ShellyManager = new ShellyManager(true);
            this.ShellyManager.ShelliesRefreshed += ShellyManager_ShelliesRefreshed;
        }

        private void ShellyManager_ShelliesRefreshed(object sender, ShelliesRefreshedEvent e)
        {
            this.TotalPower = e.FoundShellies
                                    .Where(f => f.ReadInfoError == null)
                                    .SelectMany(f => f.Components.OfType<ShellySwitch>())
                                    .Sum(sw => sw.APower);
            this.TotalEnergy = e.FoundShellies
                                    .Where(f => f.ReadInfoError == null)
                                    .SelectMany(f => f.Components.OfType<ShellySwitch>())
                                    .Sum(sw => sw.AEnergy);
        }

        public ShellyManager ShellyManager { get; private set; }

        private decimal? _totalPower;
        public decimal? TotalPower
        {
            get { return _totalPower; }
            set { SetProperty(ref _totalPower, value); }
        }

        private decimal? _totalEnergy;
        public decimal? TotalEnergy
        {
            get { return _totalEnergy; }
            set { SetProperty(ref _totalEnergy, value); }
        }

        public void Dispose()
        {
            this.ShellyManager.Dispose();
        }
    }
}