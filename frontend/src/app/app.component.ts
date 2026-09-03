import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { AuthService } from './core/auth/auth.service';

interface NavLink {
  path: string;
  label: string;
  adminOnly: boolean;
}

@Component({
  selector: 'tr-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatToolbarModule
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Tracks the mobile nav menu open state for aria-expanded. */
  protected readonly mobileNavOpen = signal(false);

  private readonly allLinks: NavLink[] = [
    { path: '/dashboard', label: 'Overview', adminOnly: false },
    { path: '/employees', label: 'Employees', adminOnly: false },
    { path: '/notifications', label: 'Notifications', adminOnly: false },
    { path: '/audit', label: 'Audit', adminOnly: true }
  ];

  /** Links the signed-in user may actually open. */
  protected readonly links = computed(() =>
    this.allLinks.filter(link => !link.adminOnly || this.auth.isAdmin())
  );

  protected readonly initials = computed(() => {
    const name = this.auth.user()?.fullName ?? '';

    return name
      .split(' ')
      .filter(part => part.length > 0)
      .slice(0, 2)
      .map(part => part[0]?.toUpperCase())
      .join('');
  });

  protected logout(): void {
    this.auth.logout().subscribe(() => void this.router.navigate(['/login']));
  }
}
