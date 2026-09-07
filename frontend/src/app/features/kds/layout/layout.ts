import { Component } from '@angular/core';
import { LucideAngularModule, Rows3, History, Funnel, Menu } from 'lucide-angular';
import { RouterLink, RouterModule, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-layout',
  imports: [LucideAngularModule, RouterOutlet, RouterModule, RouterLink],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
})
export class Layout {
  readonly Rows3 = Rows3;
  readonly Funnel = Funnel;
  readonly History = History;
  readonly Menu = Menu;

  isSidebarOpen = false;

  toggleSidebar(): void {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  closeSidebar(): void {
    this.isSidebarOpen = false;
  }
}
