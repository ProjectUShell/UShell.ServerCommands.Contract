using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using static UShell.ServerCommands.CommandExecutor;

namespace UShell.ServerCommands {

  /// <summary>
  /// Extends the basic ICommandRegistrar with overloads which are 
  /// exclusively available for ASP.NET Core hosting
  /// </summary>
  public interface IAspCommandRegistrar : ICommandRegistrar {

    /// <summary>
    /// An overload which exclusively available for ASP.NET Core hosting, because here we have
    /// the Microsoft DI framework available. The methods registers the commands of a given interface,
    /// without the need to provide an instance of the interface, because the DI framework will be used 
    /// to resolve the instance when needed...
    /// </summary>
    /// <typeparam name="TCommandInterface"></typeparam>
    /// <returns></returns>
    RegisteredCommand[] RegisterCommands<TCommandInterface>();

  }

}
