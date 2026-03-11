using Chairly.Api.Features.Billing;
using Chairly.Api.Features.Billing.AddInvoiceLineItem;
using Chairly.Api.Features.Billing.GenerateInvoice;
using Chairly.Api.Features.Billing.GetInvoice;
using Chairly.Api.Features.Billing.GetInvoicesList;
using Chairly.Api.Features.Billing.MarkInvoicePaid;
using Chairly.Api.Features.Billing.MarkInvoiceSent;
using Chairly.Api.Features.Billing.RegenerateInvoice;
using Chairly.Api.Features.Billing.RemoveInvoiceLineItem;
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
            SubTotalAmount = 31.60m,
            TotalVatAmount = 8.40m,
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
                    VatPercentage = 21.00m,
                    VatAmount = 5.25m,
                    SortOrder = 0,
                    IsManual = false,
                },
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Baard trimmen",
                    Quantity = 1,
                    UnitPrice = 15.00m,
                    TotalPrice = 15.00m,
                    VatPercentage = 21.00m,
                    VatAmount = 3.15m,
                    SortOrder = 1,
                    IsManual = false,
                },
            ],
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Invoices.Add(invoice);
        db.SaveChanges();
        return invoice;
    }

    private static Invoice CreateTestInvoiceWithManualItem(ChairlyDbContext db)
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = "Pieter",
            LastName = "Bakker",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Clients.Add(client);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = client.Id,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0001",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SubTotalAmount = 39.50m,
            TotalVatAmount = 10.50m,
            TotalAmount = 50.00m,
            LineItems =
            [
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Herenknippen",
                    Quantity = 1,
                    UnitPrice = 25.00m,
                    TotalPrice = 25.00m,
                    VatPercentage = 21.00m,
                    VatAmount = 5.25m,
                    SortOrder = 0,
                    IsManual = false,
                },
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Baard trimmen",
                    Quantity = 1,
                    UnitPrice = 15.00m,
                    TotalPrice = 15.00m,
                    VatPercentage = 21.00m,
                    VatAmount = 3.15m,
                    SortOrder = 1,
                    IsManual = false,
                },
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Toeslag",
                    Quantity = 1,
                    UnitPrice = 10.00m,
                    TotalPrice = 10.00m,
                    VatPercentage = 21.00m,
                    VatAmount = 2.10m,
                    SortOrder = 2,
                    IsManual = true,
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
        var handler = new GenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));

        var result = await handler.Handle(new GenerateInvoiceCommand { BookingId = booking.Id });

        var response = result.AsT0;
        Assert.NotEqual(Guid.Empty, response.Id);
        // New formula: vatAmount = price * rate / 100. SubTotal = sum(TotalPrice) - TotalVat
        // 25*21/100 = 5.25, 15*21/100 = 3.15, totalVat = 8.40, subTotal = 40 - 8.40 = 31.60
        Assert.Equal(31.60m, response.SubTotalAmount);
        Assert.Equal(8.40m, response.TotalVatAmount);
        Assert.Equal(40.00m, response.TotalAmount);
        Assert.Equal(2, response.LineItems.Count);
        Assert.Equal("Herenknippen", response.LineItems[0].Description);
        Assert.Equal("Baard trimmen", response.LineItems[1].Description);
        Assert.Equal("Concept", response.Status);
        Assert.Equal("Jan de Vries", response.ClientFullName);
        Assert.Equal(1, await db.Invoices.CountAsync());
    }

    [Fact]
    public async Task GenerateInvoiceHandler_HappyPath_SetsVatFieldsOnLineItems()
    {
        await using var db = CreateDbContext();
        var booking = CreateCompletedBooking(db);
        var handler = new GenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));

        var result = await handler.Handle(new GenerateInvoiceCommand { BookingId = booking.Id });

        var response = result.AsT0;
        Assert.All(response.LineItems, li =>
        {
            Assert.Equal(21.00m, li.VatPercentage);
            Assert.False(li.IsManual);
        });
        // Formula: price * rate / 100. 25.00 * 21 / 100 = 5.25
        Assert.Equal(5.25m, response.LineItems[0].VatAmount);
        // 15.00 * 21 / 100 = 3.15
        Assert.Equal(3.15m, response.LineItems[1].VatAmount);
    }

    [Fact]
    public async Task GenerateInvoiceHandler_InvoiceNumberIncrementsCorrectly()
    {
        await using var db = CreateDbContext();
        var booking1 = CreateCompletedBooking(db);
        var handler = new GenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));

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
        var handler = new GenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));

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
        var handler = new GenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));

        var result = await handler.Handle(new GenerateInvoiceCommand { BookingId = booking.Id });

        Assert.True(result.IsT2);
        Assert.Equal("Boeking is niet afgerond", result.AsT2.Message);
    }

    [Fact]
    public async Task GenerateInvoiceHandler_InvoiceAlreadyExists_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var booking = CreateCompletedBooking(db);
        var handler = new GenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));

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
            SubTotalAmount = 10.00m,
            TotalVatAmount = 2.10m,
            TotalAmount = 12.10m,
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
            SubTotalAmount = 20.00m,
            TotalVatAmount = 4.20m,
            TotalAmount = 24.20m,
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
            SubTotalAmount = 30.00m,
            TotalVatAmount = 6.30m,
            TotalAmount = 36.30m,
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
            SubTotalAmount = 40.00m,
            TotalVatAmount = 8.40m,
            TotalAmount = 48.40m,
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
            SubTotalAmount = 10.00m,
            TotalVatAmount = 2.10m,
            TotalAmount = 12.10m,
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
            SubTotalAmount = 20.00m,
            TotalVatAmount = 4.20m,
            TotalAmount = 24.20m,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
        });

        await db.SaveChangesAsync();
        var handler = new GetInvoicesListHandler(db);

        var result = (await handler.Handle(new GetInvoicesListQuery())).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(24.20m, result[0].TotalAmount);
        Assert.Equal(12.10m, result[1].TotalAmount);
    }

    [Fact]
    public async Task GetInvoicesListHandler_FilterByClientName_ReturnsMatchingInvoices()
    {
        await using var db = CreateDbContext();
        var client1 = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = "Jan",
            LastName = "de Vries",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        var client2 = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = "Pieter",
            LastName = "Bakker",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Clients.AddRange(client1, client2);

        db.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = client1.Id,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0001",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SubTotalAmount = 10.00m,
            TotalVatAmount = 2.10m,
            TotalAmount = 12.10m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        db.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = client2.Id,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0002",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SubTotalAmount = 20.00m,
            TotalVatAmount = 4.20m,
            TotalAmount = 24.20m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync();
        var handler = new GetInvoicesListHandler(db);

        var result = (await handler.Handle(new GetInvoicesListQuery { ClientName = "vries" })).ToList();

        Assert.Single(result);
        Assert.Equal("Jan de Vries", result[0].ClientFullName);

        var resultUpper = (await handler.Handle(new GetInvoicesListQuery { ClientName = "VRIES" })).ToList();

        Assert.Single(resultUpper);
        Assert.Equal("Jan de Vries", resultUpper[0].ClientFullName);
    }

    [Fact]
    public async Task GetInvoicesListHandler_FilterByDateRange_ReturnsMatchingInvoices()
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
            InvoiceDate = new DateOnly(2026, 1, 15),
            SubTotalAmount = 10.00m,
            TotalVatAmount = 2.10m,
            TotalAmount = 12.10m,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
        });

        db.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = client.Id,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0002",
            InvoiceDate = new DateOnly(2026, 3, 10),
            SubTotalAmount = 20.00m,
            TotalVatAmount = 4.20m,
            TotalAmount = 24.20m,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
        });

        await db.SaveChangesAsync();
        var handler = new GetInvoicesListHandler(db);

        var result = (await handler.Handle(new GetInvoicesListQuery
        {
            FromDate = new DateOnly(2026, 3, 1),
            ToDate = new DateOnly(2026, 3, 31),
        })).ToList();

        Assert.Single(result);
        Assert.Equal(new DateOnly(2026, 3, 10), result[0].InvoiceDate);
    }

    [Fact]
    public async Task GetInvoicesListHandler_FilterByStatus_ReturnsMatchingInvoices()
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

        // Draft invoice
        db.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = client.Id,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0001",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SubTotalAmount = 10.00m,
            TotalVatAmount = 2.10m,
            TotalAmount = 12.10m,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
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
            SubTotalAmount = 20.00m,
            TotalVatAmount = 4.20m,
            TotalAmount = 24.20m,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            SentAtUtc = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync();
        var handler = new GetInvoicesListHandler(db);

        var draftResult = (await handler.Handle(new GetInvoicesListQuery { Status = "Concept" })).ToList();
        Assert.Single(draftResult);
        Assert.Equal("Concept", draftResult[0].Status);

        var sentResult = (await handler.Handle(new GetInvoicesListQuery { Status = "Verzonden" })).ToList();
        Assert.Single(sentResult);
        Assert.Equal("Verzonden", sentResult[0].Status);
    }

    [Fact]
    public async Task GetInvoicesListHandler_FilterByClientId_ReturnsMatchingInvoices()
    {
        await using var db = CreateDbContext();
        var client1 = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = "Jan",
            LastName = "de Vries",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        var client2 = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = "Pieter",
            LastName = "Bakker",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Clients.AddRange(client1, client2);

        db.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = client1.Id,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0001",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SubTotalAmount = 10.00m,
            TotalVatAmount = 2.10m,
            TotalAmount = 12.10m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        db.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = client2.Id,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0002",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SubTotalAmount = 20.00m,
            TotalVatAmount = 4.20m,
            TotalAmount = 24.20m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync();
        var handler = new GetInvoicesListHandler(db);

        var result = (await handler.Handle(new GetInvoicesListQuery { ClientId = client1.Id })).ToList();

        Assert.Single(result);
        Assert.Equal(client1.Id, result[0].ClientId);
        Assert.Equal("Jan de Vries", result[0].ClientFullName);
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
        Assert.Equal(31.60m, response.SubTotalAmount);
        Assert.Equal(8.40m, response.TotalVatAmount);
        Assert.Equal(40.00m, response.TotalAmount);
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
    public async Task MarkInvoiceSentHandler_ResendAfterEdit_SetsSentTimestampAgain()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);

        // First: send the invoice
        var sentHandler = new MarkInvoiceSentHandler(db);
        var sentResult = await sentHandler.Handle(new MarkInvoiceSentCommand(invoice.Id));
        Assert.Equal("Verzonden", sentResult.AsT0.Status);

        // Second: edit the sent invoice (adds line item, resets to draft)
        var addHandler = new AddInvoiceLineItemHandler(db);
        var addResult = await addHandler.Handle(new AddInvoiceLineItemCommand
        {
            InvoiceId = invoice.Id,
            Description = "Extra toeslag",
            Quantity = 1,
            UnitPrice = 5.00m,
        });
        Assert.Equal("Concept", addResult.AsT0.Status);
        Assert.Null(addResult.AsT0.SentAtUtc);

        // Third: re-send the invoice
        var resendResult = await sentHandler.Handle(new MarkInvoiceSentCommand(invoice.Id));
        Assert.Equal("Verzonden", resendResult.AsT0.Status);
        Assert.NotNull(resendResult.AsT0.SentAtUtc);
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

    // ── AddInvoiceLineItem ──────────────────────────────────────────

    [Fact]
    public async Task AddInvoiceLineItemHandler_HappyPath_AddsSurcharge()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        var handler = new AddInvoiceLineItemHandler(db);

        var command = new AddInvoiceLineItemCommand
        {
            InvoiceId = invoice.Id,
            Description = "Te laat annuleringskosten",
            Quantity = 1,
            UnitPrice = 10.00m,
            VatPercentage = 21.00m,
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal(3, response.LineItems.Count);
        var manualItem = response.LineItems.First(li => li.IsManual);
        Assert.Equal("Te laat annuleringskosten", manualItem.Description);
        Assert.Equal(10.00m, manualItem.TotalPrice);
        Assert.Equal(21.00m, manualItem.VatPercentage);
        Assert.Equal(2.10m, manualItem.VatAmount);
        Assert.True(manualItem.IsManual);

        // Totals recalculated: sum(TotalPrice)=50, totalVat=10.50, subTotal=50-10.50=39.50
        Assert.Equal(39.50m, response.SubTotalAmount);
        Assert.Equal(10.50m, response.TotalVatAmount);
        Assert.Equal(50.00m, response.TotalAmount);
    }

    [Fact]
    public async Task AddInvoiceLineItemHandler_HappyPath_AddsDiscount_WithZeroVat()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        var handler = new AddInvoiceLineItemHandler(db);

        var command = new AddInvoiceLineItemCommand
        {
            InvoiceId = invoice.Id,
            Description = "Loyaliteitskorting",
            Quantity = 1,
            UnitPrice = -5.00m,
            VatPercentage = 21.00m, // Should be forced to 0% for discounts
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal(3, response.LineItems.Count);
        var manualItem = response.LineItems.First(li => li.IsManual);
        Assert.Equal("Loyaliteitskorting", manualItem.Description);
        Assert.Equal(-5.00m, manualItem.TotalPrice);
        Assert.Equal(0m, manualItem.VatPercentage); // Discount = always 0% VAT
        Assert.Equal(0m, manualItem.VatAmount); // Discount = always 0 VAT amount

        // Totals: sum(TotalPrice)=35, totalVat=8.40, subTotal=35-8.40=26.60
        Assert.Equal(26.60m, response.SubTotalAmount);
        Assert.Equal(8.40m, response.TotalVatAmount);
        Assert.Equal(35.00m, response.TotalAmount);
    }

    [Fact]
    public async Task AddInvoiceLineItemHandler_CustomVatPercentage()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        var handler = new AddInvoiceLineItemHandler(db);

        var command = new AddInvoiceLineItemCommand
        {
            InvoiceId = invoice.Id,
            Description = "Producten (laag tarief)",
            Quantity = 2,
            UnitPrice = 10.00m,
            VatPercentage = 9.00m,
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        var manualItem = response.LineItems.First(li => li.IsManual);
        Assert.Equal(9.00m, manualItem.VatPercentage);
        Assert.Equal(20.00m, manualItem.TotalPrice); // 2 * 10
        Assert.Equal(1.80m, manualItem.VatAmount); // 20 * 9% = 1.80
    }

    [Fact]
    public async Task AddInvoiceLineItemHandler_SentInvoice_AllowsEditAndResetsToDraft()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        invoice.SentAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026
        invoice.SentBy = Guid.Empty;
