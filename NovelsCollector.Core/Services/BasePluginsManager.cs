﻿using MongoDB.Driver;
using NovelsCollector.Core.Exceptions;
using NovelsCollector.Core.Models;
using NovelsCollector.Core.Utils;
using NovelsCollector.SDK.Plugins;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace NovelsCollector.Core.Services
{
    public abstract class BasePluginsManager<Abstract, Interface> where Abstract : BasePlugin where Interface : IPlugin
    {
        #region Properties

        protected readonly ILogger _logger;

        // Storing the plugins and their own contexts
        public List<Abstract> Installed { get; }

        // The path to the /___-plugins and /temp folders
        protected string _tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
        protected string _pluginsPath;

        // The collection of source-plugins in the database
        protected IMongoCollection<Abstract> _pluginsCollection;

        // FOR DEBUGGING: The list of weak references to the unloaded contexts in the past
        public List<WeakReference> unloadedHistory = new List<WeakReference>();

        #endregion

        /// <summary>
        /// The constructor of the BasePluginsManager class.
        /// </summary>
        /// <param name="logger"> The logger, injected by the DI. </param>
        /// <param name="mongoDbContext"> The MongoDB context, injected by the DI. </param>
        /// <param name="collectionName"> The name of the collection in the database. e.g: "Sources". </param>
        /// <param name="pluginsFolderName"> The name of the folder to store the plugins. e.g: "source-plugins". </param>
        public BasePluginsManager(ILogger logger, MongoDbContext mongoDbContext, string collectionName, string pluginsFolderName)
        {
            _logger = logger;
            _pluginsCollection = mongoDbContext.GetCollection<Abstract>(collectionName);
            _pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pluginsFolderName);

            // Get installed plugins from the database
            Installed = _pluginsCollection.Find(plugin => true).ToList();

            // Create the /___-plugins and /temp folders if not exist
            if (!Directory.Exists(_pluginsPath)) Directory.CreateDirectory(_pluginsPath);
            if (!Directory.Exists(_tempPath)) Directory.CreateDirectory(_tempPath);

            // Load all installed plugins
            loadAll();
        }

        /// <summary>
        /// Load all installed plugins.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // Avoid JIT optimizations which causes issues with PluginLoadContext.Unload() (cannot GC)
        protected void loadAll()
        {
            unloadAll();
            foreach (var plugin in Installed)
            {
                if (plugin.Name != null && plugin.PluginInstance == null)
                {
                    try
                    {
                        LoadPlugin(plugin.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error loading plugin {plugin.Name}");
                    }
                }
            }
        }

        /// <summary>
        /// Unload all loaded plugins.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void unloadAll()
        {
            foreach (var plugin in Installed)
            {
                if (plugin.Name != null && plugin.PluginInstance != null)
                {
                    try
                    {
                        UnloadPlugin(plugin.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error unloading plugin {plugin.Name}");
                    }
                }
            }

            // Count how many plugins are still loaded
            int countStillLoaded = Installed.Count(plugin => plugin.PluginInstance != null);
            if (countStillLoaded > 0)
            {
                _logger.LogError($"{countStillLoaded} plugins are not unloaded properly");
            }
            else
            {
                _logger.LogInformation("All plugins are UNLOADED successfully");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// Load a plugin by name.
        /// </summary>
        /// <param name="pluginName"> The name of the plugin to load. </param>
        /// <exception cref="NotFoundException"> Plugin not found. </exception>
        /// <exception cref="Exception"> Plugin has been already loaded. </exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LoadPlugin(string pluginName)
        {
            // Find the plugin in the Installed list
            Abstract? loadingPlugin = Installed.Find(p => p.Name == pluginName);

            // If the plugin is not found or already loaded, throw an exception
            if (loadingPlugin == null)
                throw new NotFoundException("Plugin not found");
            if (loadingPlugin.PluginInstance != null)
                throw new Exception("Plugin has been already loaded");

            // If the plugin is not loaded, load it

            // Path to the plugin dll: /___-plugins/{pluginName}/{loadingPlugin.Assembly} .e.g: /source-plugins/TruyenFullVn/Source.TruyenFullVn.dll
            string pluginNameFolder = Path.Combine(_pluginsPath, pluginName);
            if (!Directory.Exists(pluginNameFolder))
                throw new NotFoundException($"Plugin folder /{pluginName} not found");
            string assemblyName = loadingPlugin.Assembly;
            if (!assemblyName.EndsWith(".dll")) assemblyName += ".dll";
            string? pluginDll = Directory.GetFiles(pluginNameFolder, $"{assemblyName}").FirstOrDefault();
            if (pluginDll == null)
                throw new NotFoundException($"Assembly {assemblyName} not found");

            // Create a new context to load the plugin into
            PluginLoadContext loadContext = new PluginLoadContext(pluginDll);

            // Load the plugin assembly: {plugin.Assembly} .e.g: Source.{pluginName}.dll
            Assembly pluginAssembly = loadContext.LoadFromAssemblyName(AssemblyName.GetAssemblyName(pluginDll));
            Type[] types = pluginAssembly.GetTypes();
            foreach (var type in types)
            {
                // If the type implementing the ISourcePlugin/IExporter interface, then create an instance of it
                if (typeof(Interface).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    if (Activator.CreateInstance(type) is Interface plugin)
                    {
                        // Load the plugin
                        loadingPlugin.PluginInstance = plugin;
                        loadingPlugin.LoadContext = loadContext;
                        loadingPlugin.IsLoaded = true;
                        _logger.LogInformation($"\tLOADED plugin {pluginName} from {assemblyName}");
                        return;
                    }
                }
            }

            // If there is no plugin SourcePlugin/ExporterPlugin loaded, cancel the loading process
            loadContext.Unload();
            loadingPlugin.IsLoaded = false;
            loadingPlugin.PluginInstance = null;
            loadingPlugin.LoadContext = null;
            throw new NotFoundException($"No plugin found in {assemblyName}");

        }

        /// <summary>
        /// Unload a plugin by name.
        /// </summary>
        /// <param name="pluginName"> The name of the plugin to unload. </param>
        /// <exception cref="NotFoundException"> Plugin not found. </exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UnloadPlugin(string pluginName)
        {
            // Find the plugin in the Installed list
            Abstract? unloadingPlugin = Installed.Find(p => p.Name == pluginName);

            // If the plugin is not found or already unloaded, throw an exception
            if (unloadingPlugin == null)
                throw new NotFoundException("Plugin not found");
            if (unloadingPlugin.PluginInstance == null)
                throw new Exception("Plugin has been already unloaded");


            // FOR DEBUGGING: Add the loaded context to the history
            unloadedHistory.Add(new WeakReference(unloadingPlugin.LoadContext));

            // If the plugin is loaded, unload it
            unloadingPlugin.LoadContext?.Unload();
            unloadingPlugin.IsLoaded = false;
            unloadingPlugin.PluginInstance = null;
            unloadingPlugin.LoadContext = null;
            _logger.LogInformation($"\tUNLOADING plugin {pluginName} has been initiated");

        }

        /// <summary>
        /// Add a new plugin from a file.
        /// </summary>
        /// <param name="file"> The file to add as a plugin, in .zip format. </param>
        /// <returns> The name of the plugin added. </returns>
        /// <exception cref="NotFoundException"> Manifest file not found. </exception>
        /// <exception cref="Exception"> Plugin already exists. </exception>
        public async Task<string> AddPluginFromFile(IFormFile file)
        {
            // Get the timestamp to create a unique folder
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            // Download the plugin into the folder
            string pluginZipPath = Path.Combine(_tempPath, timestamp + ".zip");
            string tempPath = Path.Combine(_tempPath, timestamp);
            Directory.CreateDirectory(tempPath);

            // Download the plugin
            _logger.LogInformation($"\tDownloading new plugin");
            using (var stream = new FileStream(pluginZipPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Extract the plugin
            _logger.LogInformation($"\tExtracting plugin to /temp/{timestamp}");
            ZipFile.ExtractToDirectory(pluginZipPath, tempPath);
            File.Delete(pluginZipPath);

            // Delete the .pdb file - they are not needed
            string[] pdbFiles = Directory.GetFiles(tempPath, "*.pdb");
            foreach (var pdbFile in pdbFiles) File.Delete(pdbFile);

            // Read the plugin name, author, version, etc. from the manifest file
            string manifestPath = Path.Combine(tempPath, "manifest.json");
            if (!File.Exists(manifestPath))
                throw new NotFoundException("Manifest file not found");

            // Deserialize manifest and get the new plugin
            string manifestContent = File.ReadAllText(manifestPath);
            var newPlugin = JsonSerializer.Deserialize<Abstract>(manifestContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            });
            if (newPlugin == null)
                throw new NotFoundException("Manifest file is invalid");

            // Check if the plugin already exists
            if (Installed.Any(plugin => plugin.Name == newPlugin.Name))
                throw new Exception("Plugin already exists");

            // Add the plugin to the Installed list
            Installed.Add(newPlugin);

            // Move the plugin to the plugins folder
            string pluginPath = Path.Combine(_pluginsPath, newPlugin.Name);
            if (Directory.Exists(pluginPath)) Directory.Delete(pluginPath, true);
            Directory.Move(tempPath, pluginPath);

            // Try to load the plugin
            try
            {
                LoadPlugin(newPlugin.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading plugin {newPlugin.Name}");
                Installed.Remove(newPlugin);
                Directory.Delete(pluginPath, true);
                throw;
            }

            // If loaded successfully, add the plugin to the database
            await _pluginsCollection.InsertOneAsync(newPlugin);

            _logger.LogInformation($"\tPlugin {newPlugin.Name} ADDED successfully");
            return newPlugin.Name;
        }

        public async void RemovePlugin(string pluginName)
        {
            // Find the plugin in the Installed list
            Abstract? removingPlugin = Installed.Find(p => p.Name == pluginName);

            // If the plugin is not found, throw an exception
            if (removingPlugin == null)
                throw new NotFoundException("Plugin not found");

            // If the plugin is loaded, unload it
            if (removingPlugin.PluginInstance != null)
            {
                try
                {
                    UnloadPlugin(pluginName);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error unloading plugin", ex);
                }
            }

            // Remove the plugin from the Installed list
            Installed.Remove(removingPlugin);

            // Remove the plugin from the database
            await _pluginsCollection.DeleteOneAsync(plugin => plugin.Name == pluginName);

            // Ensure the plugin is unloaded, before deleting the folder, to avoid file locks
            var count = 10;
            do
            {
                count--;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            } while (unloadedHistory.LastOrDefault()?.IsAlive == true && count > 0);

            // Delete the plugin folder
            string pluginPath = Path.Combine(_pluginsPath, pluginName);
            if (Directory.Exists(pluginPath))
                try { Directory.Delete(pluginPath, true); } catch { Console.WriteLine("Error deleting plugin folder"); }

            _logger.LogInformation($"\tPlugin {pluginName} REMOVED successfully");
        }


    }
}