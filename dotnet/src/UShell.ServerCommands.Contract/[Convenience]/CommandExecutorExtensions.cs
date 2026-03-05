using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace UShell.ServerCommands {

  public static class CommandExecutorExtensions {

    public static ServerCommandExecutionState ExecuteAndPoll(
      this IServerCommandExecutor executor,
      string commandName, string[] arguments = null, int pollingIntervalSeconds = 10,
      CancellationToken cancellationToken = default,
      Action<ServerCommandExecutionState> onNotCompletedOutcome = null,
      Action<int,int> onProgressUpdate = null,
      Action<string> onStatusMessageUpdate = null
    ) {

      ServerCommandExecutionState finalState = executor.TryExecuteAndPoll(
        commandName, arguments, pollingIntervalSeconds, cancellationToken,
        onProgressUpdate, onStatusMessageUpdate
      );
   
      if (finalState.InvocationState != InvocationStatus.Completed) {
        if(onNotCompletedOutcome != null) {
          onNotCompletedOutcome(finalState);
        }
        else if(finalState.InvocationState == InvocationStatus.Canceled) {
          if (cancellationToken.IsCancellationRequested) {
            // Self - initiated cancellation, expected outcome
          }
          else {
            throw new OperationCanceledException(
              $"Execution of command '{commandName}' was canceled: {finalState?.StatusMessage}"
            );
          }
        }
        else {
          throw new Exception(
            $"Execution of command '{commandName}' did not complete successfully (final state: {finalState?.InvocationState}): {finalState?.StatusMessage}"
          );
        }
      }

      return finalState;
    }

    private const string NoResponseStatusMessage = "HANGING~";

    public static ServerCommandExecutionState TryExecuteAndPoll(
      this IServerCommandExecutor executor,
      string commandName, string[] arguments = null, int pollingIntervalSeconds = 10,
      CancellationToken cancellationToken = default,
      Action<int, int> onProgressUpdate = null,
      Action<string> onStatusMessageUpdate = null
    ) {

      ServerCommandExecutionState state = new ServerCommandExecutionState() { CommandName = commandName };

      executor.StartExecution(
        commandName, arguments, pollingIntervalSeconds,
        out state
      );

      string lastStatusMessage = state?.StatusMessage;
      onStatusMessageUpdate?.Invoke(lastStatusMessage);

      int currentStep = state?.CurrentStep ?? 0;
      int totalSteps = state?.TotalSteps ?? 0;
      onProgressUpdate?.Invoke(currentStep, totalSteps);

      bool cancallationRequestRedirected = false;
      while (
        !cancellationToken.IsCancellationRequested && (
          state.InvocationState == InvocationStatus.Queued ||
          state.InvocationState == InvocationStatus.InProgress
        )
      ){

        Thread.Sleep(pollingIntervalSeconds * 1000);

        try {

          executor.GetLatestExecutionState(
            state.ExecutionId, out ServerCommandExecutionState newState
          );

          if(newState != null) {
            state = newState;
          }

        }
        catch {
          if (string.IsNullOrWhiteSpace(state.StatusMessage)) {
            state.StatusMessage = NoResponseStatusMessage;
          }
          else if(!state.StatusMessage.StartsWith(NoResponseStatusMessage) ) {
            state.StatusMessage = NoResponseStatusMessage + state.StatusMessage;
          }

        }

        if (state.StatusMessage != lastStatusMessage) {
          lastStatusMessage = state.StatusMessage;
          onStatusMessageUpdate?.Invoke(lastStatusMessage);
        }

        if( state.CurrentStep != currentStep || state.TotalSteps != totalSteps ) {
          currentStep = state.CurrentStep;
          totalSteps = state.TotalSteps;
          onProgressUpdate?.Invoke(currentStep, totalSteps);
        }

        if (cancellationToken.IsCancellationRequested && state.CancellationPossible && !cancallationRequestRedirected) {
          try {
            executor.RequestCancellation(state.ExecutionId);
            cancallationRequestRedirected = true;
            lastStatusMessage = "Cancellation requested...";
            onStatusMessageUpdate?.Invoke(lastStatusMessage);
          }
          catch {
          }
        }

      }

      return state;
    }

    public static async Task<ServerCommandExecutionState> TryExecuteAndPollAsync(
      this IServerCommandExecutor executor, 
      string commandName, string[] arguments = null, int pollingIntervalSeconds = 10
    ) {

      return await Task<ServerCommandExecutionState>.Run(
        () => executor.TryExecuteAndPoll(commandName, arguments, pollingIntervalSeconds)
      );

    }

  }

}
