import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { UiToastContainerComponent } from './shared/ui/toast/ui-toast-container.component';

@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive, UiToastContainerComponent],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {}
