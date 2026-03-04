using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UShell.ServerCommands {

  public partial class AspWorkerBasedCommandExecutor : CommandExecutor, IAspCommandRegistrar, IHostedService {

    public AspWorkerBasedCommandExecutor(IServiceProvider serviceProvider) {
      _ServiceProvider = serviceProvider;

      //interim state until we have received our 'StartAsync'-call from the hosting environment
      this.EngineLifetimeCancellationTokenSource.Cancel();
      this.EnvironmentHardShutdownCancellationTokenSource.Cancel();

    }

    #region " exclusive Feature 1: let DI automatically resove the impl.-instance to use...  "

    private readonly IServiceProvider _ServiceProvider;

    private TService GetInstanceFromServiceCollection<TService>() {
      return (TService) _ServiceProvider.GetRequiredService(typeof(TService));
    }

    /// <summary>
    /// An overload which exclusively available for ASP.NET Core hosting, because here we have
    /// the Microsoft DI framework available. The methods registers the commands of a given interface,
    /// without the need to provide an instance of the interface, because the DI framework will be used 
    /// to resolve the instance when needed..
    /// </summary>
    /// <typeparam name="TCommandInterface"></typeparam>
    /// <returns></returns>
    public RegisteredCommand[] RegisterCommands<TCommandInterface>() {

      RegisteredCommand[] commands = base.RegisterCommands<TCommandInterface>(
        () => this.GetInstanceFromServiceCollection<TCommandInterface>()
      );

      return commands;
    }

    #endregion

    #region " exclusive Feature 2: be a 'IHostedService' to get our lifetime managed by the asp hosting environment  "

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected CancellationTokenSource EnvironmentHardShutdownCancellationTokenSource { get; set; } = new CancellationTokenSource();
 
    /// <summary>
    /// Customizing-Hook
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="context"></param>
    protected override Task InvokeAsTask(
      Action callback, IExecutionContext context
    ) {
      return Task.Run(callback, this.EnvironmentHardShutdownCancellationTokenSource.Token);
    }


    private Task _DelayedHardShutdownTask = null;

    // Wird vom Host beim Start aufgerufen
    public Task StartAsync(CancellationToken cancellationToken) {

      //preserve the official cancellation token comming from the hosting environment
      this.EngineLifetimeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      this.EnvironmentHardShutdownCancellationTokenSource = new CancellationTokenSource();

      if (_DelayedHardShutdownTask != null) {
        return Task.CompletedTask;
      }

      _DelayedHardShutdownTask = Task.Run(() => {
        while (!this.EngineLifetimeCancellationTokenSource.IsCancellationRequested) {
          Thread.Sleep(1000);
        }
        Thread.Sleep(10000);
        this.EnvironmentHardShutdownCancellationTokenSource.Cancel();
        _DelayedHardShutdownTask = null;
      });

      //only the "Start"-Operation has completed ;-)
      return Task.CompletedTask;
    }

    // Wird vom Host beim Shutdown aufgerufen
    public async Task StopAsync(CancellationToken cancellationToken) {

      this.EngineLifetimeCancellationTokenSource.Cancel();

      await Task.Run(
        () => {
          //just to report, if 'StopAsync' had come to a clean end, we try to wait
          //until no one is still at work...
          while (base.NumberOfRunningTasks > 0 && !cancellationToken.IsCancellationRequested) {
            Thread.Sleep(300);
          }
        }, cancellationToken
      );

    }

    #endregion 

  }

}
