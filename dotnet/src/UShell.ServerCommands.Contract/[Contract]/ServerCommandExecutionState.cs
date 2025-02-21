using System.Diagnostics;

namespace UShell.ServerCommands {

  [DebuggerDisplay(nameof(ServerCommandExecutionState) + " ({ExecutionId})")]
  public class ServerCommandExecutionState {

    /// <summary>
    /// The UniqueId of the current execution
    /// </summary>
    public string ExecutionId { get; internal set; }

    public string CommandName { get; internal set; }

    /// <summary>
    /// 0=Queued, 1=InProgress, 
    /// 2=Completed, 3=Canceled, 4=FailedDuringExecution,
    /// 5=RejectedConcurrencyLock, 6=RejectedTresholdLock,
    /// 7=RejectedNoPermission, 8=RejectedPermanentlyUnavailable
    /// </summary>
    public InvocationStatus InvocationState { get; set; } = InvocationStatus.Queued;

    /// <summary>
    /// During execution (State 1) additional info regarding the current progress.
    /// After Failure (State 4) additional additional error information.
    /// </summary>
    public string StatusMessage { get; set; }

    /// <summary>
    /// Used for client-side Progressbar.
    /// Can also carry a running number of that record wich is currently processed or
    /// the percentage (if TotalSteps==100).
    /// After Failure (State 4), this value can be left lower than TotalSteps to indicate
    /// the moment of failure.
    /// </summary>
    public int CurrentStep { get; set; }

    /// <summary>
    /// Used for client-side Progressbar. Carries the target step / record number to be reached
    /// when the execution is completed.
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Indicates, if the current execution can be canceled at the current time
    /// (while queued and/or while in progress)
    /// </summary>
    public bool CancellationPossible { get; set; } = false;

    /// <summary>
    /// Indicates, if the current execution should be canceled.
    /// This can also be set, if a cancellation is currently not possible,
    /// but should be done on earliest possible moment.
    /// </summary>
    public bool CancellationRequested { get; set; } = false;

  }

}
