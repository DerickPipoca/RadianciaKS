import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth-service';
import { Router } from '@angular/router';
import { ButtonComponent } from '../../../shared/components/button-component/button-component';
import { InputComponent } from '../../../shared/components/input-component/input-component';

@Component({
  selector: 'app-login',
  imports: [CommonModule, FormsModule, ButtonComponent, InputComponent],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private authService = inject(AuthService);
  private router = inject(Router);

  credentials = { cpf: '', password: '' };
  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(false);

  onLogin(): void {
    if (!this.credentials.cpf || !this.credentials.password) {
      this.errorMessage.set('Por favor, preencha todos os campos.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.credentials).subscribe({
      next: (res) => {
        this.isLoading.set(false);

        this.router.navigate(['/home']);
      },
      error: (err) => {
        this.isLoading.set(false);

        this.errorMessage.set(err.error?.message || 'CPF ou senha incorretos.');
      },
    });
  }
}
