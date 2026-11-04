using AbsenceService.Api.Domain;
using AbsenceService.Api.Repositories;
using AbsenceService.Api.Services;
using EmployeeAbsence.Shared;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sut = AbsenceService.Api.Services.AbsenceService;

namespace AbsenceService.Tests.Services;

public class AbsenceServiceTests
{
    private static readonly Guid EmployeeId = Guid.NewGuid();

    // 2026-11-04 01:30 UTC = 2026-11-04 08:30 in Asia/Jakarta (UTC+7)
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 4, 1, 30, 0, TimeSpan.Zero));
    private readonly Mock<IAbsenceRepository> _repository = new();
    private readonly Sut _sut;

    public AbsenceServiceTests()
    {
        _repository.Setup(r => r.GetForEmployeeOnDateAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _sut = new Sut(_repository.Object, _clock, Options.Create(new AttendanceSettings { TimeZone = "Asia/Jakarta", MaxRangeDays = 366 }));
    }

    private static Absence Record(string date, string time, string status) => new()
    {
        EmployeeId = EmployeeId, Date = DateOnly.Parse(date), Time = TimeOnly.Parse(time), Status = status,
    };

    [Fact]
    public async Task RecordAsync_CheckIn_UsesServerTimeInConfiguredTimeZone()
    {
        Absence? saved = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<Absence>(), It.IsAny<CancellationToken>()))
            .Callback<Absence, CancellationToken>((a, _) => saved = a);

        var result = await _sut.RecordAsync(EmployeeId, AttendanceStatus.CheckIn);

        Assert.NotNull(saved);
        Assert.Equal(new DateOnly(2026, 11, 4), result.Date);
        Assert.Equal(new TimeOnly(8, 30, 0), result.Time);
        Assert.Equal(EmployeeId, result.EmployeeId);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordAsync_LateNightUtc_IsRecordedOnNextLocalDay()
    {
        _clock.SetUtcNow(new DateTimeOffset(2026, 11, 4, 18, 0, 0, TimeSpan.Zero)); // 01:00 on 5 Nov in Jakarta

        var result = await _sut.RecordAsync(EmployeeId, AttendanceStatus.CheckIn);

        Assert.Equal(new DateOnly(2026, 11, 5), result.Date);
    }

    [Fact]
    public async Task RecordAsync_SecondCheckInSameDay_ThrowsConflict()
    {
        _repository.Setup(r => r.GetForEmployeeOnDateAsync(EmployeeId, new DateOnly(2026, 11, 4), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Record("2026-11-04", "08:00", AttendanceStatus.CheckIn)]);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _sut.RecordAsync(EmployeeId, AttendanceStatus.CheckIn));

        Assert.Equal("You have already checked in today.", ex.Message);
        _repository.Verify(r => r.AddAsync(It.IsAny<Absence>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordAsync_CheckOutWithoutCheckIn_ThrowsConflict()
    {
        var ex = await Assert.ThrowsAsync<ConflictException>(() => _sut.RecordAsync(EmployeeId, AttendanceStatus.CheckOut));
        Assert.Equal("You must check in before checking out.", ex.Message);
    }

    [Fact]
    public async Task RecordAsync_CheckOutAfterCheckIn_Succeeds()
    {
        _repository.Setup(r => r.GetForEmployeeOnDateAsync(EmployeeId, new DateOnly(2026, 11, 4), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Record("2026-11-04", "08:00", AttendanceStatus.CheckIn)]);

        var result = await _sut.RecordAsync(EmployeeId, AttendanceStatus.CheckOut);

        Assert.Equal(AttendanceStatus.CheckOut, result.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("izin")]
    [InlineData("MASUK")]
    public async Task RecordAsync_InvalidStatus_ThrowsValidation(string status)
    {
        await Assert.ThrowsAsync<BusinessValidationException>(() => _sut.RecordAsync(EmployeeId, status));
    }

    [Fact]
    public async Task GetSummaryAsync_GroupsCheckInAndCheckOutPerDay_SortedByDate()
    {
        _repository.Setup(r => r.GetForEmployeeAsync(EmployeeId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                Record("2026-11-03", "08:05", AttendanceStatus.CheckIn),
                Record("2026-11-02", "17:10", AttendanceStatus.CheckOut),
                Record("2026-11-02", "08:00", AttendanceStatus.CheckIn),
                Record("2026-11-03", "17:00", AttendanceStatus.CheckOut),
                Record("2026-11-04", "08:30", AttendanceStatus.CheckIn),
            ]);

        var summary = await _sut.GetSummaryAsync(EmployeeId, null, null);

        Assert.Equal(3, summary.Count);
        Assert.Equal(new DateOnly(2026, 11, 2), summary[0].Date);
        Assert.Equal(new TimeOnly(8, 0), summary[0].Masuk);
        Assert.Equal(new TimeOnly(17, 10), summary[0].Pulang);
        Assert.Null(summary[2].Pulang); // still at work today
    }

    [Fact]
    public async Task GetSummaryAsync_WithoutDates_DefaultsToStartOfMonthUntilToday()
    {
        _repository.Setup(r => r.GetForEmployeeAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.GetSummaryAsync(EmployeeId, null, null);

        _repository.Verify(r => r.GetForEmployeeAsync(EmployeeId, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 4), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetSummaryAsync_StartAfterEnd_ThrowsValidation()
    {
        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.GetSummaryAsync(EmployeeId, new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 1)));
    }

    [Fact]
    public async Task GetSummaryAsync_RangeTooLarge_ThrowsValidation()
    {
        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.GetSummaryAsync(EmployeeId, new DateOnly(2024, 1, 1), new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public async Task GetByIdAsync_OtherEmployeesRecord_ThrowsNotFoundForNonAdmin()
    {
        var record = Record("2026-11-04", "08:00", AttendanceStatus.CheckIn);
        _repository.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>())).ReturnsAsync(record);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(record.Id, Guid.NewGuid(), requesterIsAdmin: false));
        var asAdmin = await _sut.GetByIdAsync(record.Id, Guid.NewGuid(), requesterIsAdmin: true);
        Assert.Equal(record.Id, asAdmin.Id);
    }
}
