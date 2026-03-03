using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace UShell.ServerCommands {

  public class DemoService : IDemoCommands {

    public int ProcessAndCountManyManyRecords(CancellationToken ct) {

      int counter = 0;

      while (!ct.IsCancellationRequested && counter < 20) {

        Thread.Sleep(1000);
        counter++;

      }

      return counter;
    }

  }

}