#pragma warning restore MA0026
        await db.SaveChangesAsync();
        var handler = new AddInvoiceLineItemHandler(db);

        var command = new AddInvoiceLineItemCommand
        {
            InvoiceId = invoice.Id,
            Description = "Toeslag",
            Quantity = 1,
            UnitPrice = 5.00m,
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal(3, response.LineItems.Count);
        Assert.Equal("Concept", response.Status); // Reset to draft after editing
        Assert.Null(response.SentAtUtc); // SentAtUtc is cleared
    }

    [Fact]
    public async Task AddInvoiceLineItemHandler_PaidInvoice_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        invoice.PaidAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new AddInvoiceLineItemHandler(db);

        var command = new AddInvoiceLineItemCommand
        {
            InvoiceId = invoice.Id,
            Description = "Toeslag",
            Quantity = 1,
            UnitPrice = 5.00m,
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT2);
        Assert.Equal("Betaalde of vervallen facturen kunnen niet worden gewijzigd", result.AsT2.Message);
    }

    [Fact]
    public async Task AddInvoiceLineItemHandler_VoidedInvoice_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        invoice.VoidedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new AddInvoiceLineItemHandler(db);

        var command = new AddInvoiceLineItemCommand
        {
            InvoiceId = invoice.Id,
            Description = "Toeslag",
            Quantity = 1,
            UnitPrice = 5.00m,
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT2);
        Assert.Equal("Betaalde of vervallen facturen kunnen niet worden gewijzigd", result.AsT2.Message);
    }

    [Fact]
    public async Task AddInvoiceLineItemHandler_InvoiceNotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new AddInvoiceLineItemHandler(db);

        var command = new AddInvoiceLineItemCommand
        {
            InvoiceId = Guid.NewGuid(),
            Description = "Toeslag",
            Quantity = 1,
            UnitPrice = 5.00m,
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    // ── RemoveInvoiceLineItem ───────────────────────────────────────

    [Fact]
    public async Task RemoveInvoiceLineItemHandler_HappyPath_RemovesManualItem()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoiceWithManualItem(db);
        var manualItem = invoice.LineItems.First(li => li.IsManual);

        var handler = new RemoveInvoiceLineItemHandler(db);
        var result = await handler.Handle(new RemoveInvoiceLineItemCommand(invoice.Id, manualItem.Id));

        var response = result.AsT0;
        Assert.Equal(2, response.LineItems.Count);
        Assert.DoesNotContain(response.LineItems, li => li.Id == manualItem.Id);

        // Totals recalculated: sum(TotalPrice)=40, totalVat=8.40, subTotal=40-8.40=31.60
        Assert.Equal(31.60m, response.SubTotalAmount);
        Assert.Equal(8.40m, response.TotalVatAmount);
        Assert.Equal(40.00m, response.TotalAmount);
    }

    [Fact]
    public async Task RemoveInvoiceLineItemHandler_AutoGeneratedItem_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        var autoItem = invoice.LineItems.First();
        var handler = new RemoveInvoiceLineItemHandler(db);

        var result = await handler.Handle(new RemoveInvoiceLineItemCommand(invoice.Id, autoItem.Id));

        Assert.True(result.IsT2);
        Assert.Equal("Alleen handmatig toegevoegde regels kunnen worden verwijderd", result.AsT2.Message);
    }

    [Fact]
    public async Task RemoveInvoiceLineItemHandler_SentInvoice_AllowsRemoveAndResetsToDraft()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoiceWithManualItem(db);
        invoice.SentAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026
        invoice.SentBy = Guid.Empty;
