using Microsoft.SqlServer.Dac;
using Microsoft.Win32;
using System;
using System.Xml.Linq;
using System.Linq;

namespace DacpacDeployUtility
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var publishFileFullPath = args[0];
                var dacpacFileFullPath = args[1];

                SetupRegistryQueryExecutionTimeout();
                PublishDacpacSimple(publishFileFullPath, dacpacFileFullPath);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Main: " + ex.Message + "\n" + ex.StackTrace);

                for (int i = 0; i < args.Length; i++)
                {
                    Console.WriteLine("Value in args[" + i + "]: " + args[i]);
                }

                Console.WriteLine("Failed to publish dacpac.");

                return 1;
            }
        }

        private static void SetupRegistryQueryExecutionTimeout()
        {
            //Fixes an annoying issue with slow sql servers: https://stackoverflow.com/a/26108419/2912011
            var myKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\VisualStudio\\12.0\\SQLDB\\Database", true);
            if (myKey != null)
            {
                myKey.SetValue("QueryTimeoutSeconds", "0", RegistryValueKind.DWord);
                myKey.Close();
            }

            myKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\VisualStudio\\14.0\\SQLDB\\Database", true);
            if (myKey != null)
            {
                myKey.SetValue("QueryTimeoutSeconds", "0", RegistryValueKind.DWord);
                myKey.Close();
            }

            myKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\VisualStudio\\15.0\\SQLDB\\Database", true);
            if (myKey != null)
            {
                myKey.SetValue("QueryTimeoutSeconds", "0", RegistryValueKind.DWord);
                myKey.Close();
            }
        }

        private static void PublishDacpacSimple(
            string publishFileFullPath,
            string dacpacFileFullPath)
        {
            var root = XElement.Load(publishFileFullPath);
            var ns = root.GetDefaultNamespace(); 
            var targetDatabaseNameNode = root.Descendants(ns + "TargetDatabaseName").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(targetDatabaseNameNode?.Value))
            {
                throw new ApplicationException("Required 'TargetDatabaseName' node is missing or empty");
            }
            var targetDatabaseName = targetDatabaseNameNode.Value;
            var connectionStringNode = root.Descendants(ns + "TargetConnectionString").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(connectionStringNode?.Value))
            {
                throw new ApplicationException("Required 'TargetConnectionString' node is missing or empty");
            }
            var connectionString = connectionStringNode.Value;
            var createNewDatabaseNode = root.Descendants(ns + "CreateNewDatabase").FirstOrDefault();
            bool createNewDatabase = createNewDatabaseNode?.Value?.ToLowerInvariant() == "true";
            var blockOnPossibleDataLossNode = root.Descendants(ns + "BlockOnPossibleDataLoss").FirstOrDefault();
            var blockOnPossibleDataLoss = blockOnPossibleDataLossNode.Value.ToLowerInvariant() == "true";
            var includeCompositeObjectsNode = root.Descendants(ns + "IncludeCompositeObjects").FirstOrDefault();
            var includeCompositeObjects = includeCompositeObjectsNode.Value.ToLowerInvariant() == "true";
            var scriptDatabaseCompatibilityNode = root.Descendants(ns + "ScriptDatabaseCompatibility").FirstOrDefault();
            var scriptDatabaseCompatibility = scriptDatabaseCompatibilityNode?.Value?.ToLowerInvariant() == "true";
            var sqlCmdVariableNodes = root.Descendants(ns + "SqlCmdVariable");
            var ds = new DacServices(connectionString);
            using (var package = DacPackage.Load(dacpacFileFullPath))
            {
                var options = new DacDeployOptions
                {
                    CommandTimeout = 600
                };

                if (createNewDatabaseNode != null)
                {
                    options.CreateNewDatabase = createNewDatabase;
                }

                if (blockOnPossibleDataLossNode != null)
                {
                    options.BlockOnPossibleDataLoss = blockOnPossibleDataLoss;
                }

                if (includeCompositeObjectsNode != null)
                {
                    options.IncludeCompositeObjects = includeCompositeObjects;
                }

                if (scriptDatabaseCompatibilityNode != null)
                {
                    options.ScriptDatabaseCompatibility = scriptDatabaseCompatibility;
                }

                foreach(XElement node in sqlCmdVariableNodes)
                {
                    var variableName = (string)node.Attribute("Include");
                    var valueNode = node.Element(ns + "Value");
                    var variableValue = node.Value;
                    options.SqlCommandVariableValues.Add(variableName, variableValue);
                }

                ds.Message += (object sender, DacMessageEventArgs eventArgs) => Console.WriteLine(eventArgs.Message.Message);

                ds.Deploy(package, targetDatabaseName, true, options);
            }
        }
    }
}
