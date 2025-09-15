using Aggregator.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aggregator.Connectors.Abstractions;

public interface IOpenMeteoConnector
{
    Task<ConnectorResult<WeatherInfo>> GetAsync(double lat, double lon, CancellationToken ct);
}

