using Autofac;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Catalyst.Modules.PluginManager
{
    public class PluginManagerModule : Autofac.Module
    {
        private const string ModuleParamterFile = "plugin.json";
        private const string AssemblyNameProperty = "AssemblyName";
        private const string ModuleNameProperty = "ModuleNames";
        private readonly string _pluginFolderPath;

        public PluginManagerModule(string pluginFolderPath)
        {
            _pluginFolderPath = pluginFolderPath;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var pluginPaths = Directory.EnumerateFiles(_pluginFolderPath, "*.json", SearchOption.AllDirectories)
                     .Where(filename => Path.GetFileName(filename).Equals(ModuleParamterFile, StringComparison.InvariantCultureIgnoreCase)).ToList();

            pluginPaths.ForEach(pluginPath =>
            {
                var pluginDir = Path.GetDirectoryName(pluginPath);
                var pluginInfo = JObject.Parse(File.ReadAllText(pluginPath));
                var moduleNames = pluginInfo[ModuleNameProperty].ToObject<string[]>();
                var pluginAssembly = pluginInfo[AssemblyNameProperty].ToObject<string>();
                var assemblies = new List<Assembly>();

                Directory.EnumerateFiles(pluginDir, "*.dll", SearchOption.AllDirectories)
                         .Where(filename => new Regex(".*.dll").IsMatch(filename))
                         .Select(LoadAssembly)
                         .Where(result => result != null)
                         .ToList()
                         .ForEach(result => assemblies.Add(result));

                var assembly = assemblies.Where(t => t.GetName().Name.Equals(pluginAssembly)).FirstOrDefault();
                if (assembly == null)
                {
                    Console.WriteLine($"Plugin assembly {pluginAssembly} not found.");
                    return;
                }

                var clazzes = assembly.GetTypes()
                .Where(clazz => clazz.BaseType == typeof(Autofac.Module)
                && moduleNames.Any(moduleName => clazz.Name.Equals(moduleName, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();

                clazzes.ForEach(clazz =>
                {
                    ResolveModule(pluginInfo, clazz, builder);
                });
            });
        }

        private Assembly LoadAssembly(string fileName)
        {
            try
            {
                return Assembly.LoadFrom(fileName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void ResolveModule(JObject pluginInfo, Type clazz, ContainerBuilder bldr)
        {
            // Assume first public constructor is ctor
            var ctor = clazz.GetConstructors().Where(ct => ct.IsPublic).FirstOrDefault();

            if (ctor == null)
            {
                Console.WriteLine($"Error resolving {clazz.Name}, no public constructor found");
                return;
            }

            bool hasNoArguments = ctor.GetParameters().Length == 0;
            if (hasNoArguments)
            {
                var module = (Autofac.Module)Activator.CreateInstance(clazz);
                LoadModule(bldr, module, clazz);
                return;
            }

            ResolveParametersFromJson(pluginInfo, ctor, clazz, bldr);
        }

        private void ResolveParametersFromJson(JObject pluginInfo, ConstructorInfo info, Type clazz, ContainerBuilder bldr)
        {
            var paramters = info.GetParameters();
            var paramNames = paramters.Select(t => t.Name).ToArray();
            var ctorParams = new object[paramters.Length];
            var paramterInfo = pluginInfo[clazz.Name];

            for (int i = 0; i < paramters.Length; i++)
            {
                var parameter = paramters[i];
                try
                {
                    var resolvedParam = paramterInfo[parameter.Name].ToObject(parameter.ParameterType);
                    ctorParams[i] = resolvedParam;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not load module {clazz.FullName} failed resolving " +
                        $"Parameter: {parameter.Name}, Type {parameter.ParameterType} Error: {e}");
                    return;
                }
            }

            var module = (Autofac.Module)Activator.CreateInstance(clazz, ctorParams);
            LoadModule(bldr, module, clazz);
        }

        private void LoadModule(ContainerBuilder bldr, Autofac.Module module, Type clazz)
        {
            bldr.RegisterModule(module);
            Console.WriteLine("Registered Plugin Module: " + clazz.Name);
        }
    }
}
