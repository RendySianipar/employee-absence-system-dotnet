using System.Text;
using System.Text.Json;
using EmployeeAbsence.Shared.Messaging;
using LoggingService.Worker.Data;
using LoggingService.Worker.Domain;
using LoggingService.Worker.Messaging;
using LoggingService.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace LoggingService.Tests;

public class ProfileUpdatedMessageHandlerTests
{
    private readonly Mock<IActivityLogService> _logService = new();
    private readonly ProfileUpdatedMessageHandler _sut;

    public ProfileUpdatedMessageHandlerTests()
    {
        _sut = new ProfileUpdatedMessageHandler(_logService.Object, NullLogger<ProfileUpdatedMessageHandler>.Instance);
    }

    private static ReadOnlyMemory<byte> Json(string json) => Encoding.UTF8.GetBytes(json);

    [Fact]
    public async Task ValidMessage_IsLogged_AndReturnsSuccess()
    {
        var employeeId = Guid.NewGuid();
        var body = MessageSerializer.Serialize(new EmployeeProfileUpdatedEvent(
            employeeId, "Siti", "PROFILE_UPDATED", new Dictionary<string, string?> { ["phoneNumber"] = "0899" }, DateTimeOffset.UtcNow));

        var result = await _sut.HandleAsync(body);

        Assert.Equal(HandleResult.Success, result);
        _logService.Verify(s => s.LogProfileUpdatedAsync(
            It.Is<EmployeeProfileUpdatedEvent>(e => e.EmployeeId == employeeId && e.ChangedFields["phoneNumber"] == "0899"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"action\":\"PROFILE_UPDATED\"}")]
    [InlineData("null")]
    public async Task InvalidMessage_IsDiscarded_WithoutWritingLog(string json)
    {
        var result = await _sut.HandleAsync(Json(json));

        Assert.Equal(HandleResult.Discard, result);
        _logService.Verify(s => s.LogProfileUpdatedAsync(It.IsAny<EmployeeProfileUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DatabaseFailure_Bubbles_SoConsumerCanRequeue()
    {
        _logService.Setup(s => s.LogProfileUpdatedAsync(It.IsAny<EmployeeProfileUpdatedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("db down"));
        var body = MessageSerializer.Serialize(new EmployeeProfileUpdatedEvent(
            Guid.NewGuid(), "Siti", "PROFILE_UPDATED", new Dictionary<string, string?>(), DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => _sut.HandleAsync(body));
    }
}

public class ActivityLogServiceTests
{
    [Fact]
    public async Task LogProfileUpdatedAsync_PersistsChangedFieldsAsJson()
    {
        var options = new DbContextOptionsBuilder<LogsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new LogsDbContext(options);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 4, 3, 0, 0, TimeSpan.Zero));
        var sut = new ActivityLogService(db, clock);
        var occurredAt = clock.GetUtcNow().AddSeconds(-2);

        await sut.LogProfileUpdatedAsync(new EmployeeProfileUpdatedEvent(
            Guid.NewGuid(), "Siti", "PROFILE_UPDATED", new Dictionary<string, string?> { ["password"] = "********" }, occurredAt));

        var log = Assert.Single(db.ActivityLogs);
        Assert.Equal("PROFILE_UPDATED", log.Action);
        Assert.Equal(occurredAt, log.Timestamp);
        Assert.Equal(clock.GetUtcNow(), log.LoggedAt);
        var fields = JsonSerializer.Deserialize<Dictionary<string, string>>(log.ChangedFields!);
        Assert.Equal("********", fields!["password"]);
    }
}
