using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AllaganLib.Data.Service;

using AllaganMarket.Extensions;
using AllaganMarket.Models;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public class ConfigurationLoaderService(
    CsvLoaderService csvLoaderService,
    IDalamudPluginInterface pluginInterface,
    IPluginLog pluginLog) : IHostedService
{
    private Configuration? configuration;

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
                this.configuration = pluginInterface.GetPluginConfig() as Configuration ??
                                     new Configuration();
            }
            catch (Exception e)
            {
                pluginLog.Error(e, "Failed to load configuration");
                this.configuration = new Configuration();
            }

            this.LoadCsvs(this.configuration);
        }

        return this.configuration;
    }

    public void Save()
    {
        this.GetConfiguration().IsDirty = false;
        pluginInterface.SavePluginConfig(this.GetConfiguration());
        this.SaveCsvs();
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.Save();
        pluginLog.Verbose("Stopping configuration loader, saving.");
        return Task.CompletedTask;
    }

    private void LoadCsvs(Configuration configuration)
    {
        try
        {
            var saleItems = csvLoaderService.LoadCsv<SaleItem>(
                Path.Combine(pluginInterface.GetPluginConfigDirectory(), "SaleItems.csv"),
                out var failedLines);
            configuration.SaleItems = saleItems.GroupBy(c => c.RetainerId).ToDictionary(c => c.Key, c => c.ToArray().FillList(c.Key));
            foreach (var failedLine in failedLines)
            {
                pluginLog.Error($"Failed to parse line {failedLine}");
            }
        }
        catch (FileNotFoundException)
        {
        }

        try
        {
            configuration.Sales = csvLoaderService
                                      .LoadCsv<SoldItem>(
                                          Path.Combine(
                                              pluginInterface.GetPluginConfigDirectory(),
                                              "SoldItems.csv"),
                                          out _).GroupBy(c => c.RetainerId).ToDictionary(c => c.Key, c => c.ToList());
        }
        catch (FileNotFoundException)
        {
        }

        try
        {
            configuration.MarketPriceCache = csvLoaderService
                                      .LoadCsv<MarketPriceCache>(
                                          Path.Combine(
                                              pluginInterface.GetPluginConfigDirectory(),
                                              "MarketPriceCache.csv"),
                                          out _).GroupBy(c => c.WorldId).ToDictionary(c => c.Key, c => c.ToDictionary(d => (d.ItemId, d.IsHq), e => e));
        }
        catch (FileNotFoundException)
        {
        }
    }

    private void SaveCsvs()
    {
        var loadedConfiguration = this.GetConfiguration();
        csvLoaderService.ToCsv(
            loadedConfiguration.SaleItems.SelectMany(c => c.Value).ToList(),
            Path.Combine(pluginInterface.GetPluginConfigDirectory(), "SaleItems.csv"));
        csvLoaderService.ToCsv(
            loadedConfiguration.Sales.SelectMany(c => c.Value).ToList(),
            Path.Combine(pluginInterface.GetPluginConfigDirectory(), "SoldItems.csv"));
        csvLoaderService.ToCsv(
            loadedConfiguration.MarketPriceCache.SelectMany(c => c.Value.Select(d => d.Value)).ToList(),
            Path.Combine(pluginInterface.GetPluginConfigDirectory(), "MarketPriceCache.csv"));
    }
}
