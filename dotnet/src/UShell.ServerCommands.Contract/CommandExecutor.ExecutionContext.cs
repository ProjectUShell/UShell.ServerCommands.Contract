using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace UShell.ServerCommands {

  public partial class CommandExecutor {

    // NESTED: ExecutionContext //

    public delegate CommandAvailabilityInfo AvailabilityEvaluationDelegate(CommandExecutor executor, string commandName);
   
    public delegate InvocationResult CommandInvokationDelegate(IExecutionContext execution);

    public interface IExecutionContext {

      CommandExecutor Executor { get; }

      string[] Arguments { get; }

      CancellationToken CancellationToken { get; }

      void SetCancellationPossible(bool isPossible);

      bool CancellationPossible{ get; }

      /// <summary> </summary>
      /// <param name="currentStep">
      /// Used for client-side Progressbar.
      /// Can also carry a running number of that record wich is currently processed or
      /// the percentage (if TotalSteps==100).
      /// After Failure, this value can be left lower than TotalSteps to indicate
      /// the moment of failure.
      /// </param>
      /// <param name="totalSteps">
      /// Used for client-side Progressbar. Carries the target step / record number to be reached
      /// when the execution is completed.
      /// </param>
      /// <param name="statusMessage">
      /// Additional info regarding the current progress or additional additional error information
      /// when failing.
      /// </param>
      /// <param name="cancellationPossible">
      /// Gives information, if the current execution can be canceled at the current time
      /// </param>
      void ReportProgress(
        int currentStep,
        int totalSteps,
        string statusMessage = null
      );

    }

    [DebuggerDisplay(nameof(ExecutionContext) + " ({CommandName} / {ExecutionId})")]
    internal class ExecutionContext : IExecutionContext {

      [DebuggerBrowsable(DebuggerBrowsableState.Never)]
      private CommandExecutor _Executor;

      [DebuggerBrowsable(DebuggerBrowsableState.Never)]
      private RegisteredCommand _Command;

      [DebuggerBrowsable(DebuggerBrowsableState.Never)]
      private ServerCommandExecutionState _State;

      [DebuggerBrowsable(DebuggerBrowsableState.Never)]
      private string[] _Arguments;

      [DebuggerBrowsable(DebuggerBrowsableState.Never)]
      private readonly CancellationTokenSource _CancellationTokenSrc = new CancellationTokenSource();

      [DebuggerBrowsable(DebuggerBrowsableState.Never)]
      private DateTime _LastAccess = DateTime.Now;

      [DebuggerBrowsable(DebuggerBrowsableState.Never)]
      private Task _ExecutionTask = null;

      public ExecutionContext(
        CommandExecutor executor, RegisteredCommand command, string[] arguments, ServerCommandExecutionState state
      ) {
        _Executor = executor;
        _Command = command;
        _Arguments = arguments;
        _State = state;
      }

      #region " Properties "

      public CommandExecutor Executor {
        get {
          return _Executor;
        }
      }

      public string CommandName {
        get {
          return _Command.CommandName;
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

        if(this.State.InvocationState <  InvocationStatus.Completed) {
          return false;
        }

        return DateTime.Now > _LastAccess.AddMinutes(2);
      }

      #endregion 

      public void Start() {

        this.UpdateLastAcccess();
        if(_ExecutionTask != null) {
          return;
        }
   
        _ExecutionTask = Task.Run(
          () => {
            try {
              this.State.InvocationState = InvocationStatus.InProgress;
              InvocationResult result = _Command.OnInvoke.Invoke(this);
              if(result == InvocationResult.Completed) {
                _State.InvocationState = InvocationStatus.Completed;
              }
              else if (result == InvocationResult.Canceled) {
                _State.InvocationState = InvocationStatus.Canceled;
              }
              else { //if (result == InvocationResult.Failed) {
                _State.InvocationState = InvocationStatus.FailedDuringExecution;
              }
            }
            catch (Exception ex) {
              _State.InvocationState = InvocationStatus.FailedDuringExecution;
              _State.CancellationPossible = false;
              _State.StatusMessage = ex.Message;
            }
            this.UpdateLastAcccess();
          }, _CancellationTokenSrc.Token
        );

      }

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

}
