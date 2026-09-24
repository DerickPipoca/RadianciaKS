import { OrderStatusLabelPipe } from './order-status-pipe';

describe('OrderStatusPipePipe', () => {
  it('create an instance', () => {
    const pipe = new OrderStatusLabelPipe();
    expect(pipe).toBeTruthy();
  });
});
