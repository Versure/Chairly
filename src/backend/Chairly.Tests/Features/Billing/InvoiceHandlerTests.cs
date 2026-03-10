using Chairly.Api.Features.Billing;
using Chairly.Api.Features.Billing.GenerateInvoice;
using Chairly.Api.Features.Billing.GetInvoice;
using Chairly.Api.Features.Billing.GetInvoicesList;
using Chairly.Api.Features.Billing.MarkInvoicePaid;
using Chairly.Api.Features.Billing.MarkInvoiceSent;
using Chairly.Api.Features.Billing.VoidInvoice;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;

namespace Chairly.Tests.Features.Billing;

public class InvoiceHandlerTests
{
    private static ChairlyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    private static Booking CreateCompletedBooking(ChairlyDbContext db)
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = "Jan",
            LastName = "de Vries",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Clients.Add(client);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            ClientId = client.Id,
            StaffMemberId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddHours(-2),
            EndTime = DateTimeOffset.UtcNow.AddHours(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-30),
#pragma warning disable MA0026
            CompletedBy = Guid.Empty,
#pragma warning restore MA0026
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            BookingServices =
            [
                new BookingService
                {
                    Id = Guid.NewGuid(),
                    ServiceId = Guid.NewGuid(),
                    ServiceName = "Herenknippen",
                    Duration = TimeSpan.FromMinutes(30),
                    Price = 25.00m,
                    SortOrder = 0,
                },
                new BookingService
                {
                    Id = Guid.NewGuid(),
                    ServiceId = Guid.NewGuid(),
                    ServiceName = "Baard trimmen",
                    Duration = TimeSpan.FromMinutes(15),
                    Price = 15.00m,
                    SortOrder = 1,
                },
            ],
        };
        db.Bookings.Add(booking);
        db.SaveChanges();
        return booking;
    }

    private static Invoice CreateTestInvoice(ChairlyDbContext db, Guid? clientId = null)
    {
        var resolvedClientId = clientId ?? Guid.NewGuid();
        if (clientId == null)
        {
            var client = new Client
            {
                Id = resolvedClientId,
                TenantId = TenantConstants.DefaultTenantId,
                FirstName = "Pieter",
                LastName = "Bakker",
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            db.Clients.Add(client);
        }

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = resolvedClientId,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0001",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 40.00m,
            LineItems =
            [
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Herenknippen",
                    Quantity = 1,
                    UnitPrice = 25.00m,
                    TotalPrice = 25.00m,
                    SortOrder = 0,
                },
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Baard trimmen",
                    Quantity = 1,
                    UnitPrice = 15.00m,
                    TotalPrice = 15.00m,
                    SortOrder = 1,
                },
            ],
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Invoices.Add(invoice);
        db.SaveChanges();
        return invoice;
    }

    // ── GenerateInvoice ──────────────────────────────────────────────

    [Fact]
    public async Task GenerateInvoiceHandler_HappyPath_CreatesInvoiceWithCorrectLineItems()
    {
        await using var db = CreateDbContext();
        var booking = CreateCompletedBooking(db);
        var handler = new GenerateInvoiceHandler(db);

        var result = await handler.Handle(new GenerateInvoiceCommand { BookingId = booking.Id });

        var response = result.AsT0;
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(40.00m, response.TotalAmount);
        Assert.Equal(2, response.LineItems.Count);
        Assert.Equal("Herenknippen", response.LineItems[0].Description);
        Assert.Equal("Baard trimmen", response.LineItems[1].Description);
        Assert.Equal("Concept", response.Status);
        Assert.Equal("Jan de Vries", response.ClientFullName);
        Assert.Equal(1, await db.Invoices.CountAsync());
    }

    [Fact]
    public async Task GenerateInvoiceHandler_InvoiceNumberIncrementsCorrectly()
    {
        await using var db = CreateDbContext();
        var booking1 = CreateCompletedBooking(db);
        var handler = new GenerateInvoiceHandler(db);

        var result1 = await handler.Handle(new GenerateInvoiceCommand { BookingId = booking1.Id });
        var response1 = result1.AsT0;

        var expectedYear = DateTime.UtcNow.Year;
        Assert.Equal($"{expectedYear}-0001", response1.InvoiceNumber);

        // Create a second booking and generate second invoice
        var booking2 = CreateCompletedBooking(db);
        var result2 = await handler.Handle(new GenerateInvoiceCommand { BookingId = booking2.Id });
        var response2 = result2.AsT0;

        Assert.Equal($"{expectedYear}-0002", response2.InvoiceNumber);
    }

    [Fact]
    public async Task GenerateInvoiceHandler_BookingNotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new GenerateInvoiceHandler(db);

        var result = await handler.Handle(new GenerateInvoiceCommand { BookingId = Guid.NewGuid() });

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task GenerateInvoiceHandler_BookingNotCompleted_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = "Jan",
            LastName = "de Vries",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Clients.Add(client);
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            ClientId = client.Id,
            StaffMemberId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            EndTime = DateTimeOffset.UtcNow.AddHours(2),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        var handler = new GenerateInvoiceHandler(db);

        var result = await handler.Handle(new GenerateInvoiceCommand { BookingId = booking.Id });

        Assert.True(result.IsT2);
        Assert.Equal("Boeking is niet afgerond", result.AsT2.Message);
    }

    [Fact]
    public async Task GenerateInvoiceHandler_InvoiceAlreadyExists_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var booking = CreateCompletedBooking(db);
        var handler = new GenerateInvoiceHandler(db);

        // Generate first invoice
        await handler.Handle(new GenerateInvoiceCommand { BookingId = booking.Id });

        // Try generating again
        var result = await handler.Handle(new GenerateInvoiceCommand { BookingId = booking.Id });

        Assert.True(result.IsT3);
    }

    // ── GetInvoicesList ──────────────────────────────────────────────

    [Fact]
    public async Task GetInvoicesListHandler_EmptyList_ReturnsEmptyCollection()
    {
        await using var db = CreateDbContext();
        var handler = new GetInvoicesListHandler(db);

        var result = await handler.Handle(new GetInvoicesListQuery());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetInvoicesListHandler_ReturnsCorrectStatusStrings()
    {
        await using var db = CreateDbContext();
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = "Anna",
            LastName = "Jansen",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Clients.Add(client);

        // Draft invoice
        db.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = client.Id,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0001",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 10.00m,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-4),
        });

        // Sent invoice
        db.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = client.Id,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0002",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 20.00m,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-3),
            SentAtUtc = DateTimeOffset.UtcNow,
        });

        // Paid invoice
        db.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = client.Id,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0003",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 30.00m,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            PaidAtUtc = DateTimeOffset.UtcNow,
        });

        // Voided invoice
        db.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = client.Id,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0004",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 40.00m,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            VoidedAtUtc = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync();
        var handler = new GetInvoicesListHandler(db);

        var result = (await handler.Handle(new GetInvoicesListQuery())).ToList();

        Assert.Equal(4, result.Count);
        // Ordered newest first
        Assert.Equal("Vervallen", result[0].Status);
        Assert.Equal("Betaald", result[1].Status);
        Assert.Equal("Verzonden", result[2].Status);
        Assert.Equal("Concept", result[3].Status);
    }

    [Fact]
    public async Task GetInvoicesListHandler_OrderedNewestFirst()
    {
        await using var db = CreateDbContext();
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = "Test",
            LastName = "User",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Clients.Add(client);

        db.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = client.Id,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0001",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 10.00m,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
        });

        db.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = client.Id,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0002",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 20.00m,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
        });

        await db.SaveChangesAsync();
        var handler = new GetInvoicesListHandler(db);

        var result = (await handler.Handle(new GetInvoicesListQuery())).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(20.00m, result[0].TotalAmount);
        Assert.Equal(10.00m, result[1].TotalAmount);
    }

    // ── GetInvoice ──────────────────────────────────────────────────

    [Fact]
    public async Task GetInvoiceHandler_HappyPath_ReturnsInvoiceWithLineItems()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        var handler = new GetInvoiceHandler(db);

        var result = await handler.Handle(new GetInvoiceQuery(invoice.Id));

        var response = result.AsT0;
        Assert.Equal(invoice.Id, response.Id);
        Assert.Equal(2, response.LineItems.Count);
        Assert.Equal("Pieter Bakker", response.ClientFullName);
    }

    [Fact]
    public async Task GetInvoiceHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new GetInvoiceHandler(db);

        var result = await handler.Handle(new GetInvoiceQuery(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    // ── MarkInvoiceSent ─────────────────────────────────────────────

    [Fact]
    public async Task MarkInvoiceSentHandler_HappyPath_SetsSentTimestamp()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        var handler = new MarkInvoiceSentHandler(db);

        var result = await handler.Handle(new MarkInvoiceSentCommand(invoice.Id));

        var response = result.AsT0;
        Assert.Equal("Verzonden", response.Status);
        Assert.NotNull(response.SentAtUtc);
    }

    [Fact]
    public async Task MarkInvoiceSentHandler_AlreadySent_ReturnsCurrentState()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        invoice.SentAtUtc = DateTimeOffset.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        var handler = new MarkInvoiceSentHandler(db);

        var result = await handler.Handle(new MarkInvoiceSentCommand(invoice.Id));

        var response = result.AsT0;
        Assert.Equal("Verzonden", response.Status);
    }

    [Fact]
    public async Task MarkInvoiceSentHandler_VoidedInvoice_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        invoice.VoidedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new MarkInvoiceSentHandler(db);

        var result = await handler.Handle(new MarkInvoiceSentCommand(invoice.Id));

        Assert.True(result.IsT2);
        Assert.Equal("Factuur kan niet als verzonden worden gemarkeerd", result.AsT2.Message);
    }

    [Fact]
    public async Task MarkInvoiceSentHandler_PaidInvoice_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        invoice.PaidAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new MarkInvoiceSentHandler(db);

        var result = await handler.Handle(new MarkInvoiceSentCommand(invoice.Id));

        Assert.True(result.IsT2);
        Assert.Equal("Factuur kan niet als verzonden worden gemarkeerd", result.AsT2.Message);
    }

    [Fact]
    public async Task MarkInvoiceSentHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new MarkInvoiceSentHandler(db);

        var result = await handler.Handle(new MarkInvoiceSentCommand(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    // ── MarkInvoicePaid ─────────────────────────────────────────────

    [Fact]
    public async Task MarkInvoicePaidHandler_HappyPath_SetsPaidTimestamp()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        var handler = new MarkInvoicePaidHandler(db);

        var result = await handler.Handle(new MarkInvoicePaidCommand(invoice.Id));

        var response = result.AsT0;
        Assert.Equal("Betaald", response.Status);
        Assert.NotNull(response.PaidAtUtc);
    }

    [Fact]
    public async Task MarkInvoicePaidHandler_AlreadyPaid_ReturnsCurrentState()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        invoice.PaidAtUtc = DateTimeOffset.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        var handler = new MarkInvoicePaidHandler(db);

        var result = await handler.Handle(new MarkInvoicePaidCommand(invoice.Id));

        var response = result.AsT0;
        Assert.Equal("Betaald", response.Status);
    }

    [Fact]
    public async Task MarkInvoicePaidHandler_VoidedInvoice_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        invoice.VoidedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new MarkInvoicePaidHandler(db);

        var result = await handler.Handle(new MarkInvoicePaidCommand(invoice.Id));

        Assert.True(result.IsT2);
        Assert.Equal("Vervallen factuur kan niet als betaald worden gemarkeerd", result.AsT2.Message);
    }

    [Fact]
    public async Task MarkInvoicePaidHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new MarkInvoicePaidHandler(db);

        var result = await handler.Handle(new MarkInvoicePaidCommand(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    // ── VoidInvoice ─────────────────────────────────────────────────

    [Fact]
    public async Task VoidInvoiceHandler_HappyPath_SetsVoidedTimestamp()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        var handler = new VoidInvoiceHandler(db);

        var result = await handler.Handle(new VoidInvoiceCommand(invoice.Id));

        var response = result.AsT0;
        Assert.Equal("Vervallen", response.Status);
        Assert.NotNull(response.VoidedAtUtc);
    }

    [Fact]
    public async Task VoidInvoiceHandler_PaidInvoice_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        invoice.PaidAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new VoidInvoiceHandler(db);

        var result = await handler.Handle(new VoidInvoiceCommand(invoice.Id));

        Assert.True(result.IsT2);
        Assert.Equal("Betaalde factuur kan niet vervallen worden verklaard", result.AsT2.Message);
    }

    [Fact]
    public async Task VoidInvoiceHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new VoidInvoiceHandler(db);

        var result = await handler.Handle(new VoidInvoiceCommand(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    // ── InvoiceMapper ───────────────────────────────────────────────

    [Fact]
    public void InvoiceMapper_DeriveStatus_ReturnsCorrectStatusForEachState()
    {
        var draft = new Invoice { CreatedAtUtc = DateTimeOffset.UtcNow };
        Assert.Equal("Concept", InvoiceMapper.DeriveStatus(draft));

        var sent = new Invoice { CreatedAtUtc = DateTimeOffset.UtcNow, SentAtUtc = DateTimeOffset.UtcNow };
        Assert.Equal("Verzonden", InvoiceMapper.DeriveStatus(sent));

        var paid = new Invoice { CreatedAtUtc = DateTimeOffset.UtcNow, PaidAtUtc = DateTimeOffset.UtcNow };
        Assert.Equal("Betaald", InvoiceMapper.DeriveStatus(paid));

        var voided = new Invoice { CreatedAtUtc = DateTimeOffset.UtcNow, VoidedAtUtc = DateTimeOffset.UtcNow };
        Assert.Equal("Vervallen", InvoiceMapper.DeriveStatus(voided));

        // Voided takes precedence over paid
        var voidedAndPaid = new Invoice { CreatedAtUtc = DateTimeOffset.UtcNow, PaidAtUtc = DateTimeOffset.UtcNow, VoidedAtUtc = DateTimeOffset.UtcNow };
        Assert.Equal("Vervallen", InvoiceMapper.DeriveStatus(voidedAndPaid));
    }
}
