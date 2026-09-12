import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterOutlet } from '@angular/router';
import { APP_CONFIG } from './app-config';

type ApiStatus = 'verificando' | 'online' | 'indisponivel';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App implements OnInit {
  private readonly http = inject(HttpClient);
  protected readonly config = inject(APP_CONFIG);

  protected readonly title = signal('MeuKanBan');
  protected readonly apiStatus = signal<ApiStatus>('verificando');

  ngOnInit(): void {
    // Smoke check de conectividade usando exclusivamente a URL configurada
    // em runtime (assets/config.json), sem valor embutido em build.
    this.http.get(`${this.config.apiUrl}/health/live`, { responseType: 'text' }).subscribe({
      next: () => this.apiStatus.set('online'),
      error: () => this.apiStatus.set('indisponivel'),
    });
  }
}