#pragma warning restore MA0026
        await db.SaveChangesAsync();
        var manualItem = invoice.LineItems.First(li => li.IsManual);
        var handler = new RemoveInvoiceLineItemHandler(db);

        var result = await handler.Handle(new RemoveInvoiceLineItemCommand(invoice.Id, manualItem.Id));

        var response = result.AsT0;
        Assert.Equal(2, response.LineItems.Count);
        Assert.Equal("Concept", response.Status); // Reset to draft after editing
        Assert.Null(response.SentAtUtc); // SentAtUtc is cleared
    }

    [Fact]
    public async Task RemoveInvoiceLineItemHandler_PaidInvoice_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        invoice.PaidAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new RemoveInvoiceLineItemHandler(db);

        var result = await handler.Handle(new RemoveInvoiceLineItemCommand(invoice.Id, invoice.LineItems.First().Id));

        Assert.True(result.IsT2);
        Assert.Equal("Betaalde of vervallen facturen kunnen niet worden gewijzigd", result.AsT2.Message);
    }

    [Fact]
    public async Task RemoveInvoiceLineItemHandler_LineItemNotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        var handler = new RemoveInvoiceLineItemHandler(db);

        var result = await handler.Handle(new RemoveInvoiceLineItemCommand(invoice.Id, Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task RemoveInvoiceLineItemHandler_InvoiceNotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new RemoveInvoiceLineItemHandler(db);

        var result = await handler.Handle(new RemoveInvoiceLineItemCommand(Guid.NewGuid(), Guid.NewGuid()));

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

    [Fact]
    public void InvoiceMapper_RecalculateInvoiceTotals_CalculatesCorrectly()
    {
        var invoice = new Invoice
        {
            LineItems =
            [
                new InvoiceLineItem { TotalPrice = 25.00m, VatAmount = 5.25m },
                new InvoiceLineItem { TotalPrice = 15.00m, VatAmount = 3.15m },
                new InvoiceLineItem { TotalPrice = -5.00m, VatAmount = -1.05m },
            ],
        };

        InvoiceMapper.RecalculateInvoiceTotals(invoice);

        // SubTotal = sum(TotalPrice) - TotalVat = 35 - 7.35 = 27.65
        Assert.Equal(27.65m, invoice.SubTotalAmount);
        Assert.Equal(7.35m, invoice.TotalVatAmount);
        Assert.Equal(35.00m, invoice.TotalAmount);
    }

    [Fact]
    public async Task GenerateInvoiceHandler_ServiceWithVatRate_UsesServiceVatRate()
    {
        await using var db = CreateDbContext();
        var serviceId = Guid.NewGuid();
        var service = new Service
        {
            Id = serviceId,
            TenantId = TenantConstants.DefaultTenantId,
            Name = "Herenknippen",
            Duration = TimeSpan.FromMinutes(30),
            Price = 25.00m,
            VatRate = 9m,
            IsActive = true,
            SortOrder = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Services.Add(service);

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
                    ServiceId = serviceId,
                    ServiceName = "Herenknippen",
                    Duration = TimeSpan.FromMinutes(30),
                    Price = 25.00m,
                    SortOrder = 0,
                },
            ],
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var handler = new GenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));
        var result = await handler.Handle(new GenerateInvoiceCommand { BookingId = booking.Id });

        var response = result.AsT0;
        Assert.Equal(9m, response.LineItems[0].VatPercentage);
        // Formula: 25.00 * 9 / 100 = 2.25
        Assert.Equal(2.25m, response.LineItems[0].VatAmount);
    }

    [Fact]
    public async Task GenerateInvoiceHandler_ServiceWithNullVatRate_FallsBackToDefaultVatRate()
    {
        await using var db = CreateDbContext();
        var serviceId = Guid.NewGuid();
        var service = new Service
        {
            Id = serviceId,
            TenantId = TenantConstants.DefaultTenantId,
            Name = "Herenknippen",
            Duration = TimeSpan.FromMinutes(30),
            Price = 39.99m,
            VatRate = null,
            IsActive = true,
            SortOrder = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Services.Add(service);

        db.VatSettings.Add(new VatSettings
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            DefaultVatRate = 21m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = "Pieter",
            LastName = "Bakker",
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
                    ServiceId = serviceId,
                    ServiceName = "Herenknippen",
                    Duration = TimeSpan.FromMinutes(30),
                    Price = 39.99m,
                    SortOrder = 0,
                },
            ],
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var handler = new GenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));
        var result = await handler.Handle(new GenerateInvoiceCommand { BookingId = booking.Id });

        var response = result.AsT0;
        Assert.Equal(21m, response.LineItems[0].VatPercentage);
        // Formula: 39.99 * 21 / 100 = 8.3979 -> 8.40
        Assert.Equal(8.40m, response.LineItems[0].VatAmount);
    }

    [Fact]
    public async Task GenerateInvoiceHandler_NoVatSettings_AutoCreatesVatSettings()
    {
        await using var db = CreateDbContext();
        var booking = CreateCompletedBooking(db);
        var handler = new GenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));

        await handler.Handle(new GenerateInvoiceCommand { BookingId = booking.Id });

        var vatSettings = await db.VatSettings.SingleOrDefaultAsync();
        Assert.NotNull(vatSettings);
        Assert.Equal(21m, vatSettings.DefaultVatRate);
        Assert.Equal(TenantConstants.DefaultTenantId, vatSettings.TenantId);
    }

    [Fact]
    public async Task GenerateInvoiceHandler_InclVatFormula_CorrectCalculation()
    {
        await using var db = CreateDbContext();
        var serviceId = Guid.NewGuid();
        db.Services.Add(new Service
        {
            Id = serviceId,
            TenantId = TenantConstants.DefaultTenantId,
            Name = "Herenknippen",
            Duration = TimeSpan.FromMinutes(30),
            Price = 39.99m,
            VatRate = 21m,
            IsActive = true,
            SortOrder = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

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
                    ServiceId = serviceId,
                    ServiceName = "Herenknippen",
                    Duration = TimeSpan.FromMinutes(30),
                    Price = 39.99m,
                    SortOrder = 0,
                },
            ],
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var handler = new GenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));
        var result = await handler.Handle(new GenerateInvoiceCommand { BookingId = booking.Id });

        var response = result.AsT0;
        // price 39.99, rate 21 -> vatAmount = round(39.99 * 21 / 100, 2) = 8.40
        Assert.Equal(8.40m, response.LineItems[0].VatAmount);
        Assert.Equal(21m, response.LineItems[0].VatPercentage);
    }

    // ── RegenerateInvoice ─────────────────────────────────────────

    [Fact]
    public async Task RegenerateInvoiceHandler_Returns200WithUpdatedLineItems_WhenBookingServicesChanged()
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

        // Booking has 2 services: Herenknippen (30) + Baard trimmen (15)
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
                    Price = 30.00m,
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

        // Invoice was originally generated with DIFFERENT prices (old prices before booking was updated)
        // and has an extra manual line item that should be removed by regeneration
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = booking.Id,
            ClientId = client.Id,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0001",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SubTotalAmount = 50.00m,
            TotalVatAmount = 13.00m,
            TotalAmount = 63.00m,
            LineItems =
            [
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Herenknippen (oud tarief)",
                    Quantity = 1,
                    UnitPrice = 25.00m,
                    TotalPrice = 25.00m,
                    VatPercentage = 21.00m,
                    VatAmount = 5.25m,
                    SortOrder = 0,
                    IsManual = false,
                },
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Baard trimmen (oud tarief)",
                    Quantity = 1,
                    UnitPrice = 12.00m,
                    TotalPrice = 12.00m,
                    VatPercentage = 21.00m,
                    VatAmount = 2.52m,
                    SortOrder = 1,
                    IsManual = false,
                },
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Toeslag",
                    Quantity = 1,
                    UnitPrice = 10.00m,
                    TotalPrice = 10.00m,
                    VatPercentage = 21.00m,
                    VatAmount = 2.10m,
                    SortOrder = 2,
                    IsManual = true,
                },
            ],
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        // Regenerate — should replace all 3 old line items with 2 items from current booking
        var handler = new RegenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));
        var result = await handler.Handle(new RegenerateInvoiceCommand(invoice.Id));

        Assert.True(result.IsT0);
        var response = result.AsT0;
        Assert.Equal(2, response.LineItems.Count);
        Assert.Contains(response.LineItems, li => string.Equals(li.Description, "Herenknippen", StringComparison.Ordinal));
        Assert.Contains(response.LineItems, li => string.Equals(li.Description, "Baard trimmen", StringComparison.Ordinal));
        Assert.DoesNotContain(response.LineItems, li => string.Equals(li.Description, "Toeslag", StringComparison.Ordinal));

        // Verify totals: Herenknippen 30.00 + Baard trimmen 15.00 = 45.00 total price
        var expectedVatHerenknippen = Math.Round(30.00m * 21m / 100m, 2, MidpointRounding.AwayFromZero); // 6.30
        var expectedVatBaard = Math.Round(15.00m * 21m / 100m, 2, MidpointRounding.AwayFromZero); // 3.15
        var expectedTotalVat = expectedVatHerenknippen + expectedVatBaard; // 9.45
        Assert.Equal(expectedTotalVat, response.TotalVatAmount);
        Assert.Equal(45.00m - expectedTotalVat, response.SubTotalAmount);
        Assert.Equal(45.00m, response.TotalAmount);
    }

    [Fact]
    public async Task RegenerateInvoiceHandler_Returns422_WhenInvoiceIsVerzonden()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        invoice.SentAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026
        invoice.SentBy = Guid.Empty;
