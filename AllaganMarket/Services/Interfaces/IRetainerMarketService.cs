// Copyright (c) PlaceholderCompany. All rights reserved.

using System;

using AllaganMarket.Models;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public interface IRetainerMarketService : IHostedService, IDisposable
{
    /// <summary>
    /// A item was added to your market listings for the active retainer
    /// </summary>
    event RetainerMarketService.ItemEventDelegate? OnItemAdded;
    
    /// <summary>
    /// A item was removed from your market listings for the active retainer
    /// </summary>
    event RetainerMarketService.ItemEventDelegate? OnItemRemoved;
    
    /// <summary>
    /// A item was updated on your market listings for the active retainer
    /// </summary>
    event RetainerMarketService.ItemEventDelegate? OnItemUpdated;
    
    /// <summary>
    /// Any event happened
    /// </summary>
    event RetainerMarketService.UpdatedEventDelegate? OnUpdated;
    
    /// <summary>
    /// The retainer window was opened
    /// </summary>
    event RetainerMarketService.MarketEventDelegate? OnOpened;
    
    /// <summary>
    /// The retainer window was closed
    /// </summary>
    event RetainerMarketService.MarketEventDelegate? OnClosed;
    
    bool InBadState { get; }
    
    SaleItem?[] SaleItems { get; }
}
