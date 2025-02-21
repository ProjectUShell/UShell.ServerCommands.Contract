
namespace UShell.ServerCommands {

  public interface IServerCommandExecutor {

    /// <summary>
    /// Returns an listing of supported commands and their required arguments.
    /// </summary>
    /// <returns></returns>
    ServerCommandDescription[] GetCommandDescriptions();

    /// <summary>
    /// Returns true, if a new execution of the given can be startet.
    /// </summary>
    /// <param name="commandName"></param>
    /// <param name="reason">
    /// Some detail why:
    ///  0=ExecutionPossible, 5=ConcurrencyLock, 6=TresholdLock,
    ///  7=NoPermission, 8=PermanentlyUnavailable (NotImplemented/Disabled)
    /// </param>
    /// <returns></returns>
    bool CanStartExecution(string commandName, out CommandAvailabilityInfo reason);

    /// <summary>
    /// Enqueues a Server-Side command to be executed and returns the current state of execution,
    /// which can either be directly the final outcome or an information about the current progress.
    /// In the last case the 'GetExecutionState'-Method can be called to poll for updated progress information.
    /// </summary>
    /// <param name="commandName"></param>
    /// <param name="arguments">
    /// An Array of agrument values (highly individual per concrete implementation).
    /// This is only ment for minimal usecases like passing some recordIds to be processed -
    /// for more info see the corresponding 'CommandDescription'.
    /// </param>
    /// <param name="syncWaitMs">Can be used to wait for some ms to give a short timeslot
    /// to execute the command immediately while synchronously waiting. Be aware that,
    /// the execution will always be async and it never can be expected to get an valid outcome
    /// after that timespan. This is only ment to increase the chance for getting the outcome directly.
    /// To skip this, just pass 0.</param>
    /// <param name="executionState"></param>
    void StartExecution(
      string commandName, string[] arguments, int syncWaitMs,
      out ServerCommandExecutionState executionState
    );

    /// <summary>
    /// Returns the current progress information for a given 'executionId'
    /// </summary>
    /// <param name="executionId"></param>
    /// <param name="executionState"></param>
    void GetLatestExecutionState(
      string executionId, out ServerCommandExecutionState executionState
    );

    void RequestCancellation(
      string executionId
    );

  }

}
