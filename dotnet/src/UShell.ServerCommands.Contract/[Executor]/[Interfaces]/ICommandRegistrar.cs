using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using static UShell.ServerCommands.CommandExecutor;

namespace UShell.ServerCommands {

  public interface ICommandRegistrar {

    #region " Action (0-3 Ags) "

    RegisteredCommand RegisterCommand(
      string commandName,
      Action onInvoke,
      int limitOfConcurrentExecutions = -1
    );

    RegisteredCommand RegisterCommand(
      string commandName,
      Action<string> onInvoke,
      int limitOfConcurrentExecutions = -1
    );

    RegisteredCommand RegisterCommand(
      string commandName,
      Action<string, string> onInvoke,
      int limitOfConcurrentExecutions = -1
    );

    RegisteredCommand RegisterCommand(
      string commandName,
      Action<string, string, string> onInvoke,
      int limitOfConcurrentExecutions = -1
    );

    #endregion

    #region " Action (0-3 Ags + CancellationToken) "

    RegisteredCommand RegisterCommand(
      string commandName,
      Action<CancellationToken> onInvoke,
      int limitOfConcurrentExecutions = -1
    );

    RegisteredCommand RegisterCommand(
      string commandName,
      Action<string, CancellationToken> onInvoke,
      int limitOfConcurrentExecutions = -1
    );

    RegisteredCommand RegisterCommand(
      string commandName,
      Action<string, string, CancellationToken> onInvoke,
      int limitOfConcurrentExecutions = -1
    );

    RegisteredCommand RegisterCommand(
      string commandName,
      Action<string, string, string, CancellationToken> onInvoke,
      int limitOfConcurrentExecutions = -1
    );

    #endregion

    RegisteredCommand RegisterCommand(
      string commandName,
      CommandInvokationDelegate onInvoke,
      int limitOfConcurrentExecutions
    );

    RegisteredCommand RegisterCommand(
      string commandName,
      CommandInvokationDelegate onInvoke,
      AvailabilityEvaluationDelegate onEvaluateAvailability = null
    );

    #region " Convenience to Register all Commands of an Interface "

    RegisteredCommand[] RegisterCommands<TCommandInterface>(
      Func<TCommandInterface> implementationGetter
    );

    #endregion

  }

}
