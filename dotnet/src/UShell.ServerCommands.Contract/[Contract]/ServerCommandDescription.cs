using System.Diagnostics;

namespace UShell.ServerCommands {

  [DebuggerDisplay(nameof(ServerCommandDescription) + " ({CommandName})")]
  public class ServerCommandDescription {

    /// <summary>
    /// only Unique scoped to the current Endpoint
    /// </summary>
    public string CommandName { get; set; }

    /// <summary>
    /// More or less informative here, just an indicator to check that an
    /// string array of the exact same length must be provided when triggering
    /// an execution of the current command. Any validation or parsing is highly
    /// individual per concrete implementation - see Description text! 
    /// </summary>
    public string[] ArgumentNames { get; set; }

    /// <summary>
    /// Informative text that describes what the command is doing
    /// </summary>
    public string Description { get; set; }

  }

}
