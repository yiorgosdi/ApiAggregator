using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aggregator.Core;

public sealed record Item(
    string Title,
    DateTimeOffset CreatedAt,
    string? Url = null,
    string? Source = null,
    int? Score = null
);