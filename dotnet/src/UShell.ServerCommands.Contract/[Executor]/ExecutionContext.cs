using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace UShell.ServerCommands {

  [DebuggerDisplay(nameof(ExecutionContext) + " ({CommandName} / {ExecutionId})")]
  internal class ExecutionContext : IExecutionContext {

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private ServerCommandExecutionState _State;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string[] _Arguments;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly CancellationTokenSource _CancellationTokenSrc = new CancellationTokenSource();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private DateTime _LastAccess = DateTime.Now;

    internal RegisteredCommand Command { get; private set; } = null;

    internal Task ExecutionTask {get; set;} = null;

    public ExecutionContext(
      RegisteredCommand command, string[] arguments,
      ServerCommandExecutionState state,
      CancellationToken engineLifetimeCancellationToken
    ) {

      this.Command = command;

      _Arguments = arguments;
      _State = state;
      _CancellationTokenSrc = CancellationTokenSource.CreateLinkedTokenSource(engineLifetimeCancellationToken);

    }

    #region " Properties "

    public string CommandName {
      get {
        return Command.CommandName;
      }
    }

    public ServerCommandExecutionState State {
      get {
        return _State;
      }
    }

    public string[] Arguments {
      get {
        return _Arguments;
      }
    }

    public string ExecutionId {
      get {
        return _State.ExecutionId;
      }
    }

    public bool CancellationPossible {
      get {
        return _State.CancellationPossible;
      }
    }

    public bool IsOrphaned() {

      if(this.State.InvocationState < InvocationStatus.Completed) {
        return false;
      }

      return DateTime.Now > _LastAccess.AddMinutes(2);
    }

    #endregion 

    public void RequestCancellation() {

      _CancellationTokenSrc.Cancel();
      _State.CancellationRequested = true;

      this.UpdateLastAcccess();
    }

    /// <summary>
    /// used to indicate, if the execution is orphaned
    /// </summary>
    internal void UpdateLastAcccess() {
      _LastAccess = DateTime.Now;
    }

    #region " Interface (only for access from inside of the invoked method) "

    void IExecutionContext.ReportProgress(
      int currentStep, int totalSteps, string statusMessage = null
    ) {

      this.UpdateLastAcccess();

      this.State.CurrentStep = currentStep;
      this.State.TotalSteps = totalSteps;
      this.State.StatusMessage = statusMessage;

    }

    void IExecutionContext.SetCancellationPossible(bool isPossible) {
      this.State.CancellationPossible = isPossible;
    }

    CancellationToken IExecutionContext.CancellationToken {
      get {
        return _CancellationTokenSrc.Token;
      }
    }

    #endregion

  }

}
