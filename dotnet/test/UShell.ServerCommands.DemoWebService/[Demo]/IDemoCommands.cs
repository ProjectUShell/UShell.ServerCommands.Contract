using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace UShell.ServerCommands {

  public interface IDemoCommands {

    void ProcessAndCountManyManyRecords(CancellationToken ct);

  }

}
