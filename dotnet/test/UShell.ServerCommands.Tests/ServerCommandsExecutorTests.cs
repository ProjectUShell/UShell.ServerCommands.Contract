using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace UShell.ServerCommands {

  [TestClass()]
  public class ServerCommandsExecutorTests {

    private CommandExecutor _Executor = null;
    private const string _MockExceptionMessage = "It smells";
    private const string _ExTriggeringRecordId = "OldEgg";

    public void EnsureIsInitialized() {

      if(_Executor != null) {
        return;
      }

      _Executor = new CommandExecutor();

      var cmd = _Executor.RegisterCommand(
        "EvaulateRecord",
        (string recordId, string sleepTime) => {
          if (recordId == _ExTriggeringRecordId) {
            throw new Exception(_MockExceptionMessage);
          }
          int st = int.Parse(sleepTime);
          Thread.Sleep(st);
        },
        2 //<< Limit of concurrent executions
      );

      Assert.IsNotNull(cmd);
      Assert.AreEqual(2, cmd.ArgumentNames.Length);
      Assert.AreEqual("recordId", cmd.ArgumentNames[0]);

      var cmd2 = _Executor.RegisterCommand(
        "CancelableMethod",
        (string runningDuration, CancellationToken ct) => {
          DateTime runUntil = DateTime.Now.AddMilliseconds(int.Parse(runningDuration));
          while (DateTime.Now < runUntil) {
            if (ct.IsCancellationRequested) {
              break;
            }
            Thread.Sleep(10);
          }
        }
      );
    }

    [TestMethod()]
    public void ExecuterTest_MissingArgument() {
      this.EnsureIsInitialized();
      ServerCommandExecutionState state;

      //missing argument -> 1 instead of 2
      _Executor.StartExecution(
        "EvaulateRecord", new string[] { "ABC" }, 100, out state
      );

      Assert.AreEqual(InvocationStatus.FailedDuringExecution, state.InvocationState);
      Assert.AreEqual("missing argument", state.StatusMessage);

    }

    [TestMethod()]
    public void ExecuterTest_SuccessDuringSyncWait() {
      this.EnsureIsInitialized();
      ServerCommandExecutionState state;

      DateTime startTime = DateTime.Now;

      //success within 50ms
      _Executor.StartExecution(
        "EvaulateRecord", new string[] { "ABC", "80" }, 500, out state
      );

      Assert.AreEqual(InvocationStatus.Completed, state.InvocationState);

      double durationMs = DateTime.Now.Subtract(startTime).TotalMilliseconds;
      //lets ensue, that completely synchronous operations will return
      //immdiately (here after 80ms + usually ~30-40ms overhead)
      //without waiting the full gracetime (here 500ms)
      Assert.IsTrue(durationMs < 300);

    }

    [TestMethod()]
    public void ExecuterTest_SuccessAfterSyncWait() {
      this.EnsureIsInitialized();
      ServerCommandExecutionState state;

      //success, but not finished within 50ms
      _Executor.StartExecution(
        "EvaulateRecord", new string[] { "ABC", "190" }, 100, out state
      );

      _Executor.GetLatestExecutionState(state.ExecutionId, out state);
      Assert.AreEqual(InvocationStatus.InProgress, state.InvocationState);

      Thread.Sleep(100);

      _Executor.GetLatestExecutionState(state.ExecutionId, out state);
      Assert.AreEqual(InvocationStatus.Completed, state.InvocationState);

    }

    [TestMethod()]
    public void ExecuterTest_ExceptionCatchedByFx() {
      this.EnsureIsInitialized();
      ServerCommandExecutionState state;

      _Executor.StartExecution( //        vvvvvv (will trigger exception)
        "EvaulateRecord", new string[] { _ExTriggeringRecordId, "0" }, 100, out state
      );

      Assert.AreEqual(InvocationStatus.FailedDuringExecution, state.InvocationState);
      Assert.AreEqual(_MockExceptionMessage, state.StatusMessage);

    }

    [TestMethod()]
    public void ExecuterTest_ConcurrencyLimit() {
      this.EnsureIsInitialized();
      ServerCommandExecutionState state1;
      ServerCommandExecutionState state2;
      ServerCommandExecutionState state3;

      _Executor.StartExecution(
        "EvaulateRecord", new string[] { "A", "600" }, 0, out state1
      );

      Thread.Sleep(100);

      _Executor.StartExecution( 
        "EvaulateRecord", new string[] { "B", "600" }, 0, out state2
      );

      Thread.Sleep(100);

      //this one is to much - it should be rejected because our limit is 2!
      _Executor.StartExecution(
        "EvaulateRecord", new string[] { "C", "600" }, 0, out state3
      );

      Assert.AreEqual(InvocationStatus.InProgress, state1.InvocationState);
      Assert.AreEqual(InvocationStatus.InProgress, state2.InvocationState);
      Assert.AreEqual(InvocationStatus.RejectedConcurrencyLock, state3.InvocationState);

      Thread.Sleep(500);

      _Executor.GetLatestExecutionState(state1.ExecutionId, out state1);
      _Executor.GetLatestExecutionState(state2.ExecutionId, out state2);
      _Executor.GetLatestExecutionState(state3.ExecutionId, out state3);

      Assert.AreEqual(InvocationStatus.Completed, state1.InvocationState);
      Assert.AreEqual(InvocationStatus.Completed, state2.InvocationState);
      Assert.IsNull(state3);

    }

    [TestMethod()]
    public void ExecuterTest_Cancellation() {
      this.EnsureIsInitialized();
      ServerCommandExecutionState state;

      _Executor.StartExecution( 
        "CancelableMethod", new string[] { "500" }, 0, out state
      );

      Thread.Sleep(100);

      _Executor.GetLatestExecutionState(state.ExecutionId, out state);
      Assert.AreEqual(InvocationStatus.InProgress, state.InvocationState);

      _Executor.RequestCancellation(state.ExecutionId);

      Thread.Sleep(100);

      _Executor.GetLatestExecutionState(state.ExecutionId, out state);
      Assert.AreEqual(InvocationStatus.Canceled, state.InvocationState);

    }

  }

}
