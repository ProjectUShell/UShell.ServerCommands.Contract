using System;

namespace UShell.ServerCommands {

  public partial class CommandExecutor {

    // NESTED: InvocationResult //

    /// <summary>
    /// compatible and mappable to 'InvocationStatus'
    /// </summary>
    public enum InvocationResult {
      Completed = 2,
      Canceled = 3,
      Failed = 4
    }

  }

}
