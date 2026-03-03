using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace UShell.ServerCommands {

  public partial class AspWorkerBasedCommandExecutor : CommandExecutor {

    private readonly IServiceProvider _ServiceProvider;

    public AspWorkerBasedCommandExecutor(IServiceProvider serviceProvider) {
      _ServiceProvider = serviceProvider;
    }



    xxx //DASH HIER ALS WORKER HOSTEN!!!!!



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


  }

}
