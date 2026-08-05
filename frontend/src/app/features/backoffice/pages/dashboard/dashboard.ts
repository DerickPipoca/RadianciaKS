import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { OrderService } from '../../../../core/services/order-service';
import { DashboardMetrics } from '../../../../core/models/dashboard-metrics.model';
import { ChartConfiguration, ChartType } from 'chart.js';
import { ButtonComponent } from '../../../../shared/components/button-component/button-component';
import { LucideAngularModule, TriangleAlert, Landmark, Ticket, ShoppingCart } from 'lucide-angular';

@Component({
  selector: 'app-dashboard',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    BaseChartDirective,
    ButtonComponent,
    LucideAngularModule,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  readonly TriangleAlert = TriangleAlert;
  readonly Ticket = Ticket;
  readonly ShoppingCart = ShoppingCart;
  readonly Landmark = Landmark;

  private fb = inject(FormBuilder);
  private orderService = inject(OrderService);

  filterForm!: FormGroup;
  metrics: DashboardMetrics | null = null;
  loading = false;
  errorMessage = '';

  public chartType: ChartType = 'bar';
  public chartData: ChartConfiguration['data'] = {
    labels: [],
    datasets: [
      {
        label: 'Faturação por Hora (R$)',
        data: [],
        backgroundColor: '#3b82f6',
        borderRadius: 6,
      },
    ],
  };

  public chartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: true, position: 'top' },
    },
    scales: {
      y: { beginAtZero: true },
    },
  };

  ngOnInit(): void {
    this.initForm();
    this.applyTodayFilter();
  }

  private initForm(): void {
    this.filterForm = this.fb.group({
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
    });
  }

  private applyTodayFilter(): void {
    const todayStart = new Date();
    todayStart.setHours(0, 0, 0, 0);

    const todayEnd = new Date();
    todayEnd.setHours(23, 59, 59, 999);

    this.filterForm.setValue({
      startDate: this.formatDateForInput(todayStart),
      endDate: this.formatDateForInput(todayEnd),
    });

    this.loadDashboardData();
  }

  loadDashboardData(): void {
    if (this.filterForm.invalid) return;

    this.loading = true;
    this.errorMessage = '';

    const { startDate, endDate } = this.filterForm.value;
    const start = new Date(startDate);
    const end = new Date(endDate);

    this.orderService.getDashboardMetrics(start, end).subscribe({
      next: (data) => {
        this.metrics = data;
        this.updateChart(data);
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.errorMessage = 'Erro ao carregar os dados do painel gerencial.';
        this.loading = false;
      },
    });
  }

  private updateChart(data: DashboardMetrics): void {
    if (!data.salesChart || data.salesChart.length === 0) {
      this.chartData.labels = [];
      this.chartData.datasets[0].data = [];
      return;
    }

    this.chartData.labels = data.salesChart.map((item) => item.label);
    this.chartData.datasets[0].data = data.salesChart.map((item) => item.value);

    this.chartData = { ...this.chartData };
  }

  private formatDateForInput(date: Date): string {
    const pad = (num: number) => num.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
}
