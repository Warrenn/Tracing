using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using Microsoft.CSharp.RuntimeBinder;
using Mono.Cecil;
using Mono.Cecil.Inject;

namespace EnableTracing
{
    class Program
    {
        static void Main(string[] args)
        {
            var target = args[0];

            if (!File.Exists(target))
            {
                throw new FileNotFoundException(target);
            }

            var path = Path.GetDirectoryName(target);
            var fileName = Path.GetFileName(target);
            var targetdef = AssemblyLoader.LoadAssembly(target);
            var backupPath = $"{path}\\{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";
            var hookdll = $"{path}\\{Path.GetFileNameWithoutExtension(fileName)}_Hook.dll";
            File.Copy(target, $"{backupPath}\\{fileName}");

            if (File.Exists(hookdll))
            {
                File.Delete(hookdll);
            }

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "EnableTracing.TraceSourceHook.cs";
            var hookClass = string.Empty;

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                hookClass = reader.ReadToEnd();
            }

            var mod = targetdef.MainModule;
            var traceClasses = new List<string> { hookClass };

            foreach (var typeDefinition in mod.GetTypes().Where(t => t.HasMethods && !t.IsInterface && t.IsClass))
            {
                var stringbuilder = new StringBuilder(
                    $@"
                        using System.Linq;
                        using System;
                        using System.Collections.Concurrent;
                        using System.Diagnostics;

                        namespace Hook {{
                            public static class {typeDefinition.Name}Tracer
                            {{
                     ");
                foreach (var method in typeDefinition.Methods.Where(m => m.HasBody && !m.IsConstructor))
                {
                    var genericParameters = string.Join(",", method.GenericParameters.Select(p => p.Name));
                    genericParameters = string.IsNullOrEmpty(genericParameters)
                        ? genericParameters
                        : $"<{genericParameters}>";

                    var parameterDefinition = string.Join(",",
                        method.Parameters.Select(p => $"{p.ParameterType.FullName} {p.Name}"));
                    var inlineParameters = string.Join(",",
                        method.Parameters.Select(p => $"{p.Name}"));
                    inlineParameters = string.IsNullOrEmpty(inlineParameters)
                        ? inlineParameters
                        : $",{inlineParameters}";

                    stringbuilder.Append(
                        $@"
                                public static void {method.Name}{genericParameters}({parameterDefinition})
                                {{
                                    TraceSourceHook.TraceWrite(""{typeDefinition.Namespace}"", ""{typeDefinition.FullName}.{method.Name}""{inlineParameters});
                                }}
                        ");
                }
                stringbuilder.Append(
                    $@"
                            }}    
                        }}");
                traceClasses.Add(stringbuilder.ToString());
            }


            var csc = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });
            var parameters = new CompilerParameters(new[] { "mscorlib.dll", "System.Core.dll", "System.dll" }, hookdll,
                true)
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
                CompilerOptions = "/d:TRACE"
            };

            var results = csc.CompileAssemblyFromSource(parameters, traceClasses.ToArray());

            if (results.Errors.HasErrors)
            {
                var ex = new RuntimeBinderInternalCompilerException();
                ex.Data["Errors"] = results.Errors;
                throw ex;
            }

            var hookMod = AssemblyLoader.LoadAssembly(hookdll).MainModule;

            var methods =
                from typeDefinition in mod.GetTypes().Where(t => t.HasMethods && !t.IsInterface && t.IsClass)
                let hookType = hookMod.GetType($"{typeDefinition.Name}Tracer")
                from method in typeDefinition.Methods.Where(m => m.HasBody && !m.IsConstructor)
                let hookMethod = hookType.GetMethod(method.Name)
                select new { typeDefinition, method, hookType, hookmeth = hookMethod };

            foreach (var definition in methods)
            {
                var injector = new InjectionDefinition(definition.method,
                    definition.hookmeth,
                    InjectFlags.PassParametersVal);
                injector.Inject();
            }

            targetdef.Write(target, new WriterParameters {WriteSymbols = true});
        }


    }

}
