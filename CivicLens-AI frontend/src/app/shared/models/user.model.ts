export interface User {
  id: string;
  nom?: string;
  prenom?: string;
  email: string;
  region?: string;
  delegation?: string;
  cin?: string;
  telephone?: string;
  role?: string;
  createdAt?: Date;
   photoUrl?: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  nom: string;
  prenom: string;
  email: string;
  password: string;
  region: string;
  delegation: string;
  cin: string;
}
