using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace UShell.ServerCommands {

  public partial class CommandExecutor {

    public int NumberOfRunningTasks { 
      get {
        lock (_RunningExecutionsPerId) {
          return _RunningExecutionsPerId.Values.Where(
            (c) => c.ExecutionTask.Status == TaskStatus.Running 
           ).Count();
        }
      }
    }

    private bool CommandExists(string commandName) {
      if(string.IsNullOrWhiteSpace(commandName)) {
        return false;
      }
      lock (_RegisteredCommandsPerName) {
        return _RegisteredCommandsPerName.ContainsKey(commandName);
      }
    }

    public void StartExecution(string commandName, string[] arguments, int syncWaitMs, out ServerCommandExecutionState executionState) {
      
      if (this.EngineLifetimeCancellationTokenSource.Token.IsCancellationRequested) {
        throw new InvalidOperationException("Engine not running!");
      }

      if (!CommandExists(commandName)) {
        throw new InvalidOperationException($"unknown command '{commandName}'.");
      }

      DateTime holdUntil = DateTime.Now.AddMilliseconds(syncWaitMs);
      RegisteredCommand command;

      executionState = new ServerCommandExecutionState();
      executionState.ExecutionId = Guid.NewGuid().ToString().ToLower().Replace("-", "");
      executionState.CommandName = commandName;

      this.CleanupOrphanedExecutions();

      if (!this.CanStartExecution(commandName, out CommandAvailabilityInfo reason, out command)) {

        if (reason == CommandAvailabilityInfo.ConcurrencyLock) {
          executionState.InvocationState = InvocationStatus.RejectedConcurrencyLock;
        }
        else if (reason == CommandAvailabilityInfo.TresholdLock) {
          executionState.InvocationState = InvocationStatus.RejectedTresholdLock;
        }
        else if (reason == CommandAvailabilityInfo.NoPermission) {
          executionState.InvocationState = InvocationStatus.RejectedNoPermission;
        }
        else if (reason == CommandAvailabilityInfo.PermanentlyUnavailable) {
          executionState.InvocationState = InvocationStatus.RejectedPermanentlyUnavailable;
        }

        return;
      }

      if (arguments == null) {
        arguments = new string[0];
      }

      ExecutionContext context = new ExecutionContext(
        command, arguments, executionState, this.EngineLifetimeCancellationTokenSource.Token
      );

      lock (_RunningExecutionsPerId) {
        _RunningExecutionsPerId.Add(context.ExecutionId, context);
      }

      //context.Start();

      context.UpdateLastAcccess();
      if (context.ExecutionTask != null) {
        return;
      }

      context.ExecutionTask = this.InvokeAsTask(
        () => {
          ServerCommandExecutionState state = context.State;
          try {
            state.InvocationState = InvocationStatus.InProgress;
            InvocationResult result = context.Command.OnInvoke.Invoke(context);

            if (result == InvocationResult.Completed) {
              state.InvocationState = InvocationStatus.Completed;
            }
            else if (result == InvocationResult.Canceled) {
              state.InvocationState = InvocationStatus.Canceled;
            }
            else { //if (result == InvocationResult.Failed) {
              state.InvocationState = InvocationStatus.FailedDuringExecution;
            }

          }
          catch (Exception ex) {
            state.InvocationState = InvocationStatus.FailedDuringExecution;
            state.CancellationPossible = false;
            state.StatusMessage = ex.Message;
          }
          context.UpdateLastAcccess();
        }, context
      );

      while (DateTime.Now < holdUntil) {
        Thread.Sleep(100);
        if (executionState.InvocationState > InvocationStatus.InProgress) {
          break;
        }
      }

    }

    /// <summary>
    /// Customizing-Hook
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="context"></param>
    protected virtual Task InvokeAsTask(
      Action callback, IExecutionContext context
    ) {

      //weve decided to let the internal code handle cancellation on a clean way instead of using the
      //Task cancellation mechanism, because this one would be more of a "hard" cancellation,
      //which is not always desired (e.g. when we want to let the method finish its work,
      //but just want to stop waiting for it or stop reporting progress updates)

      return Task.Run(callback);
      //NOT: Task.Run(callback, context.CancellationToken);
    }

  }

}