#pragma warning restore MA0026
        await db.SaveChangesAsync();

        var handler = new RegenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));
        var result = await handler.Handle(new RegenerateInvoiceCommand(invoice.Id));

        Assert.True(result.IsT2);
        Assert.Equal("Alleen concept-facturen kunnen opnieuw worden gegenereerd", result.AsT2.Message);
    }

    [Fact]
    public async Task RegenerateInvoiceHandler_Returns422_WhenInvoiceIsBetaald()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        invoice.PaidAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026
        invoice.PaidBy = Guid.Empty;
#pragma warning restore MA0026
        await db.SaveChangesAsync();

        var handler = new RegenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));
        var result = await handler.Handle(new RegenerateInvoiceCommand(invoice.Id));

        Assert.True(result.IsT2);
        Assert.Equal("Alleen concept-facturen kunnen opnieuw worden gegenereerd", result.AsT2.Message);
    }

    [Fact]
    public async Task RegenerateInvoiceHandler_Returns422_WhenInvoiceIsVervallen()
    {
        await using var db = CreateDbContext();
        var invoice = CreateTestInvoice(db);
        invoice.VoidedAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026
        invoice.VoidedBy = Guid.Empty;
#pragma warning restore MA0026
        await db.SaveChangesAsync();

        var handler = new RegenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));
        var result = await handler.Handle(new RegenerateInvoiceCommand(invoice.Id));

        Assert.True(result.IsT2);
        Assert.Equal("Alleen concept-facturen kunnen opnieuw worden gegenereerd", result.AsT2.Message);
    }

    [Fact]
    public async Task RegenerateInvoiceHandler_Returns404_WhenInvoiceNotFound()
    {
        await using var db = CreateDbContext();

        var handler = new RegenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));
        var result = await handler.Handle(new RegenerateInvoiceCommand(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task RegenerateInvoiceHandler_Returns422_WhenBookingIsNotCompleted()
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

        // Create booking WITHOUT CompletedAtUtc
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            ClientId = client.Id,
            StaffMemberId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddHours(-2),
            EndTime = DateTimeOffset.UtcNow.AddHours(-1),
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
            ],
        };
        db.Bookings.Add(booking);

        // Create a concept invoice linked to this booking
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = booking.Id,
            ClientId = client.Id,
            InvoiceNumber = $"{DateTime.UtcNow.Year}-0099",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SubTotalAmount = 20.66m,
            TotalVatAmount = 5.25m,
            TotalAmount = 25.00m,
            LineItems =
            [
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Herenknippen",
                    Quantity = 1,
                    UnitPrice = 25.00m,
                    TotalPrice = 25.00m,
                    VatPercentage = 21.00m,
                    VatAmount = 5.25m,
                    SortOrder = 0,
                    IsManual = false,
                },
            ],
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var handler = new RegenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));
        var result = await handler.Handle(new RegenerateInvoiceCommand(invoice.Id));

        Assert.True(result.IsT2);
        Assert.Equal("Boeking is niet afgerond", result.AsT2.Message);
    }

    [Fact]
    public async Task RegenerateInvoiceHandler_PreservesInvoiceNumberDateCreatedAtCreatedBy()
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
            ],
        };
        db.Bookings.Add(booking);

        var originalCreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-5);
        var originalInvoiceNumber = $"{DateTime.UtcNow.Year}-0042";
        var originalInvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = booking.Id,
            ClientId = client.Id,
            InvoiceNumber = originalInvoiceNumber,
            InvoiceDate = originalInvoiceDate,
            SubTotalAmount = 20.66m,
            TotalVatAmount = 5.25m,
            TotalAmount = 25.00m,
            LineItems =
            [
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Herenknippen",
                    Quantity = 1,
                    UnitPrice = 25.00m,
                    TotalPrice = 25.00m,
                    VatPercentage = 21.00m,
                    VatAmount = 5.25m,
                    SortOrder = 0,
                    IsManual = false,
                },
            ],
            CreatedAtUtc = originalCreatedAtUtc,
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        // Regenerate
        var handler = new RegenerateInvoiceHandler(db, new InvoiceLineItemBuilder(db));
        var result = await handler.Handle(new RegenerateInvoiceCommand(invoice.Id));

        Assert.True(result.IsT0);
        var response = result.AsT0;
        Assert.Equal(originalInvoiceNumber, response.InvoiceNumber);
        Assert.Equal(originalInvoiceDate, response.InvoiceDate);
        Assert.Equal(originalCreatedAtUtc, response.CreatedAtUtc);
    }
}
