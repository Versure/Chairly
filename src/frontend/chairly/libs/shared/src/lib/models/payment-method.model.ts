export type PaymentMethod = 'Cash' | 'Pin' | 'BankTransfer';

export const paymentMethodLabels: Record<PaymentMethod, string> = {
  Cash: 'Contant',
  Pin: 'Pin',
  BankTransfer: 'Overboeking',
};
