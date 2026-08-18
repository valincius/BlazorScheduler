using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.IO;
using System.Collections.Generic;

if (args.Length == 0)
{
    Console.WriteLine("Usage: ListTypes <assembly-path>");
    return 1;
}

var path = args[0];
var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
var runtimeAssemblies = Directory.GetFiles(runtimeDir, "*.dll").ToList();
runtimeAssemblies.Add(path);
var resolver = new PathAssemblyResolver(runtimeAssemblies);
using var mlc = new MetadataLoadContext(resolver);
var asm = mlc.LoadFromAssemblyPath(Path.GetFullPath(path));
var types = asm.GetTypes().OrderBy(t => t.FullName);
foreach (var t in types)
{
    Console.WriteLine(t.FullName);
}
return 0;

// using PathAssemblyResolver from System.Reflection.MetadataLoadContext
