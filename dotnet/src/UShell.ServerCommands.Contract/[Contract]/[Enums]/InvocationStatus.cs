
namespace UShell.ServerCommands {

  public enum InvocationStatus {

    /// <summary> Execution is scheduled. </summary>
    Queued = 0,


    /// <summary> Execution is in Progress (for details see 'StatusMessage').</summary>
    InProgress = 1,


    /// <summary> Execution has been completed successfully. </summary>
    Completed = 2,

    /// <summary> Execution was canceled </summary>
    Canceled = 3,

    /// <summary> Failed (for details see 'StatusMessage').</summary>
    FailedDuringExecution = 4,


    /// <summary> Locked because it is currently executing concurrent invocations. </summary>
    RejectedConcurrencyLock = 5,

    /// <summary> Locked because it was executed to often. </summary>
    RejectedTresholdLock = 6,

    /// <summary> The current user has not the required rights to execute the command. </summary>
    RejectedNoPermission = 7,

    /// <summary> The command is NotImplemented or permanently disabled </summary>
    RejectedPermanentlyUnavailable = 8

  }

}
