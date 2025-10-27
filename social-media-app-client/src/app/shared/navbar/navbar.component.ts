import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { LucideAngularModule, Zap, Home, LogOut, Users, MessageCircle, Menu, X } from 'lucide-angular';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss'
})
export class NavbarComponent {
  currentUser$;
  mobileMenuOpen = signal(false);
  
  // Icons
  readonly Zap = Zap;
  readonly Home = Home;
  readonly LogOut = LogOut;
  readonly UsersIcon = Users;
  readonly MessageCircleIcon = MessageCircle;
  readonly Menu = Menu;
  readonly X = X;
  
  constructor(
    private authService: AuthService,
    private router: Router
  ) {
    this.currentUser$ = this.authService.currentUser$;
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.set(!this.mobileMenuOpen());
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
  }

  logout(): void {
    this.authService.logout();
    this.closeMobileMenu();
  }
}

