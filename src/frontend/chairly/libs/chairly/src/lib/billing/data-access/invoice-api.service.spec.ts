import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { API_BASE_URL } from '@org/shared-lib';

import { Invoice } from '../models';
import { InvoiceApiService } from './invoice-api.service';

describe('InvoiceApiService', () => {
  let service: InvoiceApiService;
  let httpTesting: HttpTestingController;

  const mockInvoice: Invoice = {
    id: 'inv-1',
    invoiceNumber: '2026-0001',
    invoiceDate: '2026-03-10',
    bookingId: 'booking-1',
    clientId: 'client-1',
    clientFullName: 'Jan de Vries',
    clientSnapshot: {
      fullName: 'Jan de Vries',
      email: 'jan@example.com',
      phone: '0612345678',
      address: 'Kerkstraat 1, Amsterdam',
    },
    staffMemberName: 'Anna de Vries',
    subTotalAmount: 50,
    totalVatAmount: 10.5,
    totalAmount: 60.5,
    status: 'Verzonden',
    lineItems: [],
    createdAtUtc: '2026-03-10T10:00:00Z',
    sentAtUtc: '2026-03-10T12:00:00Z',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: '/api' },
      ],
    });

    service = TestBed.inject(InvoiceApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should POST /api/invoices/{id}/send when sending an invoice', () => {
    service.sendInvoice('inv-1').subscribe((invoice) => {
      expect(invoice).toEqual(mockInvoice);
    });

    const req = httpTesting.expectOne('/api/invoices/inv-1/send');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush(mockInvoice);
  });
});
