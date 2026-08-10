import { Component } from '@angular/core';
import { LucideAngularModule, Rows3, History, Funnel } from 'lucide-angular';
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
}
