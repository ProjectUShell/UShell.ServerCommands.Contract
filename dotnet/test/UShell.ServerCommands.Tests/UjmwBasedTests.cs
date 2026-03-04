using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Web.UJMW;

namespace UShell.ServerCommands {

  [TestClass()]
  public class UjmwBasedTests {

    [TestMethod()]
    public void ExecuterUjmwTest1() {

      DemoService service = new DemoService();

      CommandExecutor executor = new CommandExecutor();
      executor.Configure((registrar) => {
        registrar.RegisterCommands<IDemoCommands>(()=> service);
      });

      //IServerCommandExecutor executor = DynamicClientFactory.CreateInstance<IServerCommandExecutor>(
      // "http://localhost:55202/DemoCommands", "dummy-auth-header"
      //);

      //ServerCommandExecutionState finalState = executor.ExecuteAndPoll(
      // $"{nameof(IDemoCommands)}.{nameof(IDemoCommands.ProcessAndCountManyManyRecords)}",
      // pollingIntervalSeconds: 1
      //);

      //Assert.AreEqual(InvocationStatus.Completed, finalState.InvocationState);

      ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
      // PoC - Haarsträubendes experiment, bei dem der abstrakte executor wiederum hinter ujmw "versteckt" wird... //

      IAbstractCallInvoker ujmwProxy = new ServerCommandUjmwCallInvoker(executor, 1 , nameof(IDemoCommands) + ".");
      IDemoCommands demoServiceCommands = DynamicClientFactory.CreateInstance<IDemoCommands>(ujmwProxy);

      demoServiceCommands.ProcessAndCountManyManyRecords(CancellationToken.None);

    }

  }

  //Just a simple experiment to use the UJMW-Client-Proxy to simulate a 
  //100% agnostic call invoker, based on the ASYNCHRONOUS polling mechanism of the server command executor.
  public class ServerCommandUjmwCallInvoker : IAbstractCallInvoker {

    private IServerCommandExecutor _Executor;
    private int _PollingIntervalSeconds;
    private string _CommandPrefix;

    public ServerCommandUjmwCallInvoker(IServerCommandExecutor executor, int pollingIntervalSeconds = 2, string commandPrefix = "") {
     
      _Executor = executor;
      _PollingIntervalSeconds = pollingIntervalSeconds;
      _CommandPrefix = commandPrefix;

    }

    public object InvokeCall(string methodName, object[] arguments, string[] argumentNames, string methodSignatureString) {

      CancellationToken cancellationTokenFromArgs = arguments?.OfType<CancellationToken>().FirstOrDefault() ?? CancellationToken.None;

      string[] argumentValuesAsStrings = arguments?.Where(
        arg => !(arg is CancellationToken)
      ).Select(
        (arg) => JsonConvert.SerializeObject(arg)
      ).ToArray();

      ServerCommandExecutionState finalState = _Executor.TryExecuteAndPoll(
        $"{_CommandPrefix}{methodName}", argumentValuesAsStrings,
        _PollingIntervalSeconds, cancellationTokenFromArgs
      );

      if (finalState.InvocationState != InvocationStatus.Completed) {
        if (finalState.InvocationState == InvocationStatus.Canceled) {
          if (cancellationTokenFromArgs.IsCancellationRequested) {
            // Self - initiated cancellation, expected outcome
          }
          else {
            throw new OperationCanceledException(
              $"Execution of command '{methodName}' was canceled: {finalState?.StatusMessage}"
            );
          }
        }
        else {
          throw new Exception(
            $"Execution of command '{methodName}' did not complete successfully (final state: {finalState?.InvocationState}): {finalState?.StatusMessage}"
          );
        }
      }

      //HACK: missbraucht den message-kanal für ein result...
      // -> muss überarbeitet werden wenn wir hier eine entscheidung haben ob es einen result-kanal geben sooll
      if(!string.IsNullOrWhiteSpace(finalState.StatusMessage)) {
        try {
          object deserializedResult = JsonConvert.DeserializeObject(finalState.StatusMessage);
          return deserializedResult;
        }
        catch (JsonException) {
        }
      }

      return null;
    }

  }

}
