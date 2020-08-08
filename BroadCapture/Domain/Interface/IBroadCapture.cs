using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BroadCapture.Domain.Interface
{
    public interface IBroadCapture
    {
        event Event.BroadEvent.BroadCapturedEventHandler BroadCaptured;
        event Event.BroadEvent.MaintenanceModeActivatedEventHandler MaintenanceModeActivated;
        Task RunAsync(CancellationTokenSource cancellationTokenSource = null);
    }
}
