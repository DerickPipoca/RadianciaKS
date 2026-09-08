import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { OrderService } from '../../../../core/services/order-service';
import { DashboardMetrics } from '../../../../core/models/dashboard-metrics.model';
import { ChartConfiguration, ChartType } from 'chart.js';
import { ButtonComponent } from '../../../../shared/components/button-component/button-component';
import {
  LucideAngularModule,
  TriangleAlert,
  Landmark,
  Ticket,
  ShoppingCart,
  RefreshCcw,
  ArrowLeft,
  Calendar,
  Ban,
  TrendingUp,
  TrendingDown,
  Users,
  Percent,
  ChevronDown,
  ChevronRight,
} from 'lucide-angular';
import { CashShiftHistory } from '../../../../core/models/cash-shift.model';
import { CashShiftService } from '../../../../core/services/cash-shift-service';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, BaseChartDirective, ButtonComponent, LucideAngularModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  readonly Percent = Percent;
  readonly RefreshCcw = RefreshCcw;
  readonly TriangleAlert = TriangleAlert;
  readonly Ticket = Ticket;
  readonly ShoppingCart = ShoppingCart;
  readonly Landmark = Landmark;
  readonly ArrowLeft = ArrowLeft;
  readonly Calendar = Calendar;
  readonly Ban = Ban;
  readonly TrendingUp = TrendingUp;
  readonly TrendingDown = TrendingDown;
  readonly Users = Users;
  readonly ChevronDown = ChevronDown;
  readonly ChevronRight = ChevronRight;

  loading = false;
  errorMessage = '';

  private orderService = inject(OrderService);
  private cashShiftService = inject(CashShiftService);

  selectedShiftId: string | null = null;

  shiftHistory: CashShiftHistory[] = [];
  metrics: DashboardMetrics | null = null;

  public chartType: ChartType = 'line';

  expandedProducts: Set<string> = new Set<string>();

  public chartData: ChartConfiguration['data'] = {
    labels: [],
    datasets: [
      {
        label: 'Faturação (R$)',
        data: [],
        borderColor: '#7c5cdb',
        backgroundColor: 'transparent',
        pointBackgroundColor: '#7c5cdb',
        pointRadius: 4,
        borderWidth: 2,
        tension: 0,
      },
    ],
  };

  public chartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
    },
    scales: {
      y: {
        beginAtZero: true,
        grid: {
          color: '#f0f0f0',
        },
      },
      x: {
        grid: {
          display: false,
        },
      },
    },
  };

  ngOnInit(): void {
    this.loadShiftHistory();
  }

  loadShiftHistory() {
    this.loading = true;
    this.errorMessage = '';

    this.cashShiftService.getHistory().subscribe({
      next: (response: any) => {
        this.shiftHistory = response.data || response;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Erro ao carregar o histórico de caixas.';
        this.loading = false;
      },
    });
  }

  openDashboardDetailed(shiftId: string) {
    this.selectedShiftId = shiftId;
    this.loadDashboardData();
  }

  goBackToHistory() {
    this.selectedShiftId = null;
    this.metrics = null;
    this.loadShiftHistory();
  }

  toggleProductExpand(productName: string): void {
    if (this.expandedProducts.has(productName)) {
      this.expandedProducts.delete(productName);
    } else {
      this.expandedProducts.add(productName);
    }
  }

  countTotalModifiers(item: any): number {
    if (!item.modifierGroups) return 0;
    return item.modifierGroups.reduce((acc: number, g: any) => acc + (g.modifiers?.length || 0), 0);
  }

  hasModifiers(item: any): boolean {
    return !!item.modifierGroups && item.modifierGroups.length > 0;
  }

  loadDashboardData() {
    this.loading = true;
    this.errorMessage = '';

    this.orderService.getDashboardMetrics(this.selectedShiftId || undefined).subscribe({
      next: (data) => {
        if (!data.shiftStatus && data.totalOrders === 0) {
          this.errorMessage = 'Métricas vazias ou caixa não encontrado.';
          this.metrics = null;
        } else {
          this.metrics = data;
          this.updateChart(data);
        }
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Erro ao carregar as métricas do painel.';
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
    const points = data.salesChart.map((item) => ({
      date: new Date(item.label),
      value: item.value,
      rawLabel: item.label,
    }));
    const uniqueDays = new Set(
      points
        .filter((p) => !isNaN(p.date.getTime()))
        .map((p) => `${p.date.getFullYear()}-${p.date.getMonth()}-${p.date.getDate()}`),
    );
    const spansMultipleDays = uniqueDays.size > 1;
    this.chartData.labels = points.map((p) => {
      if (isNaN(p.date.getTime())) return p.rawLabel;
      const timeStr = p.date.toLocaleTimeString([], {
        hour: '2-digit',
        minute: '2-digit',
      });
      if (!spansMultipleDays) {
        return timeStr;
      }
      const dateStr = p.date.toLocaleDateString('pt-BR', {
        day: '2-digit',
        month: '2-digit',
      });
      return [timeStr, dateStr];
    });

    this.chartData.datasets[0].data = points.map((p) => p.value);
    this.chartData = { ...this.chartData };
  }
}
