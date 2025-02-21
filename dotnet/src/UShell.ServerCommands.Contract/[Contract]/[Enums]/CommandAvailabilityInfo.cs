
namespace UShell.ServerCommands {

  public enum CommandAvailabilityInfo {

    /// <summary> A new command execution can be startet right now. </summary>
    ExecutionPossible = 0,

    /// <summary> Need to wait until the command gets available again,
    /// because it is currently executing concurrent invocations.
    /// (UI could display a User-Icon overlay) </summary>
    ConcurrencyLock = 5,

    /// <summary> Need to wait until the command gets available again,
    /// because it was executed to often. (UI could display a Clock-Icon overlay) </summary>
    TresholdLock = 6,

    /// <summary> The current user has not the required rights to execute the command.
    /// (UI could display a Lock-Icon overlay)</summary>
    NoPermission = 7,

    /// <summary> The command is NotImplemented or permanently disabled
    /// (UI could display a X-Icon overlay or hide the control) </summary>
    PermanentlyUnavailable = 8

  }

}
