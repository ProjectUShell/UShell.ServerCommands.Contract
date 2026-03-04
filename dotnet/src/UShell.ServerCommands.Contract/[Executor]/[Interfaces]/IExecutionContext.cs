using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace UShell.ServerCommands {

  public delegate CommandAvailabilityInfo AvailabilityEvaluationDelegate(
    CommandExecutor executor, string commandName
  );

  public delegate CommandExecutor.InvocationResult CommandInvokationDelegate(IExecutionContext execution);

  public interface IExecutionContext {

    //CommandExecutor Executor { get; }

    string[] Arguments { get; }

    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gives information, if the current execution can be canceled at the current time
    /// </summary>
    /// <param name="isPossible"></param>
    void SetCancellationPossible(bool isPossible);

    /// <summary>
    /// Returns information, if the current execution can be canceled at the current time
    /// </summary>
    bool CancellationPossible { get; }

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
    void ReportProgress(
      int currentStep,
      int totalSteps,
      string statusMessage = null
    );

  }

}
