import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../../../services/auth.service';
import { AuthStateService } from '../../../services/auth-state.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  loading$!: Observable<boolean>;
  error$!: Observable<string | null>;
  passwordVisible = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private authState: AuthStateService,
    private router: Router
  ) {
    this.loading$ = this.authState.loading$;
    this.error$ = this.authState.error$;
  }

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      Object.keys(this.loginForm.controls).forEach(key => {
        this.loginForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.authState.setLoading(true);
    this.authState.clearError();

    this.authService.login(this.loginForm.value).subscribe({
      next: () => {
        this.authState.setLoading(false);
        this.router.navigate(['/feed']);
      },
      error: (err) => {
        this.authState.setLoading(false);
        this.authState.setError('Invalid email or password');
      }
    });
  }

  togglePasswordVisibility(): void {
    this.passwordVisible = !this.passwordVisible;
  }

  get email() {
    return this.loginForm.get('email');
  }

  get password() {
    return this.loginForm.get('password');
  }
}

