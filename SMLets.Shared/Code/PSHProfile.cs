using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace SMLets.Shared.Code
{
    [Cmdlet(VerbsCommon.Set, "SCSMDefaultComputer")]
    public class SetDefaultComputerToProfile : PSCmdlet
    {
        // Parameters
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The default computer to use for the connection to the Service Manager Data Access Service")]
        [ValidateNotNullOrEmpty]
        public string ComputerName { get; set; }

        [Parameter]
        public SwitchParameter DoNotSaveToProfile { get; set; }

        protected override void ProcessRecord()
        {
            if (!DoNotSaveToProfile.ToBool())
            {
                var profileObject = (PSObject) SessionState.PSVariable.GetValue("PROFILE");
                if (profileObject == null)
                    ThrowTerminatingError(
                        new ErrorRecord(new Exception("Unable to get profile variable"), "GenericMessage",
                            ErrorCategory.InvalidOperation, ComputerName)
                    );

                var path = (string) profileObject.Properties["CurrentUserAllHosts"].Value;
                if (string.IsNullOrEmpty(path))
                    ThrowTerminatingError(
                        new ErrorRecord(new Exception("Unable to get profile path"), "GenericMessage",
                            ErrorCategory.InvalidOperation, ComputerName)
                    );

                using (var fs = File.OpenWrite(path))
                using (StreamWriter wr = new StreamWriter(fs))
                {
                    wr.WriteLine("");
                    wr.WriteLine("# default server name for SMLets");
                    wr.WriteLine($"$SMDefaultComputer = \"{ComputerName}\"");
                }
            }

            SessionState.PSVariable.Set("SMDefaultComputer", ComputerName);
        }
    }
}
