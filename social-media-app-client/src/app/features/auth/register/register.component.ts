import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../../../services/auth.service';
import { AuthStateService } from '../../../services/auth-state.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent implements OnInit {
  registerForm!: FormGroup;
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
    this.registerForm = this.fb.group({
      userName: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      displayName: ['', [Validators.required, Validators.minLength(2)]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      Object.keys(this.registerForm.controls).forEach(key => {
        this.registerForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.authState.setLoading(true);
    this.authState.clearError();

    this.authService.register(this.registerForm.value).subscribe({
      next: () => {
        this.authState.setLoading(false);
        this.router.navigate(['/feed']);
      },
      error: (err) => {
        this.authState.setLoading(false);
        this.authState.setError('User with this email or username already exists');
      }
    });
  }

  togglePasswordVisibility(): void {
    this.passwordVisible = !this.passwordVisible;
  }

  get userName() {
    return this.registerForm.get('userName');
  }

  get email() {
    return this.registerForm.get('email');
  }

  get displayName() {
    return this.registerForm.get('displayName');
  }

  get password() {
    return this.registerForm.get('password');
  }
}

