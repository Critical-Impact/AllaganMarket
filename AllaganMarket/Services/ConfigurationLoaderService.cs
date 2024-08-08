using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AllaganLib.Data.Service;

using AllaganMarket.Models;

using DalaMock.Host.Mediator;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public class ConfigurationLoaderService : IHostedService
{
    private readonly CsvLoaderService csvLoaderService;
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog pluginLog;
    private Configuration? configuration;

    public ConfigurationLoaderService(
        CsvLoaderService csvLoaderService,
        IDalamudPluginInterface pluginInterface,
        IPluginLog pluginLog)
    {
        this.csvLoaderService = csvLoaderService;
        this.pluginInterface = pluginInterface;
        this.pluginLog = pluginLog;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Configuration GetConfiguration()
    {
        if (this.configuration == null)
        {
            try
            {
                this.configuration = this.pluginInterface.GetPluginConfig() as Configuration ??
                                     new Configuration();
            }
            catch (Exception e)
            {
                this.pluginLog.Error(e, "Failed to load configuration");
                this.configuration = new Configuration();
            }

            this.LoadCsvs(this.configuration);
        }

        return this.configuration;
    }

    public void Save()
    {
        this.GetConfiguration().IsDirty = false;
        this.pluginInterface.SavePluginConfig(this.GetConfiguration());
        this.SaveCsvs();
    }

    private void LoadCsvs(Configuration configuration)
    {
        try
        {
            var saleItems = this.csvLoaderService.LoadCsv<SaleItem>(Path.Combine(this.pluginInterface.GetPluginConfigDirectory(), "SaleItems.csv"), out var failedLines);
            configuration.SaleItems = saleItems.GroupBy(c => c.RetainerId).ToDictionary(c => c.Key, c => c.ToArray());
            foreach (var failedLine in failedLines)
            {
                this.pluginLog.Error($"Failed to parse line {failedLine}");
            }
        }
        catch (FileNotFoundException)
        {
        }

        try
        {
            configuration.Sales = this.csvLoaderService.LoadCsv<SoldItem>(Path.Combine(this.pluginInterface.GetPluginConfigDirectory(), "SoldItems.csv"), out _).GroupBy(c => c.RetainerId).ToDictionary(c => c.Key, c => c.ToList());
        }
        catch (FileNotFoundException)
        {
        }
    }

    private void SaveCsvs()
    {
        var loadedConfiguration = this.GetConfiguration();
        this.csvLoaderService.ToCsv(
            loadedConfiguration.SaleItems.SelectMany(c => c.Value).ToList(),
            Path.Combine(this.pluginInterface.GetPluginConfigDirectory(), "SaleItems.csv"));
        this.csvLoaderService.ToCsv(
            loadedConfiguration.Sales.SelectMany(c => c.Value).ToList(),
            Path.Combine(this.pluginInterface.GetPluginConfigDirectory(), "SoldItems.csv"));
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.Save();
        this.pluginLog.Verbose("Stopping configuration loader, saving.");
        return Task.CompletedTask;
    }
}
