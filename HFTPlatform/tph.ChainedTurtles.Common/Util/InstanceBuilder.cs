using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Interfaces;

namespace tph.ChainedTurtles.Common.Util
{
    public class InstanceBuilder
    {

        public static T BuildInsance<T>(string buildAssembly, Dictionary<string, string> pConfigDict, OnLogMessage pOnLogMessage)
        {

            try
            {
                // Separate the type name and the assembly name
                var parts = buildAssembly.Split(new[] { ',' }, 2);
                if (parts.Length != 2)
                {
                    throw new ArgumentException("Invalid format for buildAssembly. Expected format: 'TypeName,AssemblyName'.");
                }

                string typeName = parts[0].Trim();
                string assemblyName = parts[1].Trim();

                // Load the specified assembly by name
                Assembly assembly = Assembly.Load(assemblyName);

                // Get the type from the assembly
                Type type = assembly.GetType(typeName);

                if (type == null)
                {
                    throw new TypeLoadException($"Type '{typeName}' could not be found in assembly '{assemblyName}'.");
                }

                // Ensure the type implements the interface T
                if (!typeof(T).IsAssignableFrom(type))
                {
                    throw new InvalidCastException($"Type '{typeName}' does not implement interface '{typeof(T).FullName}'.");
                }

                // Create an instance of the type
                T instance = (T)Activator.CreateInstance(type, pConfigDict,pOnLogMessage);

                // Return the created instance
                return instance;
            }
            catch (Exception ex)
            {
                throw new Exception($"CRITICAL error instantiating assembly {buildAssembly}: {ex.Message}", ex);
            }
        }
    }
}
