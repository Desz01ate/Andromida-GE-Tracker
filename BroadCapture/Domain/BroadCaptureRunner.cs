using BroadCapture.Domain.Event;
using BroadCapture.Domain.Interface;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BroadCapture.Domain
{
    public class BroadCaptureRunner : IBroadCapture
    {
        public event BroadEvent.BroadCapturedEventHandler BroadCaptured;
        public event BroadEvent.MaintenanceModeActivatedEventHandler MaintenanceModeActivated;
        private readonly GameEngineObservator gameEngineObservator;
        private string latestMessage = string.Empty;
        private DateTime lastUpdate = DateTime.Now;
        public BroadCaptureRunner()
        {
            this.gameEngineObservator = new GameEngineObservator();
        }

        public async Task RunAsync(CancellationTokenSource cancellationTokenSource)
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                if (gameEngineObservator.TryReadBroadMessage(out var broadMessage))
                {
                    if (latestMessage != broadMessage)
                    {
                        BroadCaptured?.Invoke(broadMessage);
                        latestMessage = broadMessage;
                    }
                }
            }
        }
    }
}
