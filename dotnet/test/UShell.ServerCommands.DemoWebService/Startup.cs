using DistributedDataFlow;
using Logging.SmartStandards;
using Logging.SmartStandards.AspSupport;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Security.AccessTokenHandling;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.UJMW;
using UShell.ServerCommands;

namespace Demo {

  public class Startup {

    public Startup(IConfiguration configuration) {
      _Configuration = configuration;
    }

    private static IConfiguration _Configuration = null;
    public static IConfiguration Configuration { get { return _Configuration; } }

    const string _ApiTitle = "ServerCommands Demo";
    Version _ApiVersion = null;

    public void ConfigureServices(IServiceCollection services) {

      services.AddLogging();
      services.AddSmartStandardsLogging(_Configuration);

      _ApiVersion = typeof(IDemoCommands).Assembly.GetName().Version;

      string outDir = AppDomain.CurrentDomain.BaseDirectory;

      services.AddControllers();

      UjmwHostConfiguration.EnableApiGroupNameFallback = true;

      UjmwHostConfiguration.AuthHeaderEvaluator = (
        (string rawAuthHeader, Type contractType, MethodInfo targetContractMethod, string callingMachine, ref int httpReturnCode, ref string failedReason) => {
          //in this demo - any auth header is ok - but there must be one ;-)
          if (string.IsNullOrWhiteSpace(rawAuthHeader)) {
            httpReturnCode = 403;
            failedReason = "This demo requires at least ANY string as authheader!";
            return false;
          }
          return true;
        }
      );

      //////////////////////////////////////////////////////////////////////////////////////////
      // USHELL-SERVER-COMMANDS:
      //////////////////////////////////////////////////////////////////////////////////////////
      
      //register the service itself to the DI framework
      services.AddSingleton<IDemoCommands>((services) => new DemoService());

      //register the server command executor
      //(this will automatically create the required controller and endpoints )
      services.AddServerCommandExecutor(
        (registrar) => {

          //register one or more services which providing methods that should be exposed as "Commands":

          registrar.RegisterCommands<IDemoCommands>(); // <- the instance will be resolved via DI (as registeres above)

        }
      );

      //////////////////////////////////////////////////////////////////////////////////////////
      
      services.AddUjmwStandardSwaggerGen();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(
      IApplicationBuilder app, IWebHostEnvironment env,
      ILoggerFactory loggerfactory, IHostApplicationLifetime lifetimeEvents
    ) {

      string logFileFullName = _Configuration.GetValue<string>("LogFileName");
      string logDir = Path.GetFullPath(Path.GetDirectoryName(logFileFullName));
      Directory.CreateDirectory(logDir);
      loggerfactory.AddFile(logFileFullName);

      //required for the www-root
      app.UseStaticFiles();

      app.UseAmbientFieldAdapterMiddleware();

      if (!_Configuration.GetValue<bool>("ProdMode")) {
        app.UseDeveloperExceptionPage();
      }

      string baseUrl = _Configuration.GetValue<string>("BaseUrl");
 
      app.UseHttpsRedirection();

      app.UseRouting();

      //CORS: muss zwischen 'UseRouting' und 'UseEndpoints' liegen!
      app.UseCors(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader() );

      app.UseAuthentication(); //<< WINDOWS-AUTH
      app.UseAuthorization();

      app.UseEndpoints(endpoints => {
        endpoints.MapControllers();
      });

      //MUST BE AFTER 'UseEndpoints'/'MapControllers'
      app.UseUjmwStandardSwagger(_Configuration);

      //var ass = AppDomain.CurrentDomain.GetAssemblies().Where((a)=>a.IsDynamic).ToArray();
      //var tps = ass.First().GetTypes();
    }

  }

}
