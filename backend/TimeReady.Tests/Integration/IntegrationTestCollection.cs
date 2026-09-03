using Xunit;

namespace TimeReady.Tests.Integration;

/// <summary>
/// Integration tests share one <see cref="TimeReadyApiFactory"/> so the API host
/// boots once. Parallel fixtures each start Serilog's reloadable host logger,
/// which freezes on the first host and breaks every later fixture.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<TimeReadyApiFactory>;
