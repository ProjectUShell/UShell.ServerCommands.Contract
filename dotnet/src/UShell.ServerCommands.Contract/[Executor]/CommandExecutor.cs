using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace UShell.ServerCommands {

  public partial class CommandExecutor : IServerCommandExecutor, ICommandRegistrar, IDisposable {

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private Dictionary<string, RegisteredCommand> _RegisteredCommandsPerName = new Dictionary<string, RegisteredCommand>();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private Dictionary<string, ExecutionContext> _RunningExecutionsPerId = new Dictionary<string, ExecutionContext>();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected CancellationTokenSource EngineLifetimeCancellationTokenSource { get; set; } = new CancellationTokenSource();

    public CommandExecutor() {
    }

    public CommandExecutor(CancellationTokenSource engineLifetimeCancellationTokenSource) {
      this.EngineLifetimeCancellationTokenSource = engineLifetimeCancellationTokenSource;
    }

    #region " ICommandRegistrar (multiple overloads for convenience) "

    #region " Action (0-3 Ags) "

    public RegisteredCommand RegisterCommand(
      string commandName,
      Action onInvoke,
      int limitOfConcurrentExecutions = -1
    ) {
      RegisteredCommand cmd = this.RegisterCommand(
        commandName, (IExecutionContext e) => {
          onInvoke.Invoke();
          return InvocationResult.Completed;
        }, limitOfConcurrentExecutions
      );
      cmd.ArgumentNames = new string[] { };
      return cmd;
    }

    public RegisteredCommand RegisterCommand(
      string commandName,
      Action<string> onInvoke,
      int limitOfConcurrentExecutions = -1
    ) {
      RegisteredCommand cmd = this.RegisterCommand(
        commandName, (IExecutionContext e) => {
          if(e.Arguments.Length < 1){
            e.ReportProgress(0, 0, "missing argument");
            return InvocationResult.Failed;
          }
          onInvoke.Invoke(e.Arguments[0]);
          return InvocationResult.Completed;
        }, limitOfConcurrentExecutions
      );
      var mp = onInvoke.Method.GetParameters();
      cmd.ArgumentNames = new string[] { mp[0].Name };
      return cmd;
    }

    public RegisteredCommand RegisterCommand(
      string commandName,
      Action<string, string> onInvoke,
      int limitOfConcurrentExecutions = -1
    ) {
      RegisteredCommand cmd = this.RegisterCommand(
        commandName, (IExecutionContext e) => {
          if (e.Arguments.Length < 2) {
            e.ReportProgress(0, 0, "missing argument");
            return InvocationResult.Failed;
          }
          onInvoke.Invoke(e.Arguments[0], e.Arguments[1]);
          return InvocationResult.Completed;
        }, limitOfConcurrentExecutions
      );
      var mp = onInvoke.Method.GetParameters();
      cmd.ArgumentNames = new string[] { mp[0].Name, mp[1].Name };
      return cmd;
    }
    public RegisteredCommand RegisterCommand(
      string commandName,
      Action<string, string, string> onInvoke,
      int limitOfConcurrentExecutions = -1
    ) {
      RegisteredCommand cmd = this.RegisterCommand(
        commandName, (IExecutionContext e) => {
          if (e.Arguments.Length < 3) {
            e.ReportProgress(0, 0, "missing argument");
            return InvocationResult.Failed;
          }
          onInvoke.Invoke(e.Arguments[0], e.Arguments[1], e.Arguments[2]);
          return InvocationResult.Completed;
        }, limitOfConcurrentExecutions
      );
      var mp = onInvoke.Method.GetParameters();
      cmd.ArgumentNames = new string[] { mp[0].Name, mp[1].Name, mp[2].Name };
      return cmd;
    }

    #endregion

    #region " Action (0-3 Ags + CancellationToken) "

    public RegisteredCommand RegisterCommand(
      string commandName,
      Action<CancellationToken> onInvoke,
      int limitOfConcurrentExecutions = -1
    ) {
      RegisteredCommand cmd = this.RegisterCommand(
        commandName, (IExecutionContext e) => {
          e.SetCancellationPossible(true);
          onInvoke.Invoke(e.CancellationToken);
          if (e.CancellationPossible && e.CancellationToken.IsCancellationRequested) {
            //yes - we cant be 100% sure that the method hasnt completed regularry, but
            //this is just a convenience overload for the only reason to reduce complexity
            return InvocationResult.Canceled;
          }
          return InvocationResult.Completed;
        }, limitOfConcurrentExecutions
      );
      cmd.ArgumentNames = new string[] { };
      return cmd;
    }

    public RegisteredCommand RegisterCommand(
      string commandName,
      Action<string, CancellationToken> onInvoke,
      int limitOfConcurrentExecutions = -1
    ) {
      RegisteredCommand cmd = this.RegisterCommand(
        commandName, (IExecutionContext e) => {
          if (e.Arguments.Length < 1) {
            e.ReportProgress(0, 0, "missing argument");
            return InvocationResult.Failed;
          }
          e.SetCancellationPossible(true);
          onInvoke.Invoke(e.Arguments[0], e.CancellationToken);
          if (e.CancellationPossible && e.CancellationToken.IsCancellationRequested) {
            //yes - we cant be 100% sure that the method hasnt completed regularry, but
            //this is just a convenience overload for the only reason to reduce complexity
            return InvocationResult.Canceled;
          }
          return InvocationResult.Completed;
        }, limitOfConcurrentExecutions
      );
      var mp = onInvoke.Method.GetParameters();
      cmd.ArgumentNames = new string[] { mp[0].Name };
      return cmd;
    }

    public RegisteredCommand RegisterCommand(
      string commandName,
      Action<string, string, CancellationToken> onInvoke,
      int limitOfConcurrentExecutions = -1
    ) {
      RegisteredCommand cmd = this.RegisterCommand(
        commandName, (IExecutionContext e) => {
          if (e.Arguments.Length < 2) {
            e.ReportProgress(0, 0, "missing argument");
            return InvocationResult.Failed;
          }
          e.SetCancellationPossible(true);
          onInvoke.Invoke(e.Arguments[0], e.Arguments[1], e.CancellationToken);
          if (e.CancellationPossible && e.CancellationToken.IsCancellationRequested) {
            //yes - we cant be 100% sure that the method hasnt completed regularry, but
            //this is just a convenience overload for the only reason to reduce complexity
            return InvocationResult.Canceled;
          }
          return InvocationResult.Completed;
        }, limitOfConcurrentExecutions
      );
      var mp = onInvoke.Method.GetParameters();
      cmd.ArgumentNames = new string[] { mp[0].Name, mp[1].Name };
      return cmd;
    }
    public RegisteredCommand RegisterCommand(
      string commandName,
      Action<string, string, string, CancellationToken> onInvoke,
      int limitOfConcurrentExecutions = -1
    ) {
      RegisteredCommand cmd = this.RegisterCommand(
        commandName, (IExecutionContext e) => {
          if (e.Arguments.Length < 3) {
            e.ReportProgress(0, 0, "missing argument");
            return InvocationResult.Failed;
          }
          e.SetCancellationPossible(true);
          onInvoke.Invoke(e.Arguments[0], e.Arguments[1], e.Arguments[2], e.CancellationToken);
          if (e.CancellationPossible && e.CancellationToken.IsCancellationRequested) {
            //yes - we cant be 100% sure that the method hasnt completed regularry, but
            //this is just a convenience overload for the only reason to reduce complexity
            return InvocationResult.Canceled;
          }
          return InvocationResult.Completed;
        }, limitOfConcurrentExecutions
      );
      var mp = onInvoke.Method.GetParameters();
      cmd.ArgumentNames = new string[] { mp[0].Name, mp[1].Name, mp[2].Name };
      return cmd;
    }

    #endregion

    public RegisteredCommand RegisterCommand(
      string commandName,
      CommandInvokationDelegate onInvoke,
      int limitOfConcurrentExecutions
    ) {
      if (limitOfConcurrentExecutions < 0) {
        return this.RegisterCommand(commandName, onInvoke);
      }
      else {
        return this.RegisterCommand(
          commandName, onInvoke,
          (executor, commandNameAgain) => {
            if (executor.CountConcurrentExecutions(commandName) < limitOfConcurrentExecutions) {
              return CommandAvailabilityInfo.ExecutionPossible;
            }
            else {
              return CommandAvailabilityInfo.ConcurrencyLock;
            }
          }
        );
      }
    }

    public RegisteredCommand RegisterCommand(
      string commandName,
      CommandInvokationDelegate onInvoke,
      AvailabilityEvaluationDelegate onEvaluateAvailability = null
    ) {
      lock (_RegisteredCommandsPerName) {

        if (_RegisteredCommandsPerName.ContainsKey(commandName)) {
          throw new Exception($"There is already a registered command named '{commandName}'.");
        }

        var newCommand = new RegisteredCommand();

        newCommand.CommandName = commandName;
        newCommand.OnInvoke = onInvoke;
        newCommand.OnEvaluateAvailability = onEvaluateAvailability;

        //just metadata for documentation
        newCommand.ArgumentNames = onInvoke.Method.GetParameters().Where(
          (p) => p.ParameterType != typeof(CancellationToken) && p.ParameterType != typeof(IExecutionContext)
        ).Select(
          p => p.Name
        ).ToArray();
        
        //TODO: newCommand.Description = LoaderOptimization FROM XML-COMMENTS!

        _RegisteredCommandsPerName.Add(commandName, newCommand);

        return newCommand;
      }
    }

    #region " Convenience to Register all Commands of an Interface "

    public RegisteredCommand[] RegisterCommands<TCommandInterface>(Func<TCommandInterface> implementationGetter) {

      Type commandInterfaceType = typeof(TCommandInterface);

      MethodInfo[] commandMethods = commandInterfaceType.GetMethods();

      List<RegisteredCommand> commands = new List<RegisteredCommand>();
      foreach (MethodInfo commandMethod in commandMethods) {

        List<Func<IExecutionContext, TCommandInterface,object>> convertedArgGetters = new List<Func<IExecutionContext, TCommandInterface, object>>();
     
        ParameterInfo[] parameters = commandMethod.GetParameters();
        int exposedArgumentsIndex = 0;
        for (int i = 0; i < parameters.Length; i++) {
          Type targetType = parameters[i].ParameterType;
          if (targetType == typeof(IExecutionContext)) {
            convertedArgGetters.Add((ctx,tgt)=> ctx);
          }
          else if (targetType == typeof(CancellationToken)) {
            convertedArgGetters.Add((ctx, tgt) => {
              ctx.SetCancellationPossible(true);
              return ctx.CancellationToken;
            });
          }
          else {
            if (targetType == typeof(string)) {
              convertedArgGetters.Add(
                (ctx, tgt) => ctx.Arguments[exposedArgumentsIndex]
              );
            }
            else if (targetType.IsEnum) {
              convertedArgGetters.Add(
                (ctx, tgt) => Enum.Parse(targetType, ctx.Arguments[exposedArgumentsIndex] ?? string.Empty)
              );
            }
            else if (targetType == typeof(Guid)) {
              convertedArgGetters.Add(
                (ctx, tgt) => Guid.Parse(ctx.Arguments[exposedArgumentsIndex] ?? Guid.Empty.ToString())
              );
            }
            else if (targetType.IsPrimitive) {
              convertedArgGetters.Add(
               (ctx, tgt) => Convert.ChangeType(ctx.Arguments[exposedArgumentsIndex] ?? string.Empty, targetType)
             );
            }
            else {
              throw new NotSupportedException(
                $"Unsupported parameter type '{targetType.FullName}' for parameter '{parameters[i].Name}' in command method '{commandMethod.Name}'. Only primitives are supported!"
              );
            }
            exposedArgumentsIndex++;
          }
        }

        RegisteredCommand command = this.RegisterCommand(
          $"{commandInterfaceType.Name}.{commandMethod.Name}",
          (IExecutionContext context) => {

            TCommandInterface impl = implementationGetter.Invoke();

            object[] args = convertedArgGetters.Select(
              (getter) => getter.Invoke(context, impl)
            ).ToArray();
 
            object result = commandMethod.Invoke(impl, args);
            if (commandMethod.ReturnType == typeof(void)) {
              return InvocationResult.Completed;
            }
            else {
              //TODO: in future, we will handle results here 
              return InvocationResult.Completed;
            }

          }
        );

        command.ArgumentNames = commandMethod.GetParameters().Where(
          (p) => p.ParameterType != typeof(CancellationToken) && p.ParameterType != typeof(IExecutionContext)
        ).Select(
          p => p.Name
        ).ToArray();

        commands.Add(command);
      }

      return commands.ToArray();
    }

    #endregion

    #endregion

    public RegisteredCommand[] RegisteredCommands {
      get {
        lock (_RegisteredCommandsPerName) {
          return _RegisteredCommandsPerName.Values.ToArray();
        }
      }
    }

    public RegisteredCommand GetCommand(string commandName) {
      lock (_RegisteredCommandsPerName) {

        if (!_RegisteredCommandsPerName.ContainsKey(commandName)) {
          throw new Exception($"There is no registered command named '{commandName}'.");
        }

        return _RegisteredCommandsPerName[commandName];
      }
    }

    public ServerCommandDescription[] GetCommandDescriptions() {
      lock (_RegisteredCommandsPerName) {
        return _RegisteredCommandsPerName.Values.Select((c) => c.ToServerCommandDescription()).ToArray();
      }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal ExecutionContext[] RunningExecutions {
      get {
        lock (_RunningExecutionsPerId) {
          return _RunningExecutionsPerId.Values.ToArray();
        }
      }
    }

    public RegisteredCommand GetExecution(string executionId) {
      lock (_RunningExecutionsPerId) {

        if (!_RunningExecutionsPerId.ContainsKey(executionId)) {
          return null;
        }

        return _RegisteredCommandsPerName[executionId];
      }
    }

    public bool CanStartExecution(string commandName, out CommandAvailabilityInfo reason) {
      return this.CanStartExecution(commandName, out reason, out RegisteredCommand dummy);
    }

    private bool CanStartExecution(string commandName, out CommandAvailabilityInfo reason, out RegisteredCommand exisitingCommand) {
      exisitingCommand = null;

      lock (_RegisteredCommandsPerName) {
        if (!_RegisteredCommandsPerName.ContainsKey(commandName)) {
          reason = CommandAvailabilityInfo.PermanentlyUnavailable;
          return false;
        }
        exisitingCommand = _RegisteredCommandsPerName[commandName];
      }

      if(exisitingCommand.OnEvaluateAvailability == null) {
        reason = CommandAvailabilityInfo.ExecutionPossible;
        return true;
      }

      CommandAvailabilityInfo availability = exisitingCommand.OnEvaluateAvailability.Invoke(this, commandName);

      if(availability == CommandAvailabilityInfo.ExecutionPossible) {
        reason = CommandAvailabilityInfo.ExecutionPossible;
        return true;
      }
      else if (availability == CommandAvailabilityInfo.ConcurrencyLock) {
        reason = CommandAvailabilityInfo.ConcurrencyLock;
        return false;
      }
      else if (availability == CommandAvailabilityInfo.TresholdLock) {
        reason = CommandAvailabilityInfo.TresholdLock;
        return false;
      }
      else if (availability == CommandAvailabilityInfo.NoPermission) {
        reason = CommandAvailabilityInfo.NoPermission;
        return false;
      }
      else { //(availability == CommandAvailabilityInfo.PermanentlyUnavailable) {
        reason = CommandAvailabilityInfo.PermanentlyUnavailable;
        return false;
      }

    }

    public void CleanupOrphanedExecutions() {
      lock (_RunningExecutionsPerId) { 
        foreach (ExecutionContext context in _RunningExecutionsPerId.Values.ToArray()) {
          if (context.IsOrphaned()) {
            _RunningExecutionsPerId.Remove(context.ExecutionId);
          }
        }
      }
    }

    public int CountConcurrentExecutions() {
      lock (_RunningExecutionsPerId) {
        return _RunningExecutionsPerId.Values.Where(
          (c)=> c.State.InvocationState <= InvocationStatus.InProgress
        ).Count();
      }
    }

    public int CountConcurrentExecutions(string commandName) {
      lock (_RunningExecutionsPerId) {
        return _RunningExecutionsPerId.Values.Where(
          (c) => c.CommandName == commandName && c.State.InvocationState <= InvocationStatus.InProgress
        ).Count();
      }
    }

    public void GetLatestExecutionState(string executionId, out ServerCommandExecutionState executionState) {
      lock (_RunningExecutionsPerId) {
        if (_RunningExecutionsPerId.TryGetValue(executionId, out ExecutionContext context)) {
          executionState = context.State;
          context.UpdateLastAcccess();
        }
        else {
          executionState = null;
        }
      }
    }

    public void RequestCancellation(string executionId) {
      lock (_RunningExecutionsPerId) {
        if(_RunningExecutionsPerId.TryGetValue(executionId, out ExecutionContext context)) {
          context.RequestCancellation();
        }
      }
    }

    public virtual void Dispose() {
      this.EngineLifetimeCancellationTokenSource.Cancel();
    }

  }

}
