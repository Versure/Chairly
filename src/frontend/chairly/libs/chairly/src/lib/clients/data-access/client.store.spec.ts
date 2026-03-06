import { TestBed } from '@angular/core/testing';

import { of, throwError } from 'rxjs';

import { ClientResponse } from '../models';
import { ClientStore } from './client.store';
import { ClientApiService } from './client-api.service';

describe('ClientStore', () => {
  const mockClient: ClientResponse = {
    id: 'client-1',
    firstName: 'Anna',
    lastName: 'Bakker',
    email: 'anna@example.com',
    phoneNumber: null,
    notes: null,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: null,
  };

  const mockApiService = {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  };

  let store: InstanceType<typeof ClientStore>;

  beforeEach(() => {
    vi.clearAllMocks();

    TestBed.configureTestingModule({
      providers: [
        ClientStore,
        { provide: ClientApiService, useValue: mockApiService },
      ],
    });

    store = TestBed.inject(ClientStore);
  });

  it('should initialize with empty state, not loading, no error', () => {
    expect(store.clients()).toEqual([]);
    expect(store.isLoading()).toBe(false);
    expect(store.error()).toBeNull();
  });

  describe('loadAll()', () => {
    it('should set clients and clear isLoading on success', () => {
      mockApiService.getAll.mockReturnValue(of([mockClient]));

      store.loadAll();

      expect(store.clients()).toEqual([mockClient]);
      expect(store.isLoading()).toBe(false);
      expect(store.error()).toBeNull();
    });

    it('should set error and clear isLoading on failure', () => {
      const error = new Error('Network error');
      mockApiService.getAll.mockReturnValue(throwError(() => error));

      store.loadAll();

      expect(store.error()).toBe('Network error');
      expect(store.isLoading()).toBe(false);
    });
  });

  describe('addClient()', () => {
    it('should append client to clients array', () => {
      const newClient: ClientResponse = {
        ...mockClient,
        id: 'client-2',
        firstName: 'Bert',
        lastName: 'Claassen',
      };

      store.addClient(newClient);

      expect(store.clients()).toEqual([newClient]);
    });

    it('should append to existing clients', () => {
      mockApiService.getAll.mockReturnValue(of([mockClient]));
      store.loadAll();

      const newClient: ClientResponse = {
        ...mockClient,
        id: 'client-2',
        firstName: 'Bert',
      };
      store.addClient(newClient);

      expect(store.clients()).toHaveLength(2);
      expect(store.clients()[1]).toEqual(newClient);
    });
  });

  describe('updateClient()', () => {
    it('should replace client by id', () => {
      mockApiService.getAll.mockReturnValue(of([mockClient]));
      store.loadAll();

      const updated: ClientResponse = { ...mockClient, firstName: 'Anne' };
      store.updateClient(updated);

      expect(store.clients()).toEqual([updated]);
    });

    it('should not affect other clients', () => {
      const client2: ClientResponse = { ...mockClient, id: 'client-2', firstName: 'Bert' };
      mockApiService.getAll.mockReturnValue(of([mockClient, client2]));
      store.loadAll();

      const updated: ClientResponse = { ...mockClient, firstName: 'Anne' };
      store.updateClient(updated);

      expect(store.clients()[0].firstName).toBe('Anne');
      expect(store.clients()[1].firstName).toBe('Bert');
    });
  });

  describe('removeClient()', () => {
    it('should filter out client with matching id', () => {
      mockApiService.getAll.mockReturnValue(of([mockClient]));
      store.loadAll();

      store.removeClient(mockClient.id);

      expect(store.clients()).toEqual([]);
    });

    it('should not affect other clients', () => {
      const client2: ClientResponse = { ...mockClient, id: 'client-2', firstName: 'Bert' };
      mockApiService.getAll.mockReturnValue(of([mockClient, client2]));
      store.loadAll();

      store.removeClient(mockClient.id);

      expect(store.clients()).toHaveLength(1);
      expect(store.clients()[0].id).toBe('client-2');
    });
  });
});
