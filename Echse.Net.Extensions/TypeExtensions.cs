using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Echse.Net.Extensions
{
    public static class TypeExtensions
    {
        public static Type[] LoadType(this string typeName)
        {
            return LoadType(typeName, true);
        }

        public static Type[] LoadType(this string typeName, bool referenced)
        {
            return LoadType(typeName, referenced, true);
        }


        private static ConcurrentBag<Type> CachedApocalypseTypes = new ConcurrentBag<Type>();
        public static Type GetTypesOfProject(this string apocType, string directory, string dllRegex = "Echse.*.dll")
        {
            var foundCachedType = CachedApocalypseTypes.FirstOrDefault(t => t.FullName == apocType);

            if (foundCachedType != null)
                return foundCachedType;
            
            string baseNameSpace = dllRegex;
            Console.WriteLine($"Looking in {directory}");
            foreach (var file in Directory.EnumerateFiles(directory, baseNameSpace, SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var loadedAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
                    var foundLoadedType = loadedAssembly.GetTypes().FirstOrDefault(t => t.FullName == apocType);
                    if (foundLoadedType != null)
                    {
                        CachedApocalypseTypes.Add(foundLoadedType);
                        return foundLoadedType;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            return null;
        }

        /// <summary>
        /// Loads a type by a string type representation (With namespace)
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="referenced"></param>
        /// <param name="gac"></param>
        /// <returns></returns>
        public static Type[] LoadType(this string typeName, bool referenced, bool gac)
        {
            if (string.IsNullOrEmpty(typeName))
                return new Type[]{};

            if (typeName.StartsWith("System."))
                return new Type[] {Type.GetType(typeName)};
            
            //check for problematic work
            if (!referenced && !gac)
            {
                var directoryPathOfExecution = Directory.GetCurrentDirectory();
                var directoryPathOfDll = Assembly.GetExecutingAssembly().Location;

                
                if (directoryPathOfDll.StartsWith("file:"))
                    directoryPathOfDll = Path.GetDirectoryName(string.Join("",directoryPathOfDll.Skip("file:".Length)));
                
                //lets assume the file is a local file
                if (directoryPathOfDll != null && directoryPathOfDll.StartsWith("\\\\"))
                    directoryPathOfDll = string.Join("",directoryPathOfDll.Skip("\\\\".Length));

                var directoryPathOfExecutionType = typeName.GetTypesOfProject(directoryPathOfExecution);
                if (directoryPathOfExecutionType != null)
                    return new[] { directoryPathOfExecutionType };

                var directoryPathOfDllType = typeName.GetTypesOfProject(directoryPathOfDll);
                if (directoryPathOfDllType != null)
                    return new[] { directoryPathOfDllType };

                return null;
            }

            Assembly currentAssembly = Assembly.GetCallingAssembly();
            
            List<string> assemblyFullnames = new List<string>();
            List<Type> types = new List<Type>();

            if (referenced)
            {            //Check refrenced assemblies
                foreach (AssemblyName assemblyName in currentAssembly.GetReferencedAssemblies().Where(a => !a.Name.StartsWith("System.")))
                {
                    
                    //Load method resolve refrenced loaded assembly
                    Assembly assembly = Assembly.Load(assemblyName.FullName);

                    var type = assembly.GetType(typeName, false, true);
                    
                    if (type != null && !assemblyFullnames.Contains(assembly.FullName))
                    {
                        types.Add(type);
                        assemblyFullnames.Add(assembly.FullName);
                    }
                }
            }

            if (gac)
            {
                //GAC files
                string gacPath = Environment.GetFolderPath(System.Environment.SpecialFolder.Windows) + "\\assembly";
                var files = GetGlobalAssemblyCacheFiles(gacPath);
                foreach (string file in files)
                {
                    try
                    {
                        //reflection only
                        Assembly assembly = Assembly.ReflectionOnlyLoadFrom(file);

                        //Check if type is exists in assembly
                        var type = assembly.GetType(typeName, false, true);

                        if (type != null && !assemblyFullnames.Contains(assembly.FullName))
                        {
                            types.Add(type);
                            assemblyFullnames.Add(assembly.FullName);
                        }
                    }
                    catch
                    {
                        //your custom handling
                    }
                }
            }

            return types.ToArray();
        }

        public static string[] GetGlobalAssemblyCacheFiles(this string path)
        {
            List<string> files = new List<string>();

            DirectoryInfo di = new DirectoryInfo(path);

            foreach (FileInfo fi in di.GetFiles("*.dll"))
            {
                files.Add(fi.FullName);
            }

            foreach (DirectoryInfo diChild in di.GetDirectories())
            {
                var files2 = GetGlobalAssemblyCacheFiles(diChild.FullName);
                files.AddRange(files2);
            }

            return files.ToArray();
        }
    }
}