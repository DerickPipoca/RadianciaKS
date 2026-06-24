import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SignalrService } from './core/services/signalr-service';
import { HeaderComponent } from './shared/components/header/header';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HeaderComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit, OnDestroy {
  private signalRService = inject(SignalrService);

  protected readonly title = signal('RadianciaKS');

  ngOnDestroy(): void {
    this.signalRService.stopConnection();
  }
  ngOnInit(): void {
    this.signalRService.startConnection();
  }
}
