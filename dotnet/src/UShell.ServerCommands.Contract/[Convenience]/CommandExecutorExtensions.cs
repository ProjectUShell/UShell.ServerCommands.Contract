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
      Action<ServerCommandExecutionState> onNotCompletedOutcome = null
    ) {

      ServerCommandExecutionState finalState = executor.TryExecuteAndPoll(
        commandName, arguments, pollingIntervalSeconds, cancellationToken
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
      CancellationToken cancellationToken = default
    ) {

      ServerCommandExecutionState state = new ServerCommandExecutionState() { CommandName = commandName };

      executor.StartExecution(
        commandName, arguments, pollingIntervalSeconds,
        out state
      );

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

        if (cancellationToken.IsCancellationRequested && state.CancellationPossible && !cancallationRequestRedirected) {
          try {
            executor.RequestCancellation(state.ExecutionId);
            cancallationRequestRedirected = true;
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
