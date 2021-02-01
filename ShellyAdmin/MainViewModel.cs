using POF.WPF;
using System;
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
using Zeroconf;

namespace ShellyAdmin
{
    public class FoundShellyInfo
    {
        public IZeroconfHost Host { get; set; }
        public ShellyInfo Info { get; set; }
        public ErrorDetails ErrorDetails { get; set; }
    }

    public class ErrorDetails
    {
        public HttpStatusCode StatusCode { get; internal set; }
        public string ReasonPhrase { get; internal set; }
        public string ErrorMessage { get; internal set; }
    }

    public class MainViewModel : BindableBase, IDisposable
    {
        private HttpClient http = new HttpClient();

        private Timer refreshTimer;

        public MainViewModel()
        {
            this.refreshTimer = new Timer(this.RefreshShelliesTimerCallback, null, 0, 3000);
        }

        private FoundShellyInfo[] _foundShellies;
        public FoundShellyInfo[] FoundShellies
        {
            get { return _foundShellies; }
            protected set { SetProperty(ref _foundShellies, value); }
        }

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

        private Task refreshTask = Task.CompletedTask;
        private void RefreshShelliesTimerCallback(object state)
        {
            this.refreshTask = this.RefreshShelliesData();
        }

        private bool shouldRefreshShellies = true;
        private async Task RefreshShelliesData()
        {
            if (this.shouldRefreshShellies)
            {
                await this.RefreshShellies();
                this.shouldRefreshShellies = false;
            }
            else
            {
                await Task.WhenAll(this.FoundShellies.Where(f => f.Info != null).Select(shelly => shelly.Info.RefreshInfo(this.http)));

                this.TotalPower = this.FoundShellies
                                      .Where(f => f.Info != null)
                                      .SelectMany(f => f.Info.Components.OfType<ShellySwitch>())
                                      .Sum(sw => sw.APower);
                this.TotalEnergy = this.FoundShellies
                                       .Where(f => f.Info != null)
                                       .SelectMany(f => f.Info.Components.OfType<ShellySwitch>())
                                       .Sum(sw => sw.AEnergy);
            }
        }

        public void ScheduleShelliesRefresh()
        {
            this.shouldRefreshShellies = true;
        }

        protected async Task RefreshShellies()
        {
            var responses = await ZeroconfResolver.ResolveAsync("_hap._tcp.local.");
            var shelies = responses
                .Where(host => host.Services["_hap._tcp.local."].Properties
                                   .Any(pSet => pSet.TryGetValue("md", out string mdValue) &&
                                                mdValue.StartsWith("shelly", StringComparison.OrdinalIgnoreCase)
                                       )
                       )
                .ToList();
            this.FoundShellies =
                (await Task.WhenAll(
                    shelies.Select(async shelly =>
                    {
                        var response = await http.GetAsync($"http://{shelly.IPAddress}/rpc/Shelly.GetInfo");

                        return new FoundShellyInfo
                        {
                            Host = shelly,
                            Info = response.IsSuccessStatusCode ? JsonSerializer.Deserialize<ShellyInfo>(await response.Content.ReadAsStringAsync())
                                                                  : null,
                            ErrorDetails = !response.IsSuccessStatusCode ? new ErrorDetails { StatusCode = response.StatusCode, ReasonPhrase = response.ReasonPhrase, ErrorMessage = await response.Content.ReadAsStringAsync() }
                                                                         : null
                        };
                    })
                )
                ).ToArray();
        }

        public void Dispose()
        {
            this.refreshTimer.Dispose();

            refreshTask.Wait();

            this.http.Dispose();
        }
    }
}