using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Microsoft.Extensions.DependencyInjection;
using MediatorImpl = Chairly.Api.Shared.Mediator.Mediator;
using ValidationException = Chairly.Api.Shared.Mediator.ValidationException;

// 'Shared' is a VB keyword — suppressed intentionally; namespace mirrors the Chairly.Api folder structure
#pragma warning disable CA1716
namespace Chairly.Tests.Shared.Mediator;
#pragma warning restore CA1716

public class MediatorTests
{
    private static readonly string[] BehaviorExecutionOrder =
        ["Before:Alpha", "Before:Beta", "After:Beta", "After:Alpha"];

    private static ServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider();
    }

    // --- Test doubles: simple request/response ---

    private sealed class EchoRequest : IRequest<string>
    {
        public string Message { get; set; } = string.Empty;
    }

    private sealed class EchoHandler : IRequestHandler<EchoRequest, string>
    {
        public Task<string> Handle(EchoRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(request.Message);
    }

    // --- Test doubles: request with Data Annotations ---

    private sealed class RequiredNameRequest : IRequest<string>
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class RequiredNameHandler : IRequestHandler<RequiredNameRequest, string>
    {
        public Task<string> Handle(RequiredNameRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult($"Hello, {request.Name}");
    }

    // --- Test doubles: pipeline behaviors with execution tracking ---

    private sealed class BehaviorLog
    {
        public List<string> Entries { get; } = [];
    }

    private sealed class AlphaBehavior : IPipelineBehavior<EchoRequest, string>
    {
        private readonly BehaviorLog _log;

        public AlphaBehavior(BehaviorLog log) => _log = log;

        public async Task<string> Handle(
            EchoRequest request,
            RequestHandlerDelegate<string> continuation,
            CancellationToken cancellationToken = default)
        {
            _log.Entries.Add("Before:Alpha");
            var result = await continuation();
            _log.Entries.Add("After:Alpha");
            return result;
        }
    }

    private sealed class BetaBehavior : IPipelineBehavior<EchoRequest, string>
    {
        private readonly BehaviorLog _log;

        public BetaBehavior(BehaviorLog log) => _log = log;

        public async Task<string> Handle(
            EchoRequest request,
            RequestHandlerDelegate<string> continuation,
            CancellationToken cancellationToken = default)
        {
            _log.Entries.Add("Before:Beta");
            var result = await continuation();
            _log.Entries.Add("After:Beta");
            return result;
        }
    }

    private sealed class RequiredNameTrackingBehavior : IPipelineBehavior<RequiredNameRequest, string>
    {
        private readonly BehaviorLog _log;

        public RequiredNameTrackingBehavior(BehaviorLog log) => _log = log;

        public async Task<string> Handle(
            RequiredNameRequest request,
            RequestHandlerDelegate<string> continuation,
            CancellationToken cancellationToken = default)
        {
            _log.Entries.Add("TrackingBefore");
            var result = await continuation();
            _log.Entries.Add("TrackingAfter");
            return result;
        }
    }

    // --- Tests: Mediator dispatching ---

    [Fact]
    public async Task Send_WithRegisteredHandler_ReturnsHandlerResult()
    {
        await using var sp = BuildProvider(services =>
        {
            services.AddScoped<IMediator, MediatorImpl>();
            services.AddSingleton<IRequestHandler<EchoRequest, string>>(new EchoHandler());
        });

        var mediator = sp.GetRequiredService<IMediator>();
        var result = await mediator.Send(new EchoRequest { Message = "Hello" });

        Assert.Equal("Hello", result);
    }

    [Fact]
    public async Task Send_WithNoHandlerRegistered_ThrowsInvalidOperationException()
    {
        await using var sp = BuildProvider(services =>
        {
            services.AddScoped<IMediator, MediatorImpl>();
            // No IRequestHandler<EchoRequest, string> registered
        });

        var mediator = sp.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Send(new EchoRequest { Message = "Hello" }));
    }

    // --- Tests: Pipeline behavior ordering ---

    [Fact]
    public async Task Send_WithPipelineBehaviors_ExecutesInRegistrationOrder()
    {
        var log = new BehaviorLog();
        await using var sp = BuildProvider(services =>
        {
            services.AddScoped<IMediator, MediatorImpl>();
            services.AddSingleton<IRequestHandler<EchoRequest, string>>(new EchoHandler());
            services.AddSingleton<IPipelineBehavior<EchoRequest, string>>(new AlphaBehavior(log));
            services.AddSingleton<IPipelineBehavior<EchoRequest, string>>(new BetaBehavior(log));
        });

        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Send(new EchoRequest { Message = "test" });

        // Alpha registered first = outermost behavior = runs Before first, After last
        Assert.Equal(BehaviorExecutionOrder, log.Entries.ToArray());
    }

    // --- Tests: ValidationBehavior ---

    [Fact]
    public async Task ValidationBehavior_InvalidRequest_ThrowsValidationException()
    {
        await using var sp = BuildProvider(services =>
        {
            services.AddScoped<IMediator, MediatorImpl>();
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddSingleton<IRequestHandler<RequiredNameRequest, string>>(new RequiredNameHandler());
        });

        var mediator = sp.GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => mediator.Send(new RequiredNameRequest { Name = string.Empty }));

        Assert.True(ex.Errors.ContainsKey(nameof(RequiredNameRequest.Name)));
    }

    [Fact]
    public async Task ValidationBehavior_ValidRequest_PassesThroughToHandler()
    {
        await using var sp = BuildProvider(services =>
        {
            services.AddScoped<IMediator, MediatorImpl>();
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddSingleton<IRequestHandler<RequiredNameRequest, string>>(new RequiredNameHandler());
        });

        var mediator = sp.GetRequiredService<IMediator>();
        var result = await mediator.Send(new RequiredNameRequest { Name = "Alice" });

        Assert.Equal("Hello, Alice", result);
    }

    // --- Tests: Multiple behaviors chained ---

    [Fact]
    public async Task MultipleBehaviors_ValidationFailure_PreventsSubsequentBehaviorExecution()
    {
        var log = new BehaviorLog();
        await using var sp = BuildProvider(services =>
        {
            services.AddScoped<IMediator, MediatorImpl>();
            // ValidationBehavior (open generic) registered first = outermost in pipeline
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddSingleton<IPipelineBehavior<RequiredNameRequest, string>>(new RequiredNameTrackingBehavior(log));
            services.AddSingleton<IRequestHandler<RequiredNameRequest, string>>(new RequiredNameHandler());
        });

        var mediator = sp.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ValidationException>(
            () => mediator.Send(new RequiredNameRequest { Name = string.Empty }));

        // TrackingBehavior never ran because validation short-circuited the pipeline
        Assert.Empty(log.Entries);
    }

    // --- Tests: AddMediator() DI registration via assembly scanning ---

    [Fact]
    public async Task AddMediator_RegistersIMediator_CanDispatchViaAssemblyScan()
    {
        // AddMediator() scans Chairly.Api assembly and registers all handlers + ValidationBehavior.
        // We add a test handler on top to verify the mediator is functional after full DI setup.
        await using var sp = BuildProvider(services =>
        {
            services.AddMediator();
            services.AddSingleton<IRequestHandler<EchoRequest, string>>(new EchoHandler());
        });

        var mediator = sp.GetRequiredService<IMediator>();
        var result = await mediator.Send(new EchoRequest { Message = "DI works" });

        Assert.Equal("DI works", result);
    }
}
