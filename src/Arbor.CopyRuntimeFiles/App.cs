using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Autofac;

namespace Arbor.CopyRuntimeFiles
{
    internal class App
    {
        private static IContainer _container;

        private readonly List<string> _blackListed = new List<string>{ "node_modules", "bin", "obj" };

        private readonly List<string> _blackListedFileExtensions = new List<string>{ ".tmp" };

        public static Task<int> CreateAndRunAsync(string[] args)
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<App>().SingleInstance();
            builder.RegisterType<ArgValidator>().SingleInstance();

            _container = builder.Build();

            var validator = _container.Resolve<ArgValidator>();

            int validatorExitCode = validator.Validate(args);

            if (validatorExitCode != 0)
            {
                return Task.FromResult(validatorExitCode);
            }

            var app = _container.Resolve<App>();

            return app.RunAsync(args);
        }

        private Task<int> RunAsync(string[] args)
        {
            string rootDirectory = VcsPathHelper.FindVcsRootPath(Directory.GetCurrentDirectory());

            string sourceDirectoryRelativePath = args[0];
            string targetDirectoryRelativePath = args[1];
            string[] filters = args[2].Split(';').ToArray();

            if (args.Length >= 4)
            {
                string[] blackListedItems = args[3].Split(';');

                _blackListed.AddRange(blackListedItems);
            }

            var sourceDirectory = new DirectoryInfo(Path.Combine(rootDirectory, sourceDirectoryRelativePath));
            var targetDirectory = new DirectoryInfo(Path.Combine(rootDirectory, targetDirectoryRelativePath));

            if (!sourceDirectory.Exists)
            {
                Console.WriteLine($"Source directory '{sourceDirectory.FullName}' does not exist");
                return Task.FromResult(0);
            }

            if (!targetDirectory.Exists)
            {
                Console.WriteLine($"Target directory '{targetDirectory.FullName}' does not exist");
                return Task.FromResult(0);
            }

            Console.WriteLine($"Using source directory '{sourceDirectory.FullName}'");
            Console.WriteLine($"Using target directory '{targetDirectory.FullName}'");

            Console.WriteLine();
            Console.WriteLine("[Black-listed]");
            foreach (string blackListedItem in _blackListed)
            {
                Console.WriteLine($"\t* '{blackListedItem}'");
            }

            Console.WriteLine();
            Console.WriteLine("[Filters]");
            foreach (string filter in filters)
            {
                Console.WriteLine($"\t* '{filter}'");
            }

            Console.WriteLine();
            Console.WriteLine("[Black-listed file extensions]");
            foreach (string fileExtension in _blackListedFileExtensions)
            {
                Console.WriteLine($"\t* '{fileExtension}'");
            }

            foreach (string filter in filters)
            {
                CreateWatcher(sourceDirectory, sourceDirectory, filter, targetDirectory);
            }

            _ResetEvent.Wait();

            return Task.FromResult(0);
        }

        private static readonly ManualResetEventSlim _ResetEvent = new ManualResetEventSlim(false);

        private void CreateWatcher(
            DirectoryInfo sourceRootDirectory,
            DirectoryInfo currentDirectory,
            string filePattern,
            DirectoryInfo targetDirectory)
        {
            if (_blackListed.Any(blackListedItem =>
                blackListedItem.Equals(currentDirectory.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var fileSystemWatcher = new FileSystemWatcher(currentDirectory.FullName, filePattern);

            Console.WriteLine($"Created watcher for {filePattern} '{currentDirectory.FullName}'");

            fileSystemWatcher.Changed += (sender, eventArgs) =>
            {
                try
                {
                    fileSystemWatcher.EnableRaisingEvents = false;

                    CopyFile(sourceRootDirectory,
                        targetDirectory,
                        eventArgs.FullPath,
                        eventArgs.ChangeType,
                        currentDirectory.FullName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not copy changed file '{eventArgs.FullPath}'. {ex}");
                }
                finally
                {
                    fileSystemWatcher.EnableRaisingEvents = true;
                }
            };

            fileSystemWatcher.Renamed += (sender, eventArgs) =>
            {
                try
                {
                    fileSystemWatcher.EnableRaisingEvents = false;

                    CopyFile(sourceRootDirectory,
                        targetDirectory,
                        eventArgs.FullPath,
                        eventArgs.ChangeType,
                        currentDirectory.FullName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not copy renamed file '{eventArgs.FullPath}'. {ex}");
                }
                finally
                {
                    fileSystemWatcher.EnableRaisingEvents = true;
                }
            };

            fileSystemWatcher.Created += (sender, eventArgs) =>
            {
                try
                {
                    CopyFile(sourceRootDirectory,
                        targetDirectory,
                        eventArgs.FullPath,
                        eventArgs.ChangeType,
                        currentDirectory.FullName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not copy created file '{eventArgs.FullPath}'. {ex}");
                }
            };

            fileSystemWatcher.Deleted += (sender, eventArgs) =>
            {
                try
                {
                    DeleteFile(sourceRootDirectory, targetDirectory, eventArgs.FullPath, currentDirectory.FullName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not delete file '{eventArgs.FullPath}'. {ex}");
                }
            };

            fileSystemWatcher.EnableRaisingEvents = true;

            foreach (DirectoryInfo subDirectory in currentDirectory.GetDirectories())
            {
                CreateWatcher(sourceRootDirectory, subDirectory, filePattern, targetDirectory);
            }
        }

        private void CopyFile(
            DirectoryInfo sourceDirectory,
            DirectoryInfo targetDirectory,
            string fileFullPath,
            WatcherChangeTypes eventArgsChangeType, string watchPath)
        {
            string extension = Path.GetExtension(fileFullPath);

            if (_blackListedFileExtensions.Any(blackListed =>
                blackListed.Equals(extension, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            string targetFullPath = targetDirectory.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar +
                                    fileFullPath.Replace(sourceDirectory.FullName, "")
                                        .TrimStart(Path.DirectorySeparatorChar);

            File.Copy(fileFullPath, targetFullPath, overwrite: true);
            Console.WriteLine($"File changed ({eventArgsChangeType}), '{fileFullPath}' watcher '{watchPath}'");
            Console.WriteLine($"Copying file to '{targetFullPath}'");
        }

        private void DeleteFile(DirectoryInfo sourceDirectory, DirectoryInfo targetDirectory, string fileFullPath, string watchPath)
        {
            string targetFullPath =
                $"{targetDirectory.FullName.TrimEnd(Path.DirectorySeparatorChar)}{Path.DirectorySeparatorChar}{fileFullPath.Replace(sourceDirectory.FullName, "").TrimStart(Path.DirectorySeparatorChar)} watcher '{watchPath}'";

            if (File.Exists(targetFullPath))
            {
                Console.WriteLine($"Deleting file '{targetFullPath}'");
                File.Delete(targetFullPath);
            }
        }
    }
}