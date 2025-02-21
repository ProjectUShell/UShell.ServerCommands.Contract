using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace UShell.ServerCommands {

  public partial class CommandExecutor : IServerCommandsExecutor {

    [DebuggerDisplay(nameof(RegisteredCommand) + " ({CommandName})")]
    public class RegisteredCommand {

      /// <summary>
      /// only Unique scoped to the current Endpoint
      /// </summary>
      public string CommandName { get; internal set; }

      [DebuggerBrowsable(DebuggerBrowsableState.Never)]
      string[] _ArgumentNames = new string[] { };

      public string[] ArgumentNames {
        get {
          return _ArgumentNames;
        }
        set {
          if (value == null) {
            _ArgumentNames = new string[] { };
          }
          else {
            _ArgumentNames = value;
          }
        }
      }

      [DebuggerBrowsable(DebuggerBrowsableState.Never)]
      string _Description = string.Empty;

      /// <summary>
      /// Informative text that describes what the command is doing
      /// </summary>
      public string Description {
        get {
          return _Description;
        }
        set {
          if (value == null) {
            _Description = string.Empty;
          }
          else {
            _Description = value;
          }
        }
      }

      public ServerCommandDescription ToServerCommandDescription() {
        return new ServerCommandDescription {
          CommandName = this.CommandName,
          ArgumentNames = this.ArgumentNames,
          Description = this.Description
        };
      }

      internal CommandInvokationDelegate OnInvoke { get; set; } = null;
      internal AvailabilityEvaluationDelegate OnEvaluateAvailability { get; set; } = null;

    }

  }

}
