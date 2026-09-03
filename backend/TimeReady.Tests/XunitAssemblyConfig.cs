using Xunit;

// Integration tests boot a real API host through WebApplicationFactory. xUnit's
// default parallel collections each spin up their own host, which freezes
// Serilog's reloadable logger on the first boot and breaks the rest.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
