using Logging.SmartStandards;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace UShell.ServerCommands {

  public class DemoService : IDemoCommands {

    public void ProcessAndCountManyManyRecords(CancellationToken ct) {

      int counter = 0;

      while (!ct.IsCancellationRequested && counter < 12) {

        DevLogger.LogInformation($"TICK... {counter}");
        Thread.Sleep(1000);
        counter++;

      }

      if (ct.IsCancellationRequested) {
        DevLogger.LogInformation($"CANCELLED");
      }
      else {
        DevLogger.LogInformation($"COMPLETED");
      }

    }

  }

}
