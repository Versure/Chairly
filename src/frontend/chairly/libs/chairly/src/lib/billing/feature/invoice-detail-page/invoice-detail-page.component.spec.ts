import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';

import { InvoiceStore } from '../../data-access';
import { CompanyInfo, Invoice } from '../../models';
import { InvoiceDetailPageComponent } from './invoice-detail-page.component';

function createInvoice(status: Invoice['status']): Invoice {
  return {
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
    status,
    lineItems: [],
    createdAtUtc: '2026-03-10T10:00:00Z',
  };
}

describe('InvoiceDetailPageComponent', () => {
  let fixture: ComponentFixture<InvoiceDetailPageComponent>;

  const selectedInvoice = signal<Invoice | null>(createInvoice('Concept'));
  const companyInfo = signal<CompanyInfo | null>(null);
  const isLoading = signal(false);
  const error = signal<string | null>(null);

  const mockInvoiceStore = {
    selectedInvoice,
    companyInfo,
    isLoading,
    error,
    loadInvoice: vi.fn(),
    loadCompanyInfo: vi.fn(),
    sendInvoice: vi.fn(),
    markAsPaid: vi.fn(),
    voidInvoice: vi.fn(),
    addLineItem: vi.fn(),
    removeLineItem: vi.fn(),
    regenerateInvoice: vi.fn(),
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    selectedInvoice.set(createInvoice('Concept'));
    error.set(null);
    isLoading.set(false);

    await TestBed.configureTestingModule({
      imports: [InvoiceDetailPageComponent],
      providers: [
        { provide: InvoiceStore, useValue: mockInvoiceStore },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: vi.fn().mockReturnValue('inv-1'),
              },
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(InvoiceDetailPageComponent);
    fixture.detectChanges();
  });

  it('should render Factuur versturen for sendable invoices', () => {
    const sendButton = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (button) => button.textContent?.trim() === 'Factuur versturen',
    );

    expect(sendButton).toBeTruthy();
  });

  it('should hide Factuur versturen for non-sendable invoices', () => {
    selectedInvoice.set(createInvoice('Verzonden'));
    fixture.detectChanges();

    const sendButton = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (button) => button.textContent?.trim() === 'Factuur versturen',
    );

    expect(sendButton).toBeUndefined();
  });

  it('should call sendInvoice and show success feedback when send succeeds', () => {
    mockInvoiceStore.sendInvoice.mockImplementation(() => {
      selectedInvoice.set({
        ...createInvoice('Verzonden'),
        sentAtUtc: '2026-03-10T12:00:00Z',
      });
    });

    const sendButton = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (button) => button.textContent?.trim() === 'Factuur versturen',
    ) as HTMLButtonElement;

    sendButton.click();
    fixture.detectChanges();

    expect(mockInvoiceStore.sendInvoice).toHaveBeenCalledExactlyOnceWith('inv-1');
    expect(fixture.nativeElement.textContent).toContain('Factuur is succesvol verzonden.');
  });

  it('should show Dutch API error message when send fails', () => {
    mockInvoiceStore.sendInvoice.mockImplementation(() => {
      error.set('Factuur kan niet worden verzonden zonder e-mailadres');
    });

    const sendButton = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (button) => button.textContent?.trim() === 'Factuur versturen',
    ) as HTMLButtonElement;

    sendButton.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(
      'Factuur kan niet worden verzonden zonder e-mailadres',
    );
  });
});
