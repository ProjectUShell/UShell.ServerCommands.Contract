using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Web.UJMW;

namespace UShell.ServerCommands {

  public static class AspServerCommandsSetupExtensions {

    public static void AddServerCommandExecutor(
      this IServiceCollection services, Action<IAspCommandRegistrar> configurationCallback
    ) {

      AddServerCommandExecutor(services, configurationCallback, null);

    }

    public static void AddServerCommandExecutor(
      this IServiceCollection services, Action<IAspCommandRegistrar> configurationCallback,
      Action<DynamicUjmwControllerOptions> optionsConfigurator
    ) {

      //register the executor as singleton (into the asp DI framework)
      services.AddSingleton<IServerCommandExecutor>((services) => {
        AspWorkerBasedCommandExecutor executor = new AspWorkerBasedCommandExecutor(services);
        configurationCallback.Invoke(executor);
        return executor;
      });

      services.AddHostedService<AspWorkerBasedCommandExecutor.HostedServiceProxyForCommandExecutor>();

      //register the executor itself as ujmw-endpoint...
      services.AddDynamicUjmwControllers(
        (DynamicUjmwControllerRegistrar ujmw) => {

          if(optionsConfigurator == null) {
            ujmw.AddControllerFor<IServerCommandExecutor>((opt) => {
              opt.ControllerRoute = "commands";
            });
          }
          else {
            ujmw.AddControllerFor<IServerCommandExecutor>((opt) => {
              opt.ControllerRoute = "commands";
              optionsConfigurator.Invoke(opt); //customizing, after setting the default
            }); 
          }

        }
      );

    }

  }
}
