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
      this IServiceCollection services, Action<AspWorkerBasedCommandExecutor> configurationCallback
    ) {

      AddServerCommandExecutor(services, configurationCallback, null);

    }

    public static void AddServerCommandExecutor(
      this IServiceCollection services, Action<AspWorkerBasedCommandExecutor> configurationCallback,
      Action<DynamicUjmwControllerOptions> optionsConfigurator
    ) {

      services.AddSingleton<IServerCommandExecutor>((services) => {
        AspWorkerBasedCommandExecutor executor = new AspWorkerBasedCommandExecutor(services);
        configurationCallback.Invoke(executor);
        return executor;
      });

      services.AddDynamicUjmwControllers(
        (DynamicUjmwControllerRegistrar cfg) => {
          if(optionsConfigurator == null) {
            cfg.AddControllerFor<IServerCommandExecutor>();
          }
          else {
            cfg.AddControllerFor<IServerCommandExecutor>(optionsConfigurator);
          }
        }
      );

    }

  }
}
