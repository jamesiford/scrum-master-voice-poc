using FluentValidation;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Behaviors;
using Zava.Scrum.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Zava.Scrum.Application.Tests.Messaging;

public sealed class MediatorTests
{
    private sealed record Ping(string Text) : IQuery<string>;

    private sealed class PingHandler : IRequestHandler<Ping, Result<string>>
    {
        public Task<Result<string>> Handle(Ping request, CancellationToken ct) =>
            request.Text == "boom"
                ? throw new InvalidOperationException("boom")
                : Task.FromResult(Result<string>.Success("pong:" + request.Text));
    }

    private sealed class DuplicatePingHandler : IRequestHandler<Ping, Result<string>>
    {
        public Task<Result<string>> Handle(Ping request, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed record Happened(DateTimeOffset OccurredAt) : IDomainEvent;

    private sealed class Recorder
    {
        public List<string> Log { get; } = [];
    }

    private sealed class FirstHandler(Recorder recorder) : INotificationHandler<Happened>
    {
        public Task Handle(Happened domainEvent, CancellationToken ct)
        {
            recorder.Log.Add("first");
            throw new InvalidOperationException("handler failure");
        }
    }

    private sealed class SecondHandler(Recorder recorder) : INotificationHandler<Happened>
    {
        public Task Handle(Happened domainEvent, CancellationToken ct)
        {
            recorder.Log.Add("second");
            return Task.CompletedTask;
        }
    }

    private sealed class TraceBehavior(Recorder recorder, string name) : IPipelineBehavior<Ping, Result<string>>
    {
        public async Task<Result<string>> Handle(Ping request, Func<Task<Result<string>>> next, CancellationToken ct)
        {
            recorder.Log.Add(name + ":in");
            var response = await next();
            recorder.Log.Add(name + ":out");
            return response;
        }
    }

    private sealed class PingValidator : FluentValidation.AbstractValidator<Ping>
    {
        public PingValidator() => RuleFor(p => p.Text).NotEmpty();
    }

    private static ServiceProvider Build(Action<IServiceCollection>? configure, params Type[] types)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Recorder>();
        services.AddMediator(types);
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Send_RegisteredRequest_DispatchesToItsHandler()
    {
        using var sp = Build(null, typeof(Ping), typeof(PingHandler));

        var result = await sp.GetRequiredService<ISender>().Send(new Ping("a"));

        Assert.Equal("pong:a", result.Value);
    }

    [Fact]
    public async Task Send_WithBehaviors_RunsThemInRegistrationOrderOutermostFirst()
    {
        using var sp = Build(
            s =>
            {
                s.AddScoped<IPipelineBehavior<Ping, Result<string>>>(p => new TraceBehavior(p.GetRequiredService<Recorder>(), "A"));
                s.AddScoped<IPipelineBehavior<Ping, Result<string>>>(p => new TraceBehavior(p.GetRequiredService<Recorder>(), "B"));
            },
            typeof(Ping),
            typeof(PingHandler));

        await sp.GetRequiredService<ISender>().Send(new Ping("a"));

        Assert.Equal(["A:in", "B:in", "B:out", "A:out"], sp.GetRequiredService<Recorder>().Log);
    }

    [Fact]
    public async Task Send_ValidatorFails_ShortCircuitsWithInvalidResult()
    {
        using var sp = Build(
            s =>
            {
                s.AddScoped<FluentValidation.IValidator<Ping>, PingValidator>();
                s.AddScoped<IPipelineBehavior<Ping, Result<string>>, ValidationBehavior<Ping, Result<string>>>();
            },
            typeof(Ping),
            typeof(PingHandler));

        var result = await sp.GetRequiredService<ISender>().Send(new Ping(""));

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
    }

    [Fact]
    public async Task Send_HandlerThrows_MapsToUnexpectedError()
    {
        using var sp = Build(
            s => s.AddScoped<IPipelineBehavior<Ping, Result<string>>>(_ =>
                new ExceptionMappingBehavior<Ping, Result<string>>(NullLogger<ExceptionMappingBehavior<Ping, Result<string>>>.Instance)),
            typeof(Ping),
            typeof(PingHandler));

        var result = await sp.GetRequiredService<ISender>().Send(new Ping("boom"));

        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    [Fact]
    public async Task Publish_OneHandlerFails_OtherHandlersStillRun()
    {
        using var sp = Build(null, typeof(FirstHandler), typeof(SecondHandler));

        await sp.GetRequiredService<IPublisher>().Publish(new Happened(DateTimeOffset.UnixEpoch));

        Assert.Equal(["first", "second"], sp.GetRequiredService<Recorder>().Log);
    }

    [Fact]
    public void AddMediator_RequestWithoutHandler_FailsFast()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Build(null, typeof(Ping)));

        Assert.Contains("Ping", ex.Message);
    }

    [Fact]
    public void AddMediator_RequestWithTwoHandlers_FailsFast()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => Build(null, typeof(Ping), typeof(PingHandler), typeof(DuplicatePingHandler)));

        Assert.Contains("More than one handler", ex.Message);
    }
}
