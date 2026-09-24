import { PaymentStatusLabelPipe } from './payment-status-pipe';

describe('PaymentStatusPipePipe', () => {
  it('create an instance', () => {
    const pipe = new PaymentStatusLabelPipe();
    expect(pipe).toBeTruthy();
  });
});
