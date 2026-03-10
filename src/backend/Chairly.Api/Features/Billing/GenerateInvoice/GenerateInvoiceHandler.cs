using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Billing.GenerateInvoice;

internal sealed class GenerateInvoiceHandler(ChairlyDbContext db) : IRequestHandler<GenerateInvoiceCommand, OneOf<InvoiceResponse, NotFound, Unprocessable, Conflict>>
{
    private const decimal DefaultVatPercentage = 21.00m;

    public async Task<OneOf<InvoiceResponse, NotFound, Unprocessable, Conflict>> Handle(GenerateInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var booking = await db.Bookings
            .Include(b => b.BookingServices)
            .FirstOrDefaultAsync(b => b.Id == command.BookingId && b.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (booking is null)
        {
            return new NotFound();
        }

        if (booking.CompletedAtUtc == null)
        {
            return new Unprocessable("Boeking is niet afgerond");
        }

        var invoiceExists = await db.Invoices
            .AnyAsync(i => i.BookingId == command.BookingId && i.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (invoiceExists)
        {
            return new Conflict();
        }

        var invoice = await BuildInvoiceAsync(booking, cancellationToken).ConfigureAwait(false);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var clientFullName = await db.Clients
            .Where(c => c.Id == invoice.ClientId)
            .Select(c => c.FirstName + " " + c.LastName)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        return InvoiceMapper.ToResponse(invoice, clientFullName);
    }

    private async Task<Invoice> BuildInvoiceAsync(Booking booking, CancellationToken cancellationToken)
    {
        var invoiceNumber = await GenerateInvoiceNumberAsync(cancellationToken).ConfigureAwait(false);

        var lineItems = booking.BookingServices
            .OrderBy(bs => bs.SortOrder)
            .Select(bs =>
            {
                var totalPrice = bs.Price;
                var vatAmount = Math.Round(totalPrice * DefaultVatPercentage / 100m, 2, MidpointRounding.AwayFromZero);
                return new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = bs.ServiceName,
                    Quantity = 1,
                    UnitPrice = bs.Price,
                    TotalPrice = totalPrice,
                    VatPercentage = DefaultVatPercentage,
                    VatAmount = vatAmount,
                    SortOrder = bs.SortOrder,
                    IsManual = false,
                };
            })
            .ToList();

        var subTotalAmount = lineItems.Sum(li => li.TotalPrice);
        var totalVatAmount = lineItems.Sum(li => li.VatAmount);

        return new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            BookingId = booking.Id,
            ClientId = booking.ClientId,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SubTotalAmount = subTotalAmount,
            TotalVatAmount = totalVatAmount,
            TotalAmount = subTotalAmount + totalVatAmount,
            LineItems = lineItems,
            CreatedAtUtc = DateTimeOffset.UtcNow,
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
            CreatedBy = Guid.Empty,
#pragma warning restore MA0026
        };
    }

    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken cancellationToken)
    {
        var currentYear = DateTime.UtcNow.Year;
        var yearPrefix = $"{currentYear}-";

        var lastNumber = await db.Invoices
            .Where(i => i.TenantId == TenantConstants.DefaultTenantId && i.InvoiceNumber.StartsWith(yearPrefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var nextSequence = 1;
        if (lastNumber is not null)
        {
            var sequencePart = lastNumber[yearPrefix.Length..];
            if (int.TryParse(sequencePart, System.Globalization.CultureInfo.InvariantCulture, out var lastSequence))
            {
                nextSequence = lastSequence + 1;
            }
        }

        return $"{currentYear}-{nextSequence:D4}";
    }
}
#pragma warning restore CA1812
