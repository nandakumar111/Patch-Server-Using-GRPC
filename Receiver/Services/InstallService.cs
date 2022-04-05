using Microsoft.Extensions.Logging;
using Installer;
using Grpc.Core;
using System.Threading.Tasks;
using System.Management.Automation;

namespace Receiver.Services
{
	public class InstallService : PowershellManager.PowershellManagerBase
	{
		private readonly ILogger _logger;

		public InstallService(ILoggerFactory loggerFactory)
		{
			_logger = loggerFactory.CreateLogger<FileShareService>();
		}

        public override Task<PowershellExecuteRes> ExecutePsScript(PowershellExecuteReq request, ServerCallContext context)
        {

            var ps = PowerShell.Create();

			if (!string.IsNullOrEmpty(request.Script))
				ps.AddScript(request.Script);

			if (!string.IsNullOrEmpty(request.Commmand))
				ps.AddCommand(request.Commmand);

			foreach (var arg in request.Argument)
				ps.AddArgument(arg);

			var res = ps.Invoke();

			var resBuilder = new System.Text.StringBuilder();
			foreach (PSObject pSObject in res)
				resBuilder.AppendLine(pSObject.ToString());

			return Task.FromResult(new PowershellExecuteRes{ 
				Message = string.IsNullOrEmpty(resBuilder.ToString()) ? $"Powershell Command ({request.Commmand}) Excuted Successfully." : resBuilder.ToString() 
			});
		}
    }
}

