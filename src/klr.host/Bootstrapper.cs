﻿
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Net.Runtime;
using Microsoft.Net.Runtime.Common;
using Microsoft.Net.Runtime.Common.DependencyInjection;

namespace klr.host
{
    public class Bootstrapper
    {
        private readonly IHostContainer _container;
        private readonly IAssemblyLoaderEngine _loaderEngine;

        public Bootstrapper(IHostContainer container, 
                            IAssemblyLoaderEngine loaderEngine)
        {
            _container = container;
            _loaderEngine = loaderEngine;
        }

        public async Task<int> Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("{app} [args]");
                return -1;
            }

            var name = args[0];
            var programArgs = args.Skip(1).ToArray();

            var assembly = Assembly.Load(new AssemblyName(name));

            if (assembly == null)
            {
                return -1;
            }

            var serviceProvider = new ServiceProvider();
            serviceProvider.Add(typeof(IHostContainer), _container);
            serviceProvider.Add(typeof(IAssemblyLoaderEngine), _loaderEngine);

            return await EntryPointExecutor.Execute(assembly, programArgs, serviceProvider);
        }
    }
}