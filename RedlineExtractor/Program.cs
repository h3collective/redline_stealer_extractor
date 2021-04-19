using System;
using System.Reflection;
using Newtonsoft.Json;

namespace RedlineExtractor
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length != 2)
            {
                Console.WriteLine(">> r1n9w0rm - Extract CnC/Id for RedLine stealer");
                Console.WriteLine("Usage: " + System.AppDomain.CurrentDomain.FriendlyName + " <Input File> <Output File>");
                System.Environment.Exit(1);
            }

            string inputPath = args[0];
            string outputPath = args[1];

            bool extracted = extract(inputPath, outputPath);

        }


        static bool extract(string input, string output)
        {
            Assembly a = LoadAssembly(input);

            // Loop through all methods, to identify a class containing a constructor that uses private methods
            // The two private methods are IP and ID


            Module[] modules = a.GetModules();
            var types = modules[0].GetTypes();

            foreach (Type t in types)
            {
                string typeName = t.ToString();
                MemberTypes memberType = t.MemberType;
                if (!typeName.Contains("Data.Core.Launchers") || memberType != MemberTypes.TypeInfo)
                {
                    // Skip irrelevant types
                    continue;
                }

                // Creates an instance of the executor class
                object executorInstance = null;
                try
                {
                    executorInstance = Activator.CreateInstance(t);
                }
                catch (System.MissingMethodException)
                {
                    executorInstance = Activator.CreateInstance(
                     t,
                     BindingFlags.CreateInstance |
                     BindingFlags.Public |
                     BindingFlags.Instance |
                     BindingFlags.OptionalParamBinding,
                     null, new Object[] { Type.Missing }, null
                     );
                }
  

                // Ensure instance has IP and ID members, if so extract
                bool hasIpMember = false;
                bool hasIdMember = false;
                foreach (var m in t.GetMembers(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (m.Name == "IP" && m.MemberType == MemberTypes.Property)
                    {
                        hasIpMember = true;
                    }
                    else if (m.Name == "ID" && m.MemberType == MemberTypes.Property)
                    {
                        hasIdMember = true;
                    }

                    if (hasIpMember && hasIdMember)
                    {

                        PropertyInfo ipPi = t.GetProperty("IP", BindingFlags.NonPublic | BindingFlags.Instance);
                        string ipAddress = (string)ipPi.GetValue(executorInstance, null);

                        PropertyInfo idPi = t.GetProperty("ID", BindingFlags.NonPublic | BindingFlags.Instance);
                        string id = (string)idPi.GetValue(executorInstance, null);
                        if (string.IsNullOrEmpty(id))
                        {
                            id = "N/A";
                        }

                        Console.WriteLine("IP: " + ipAddress);
                        Console.WriteLine("ID: " + id);

                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(output, true))
                        {
                            file.WriteLine("{\"ip\": " + JsonConvert.ToString(ipAddress) + ", \"id\": " + JsonConvert.ToString(id) + "}");
                        }

                        break;
                    }

                }
            }
            return true;
        }

        static Assembly LoadAssembly(string input)
        {
            Assembly a = null;
            try
            {
                a = Assembly.Load(System.IO.File.ReadAllBytes(input));
            }
            catch (BadImageFormatException)
            {
                var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(input);

                using (var memoryStream = new System.IO.MemoryStream())
                {
                    assembly.Write(memoryStream);
                    byte[] asssemblyBytes = memoryStream.ToArray();
                    a = Assembly.Load(asssemblyBytes);
                }
            }

            return a;
        }

    }
}
