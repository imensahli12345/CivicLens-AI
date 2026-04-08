export type SignalementStatut =
  | 'EN_ATTENTE'
  | 'OUVERTE'
  | 'ENVOYEE'
  | 'EN_COURS_DETUDE'
  | 'EN_COURS'
  | 'RESOLU'
  | 'REJETE';
export type SignalementCategorie =
  | 'ROUTE'
  | 'ECLAIRAGE'
  | 'EAU'
  | 'TRANSPORT'
  | 'ELECTRICITE'
  | 'INFRASTRUCTURE'
  | 'DECHETS'
  | 'AUTRE';

export interface Signalement {
  id: string;
  titre: string;
  description: string;
  categorie: SignalementCategorie;
  statut: SignalementStatut;
  adresse: string;
  latitude?: number;
  longitude?: number;
  region: string;
  photos?: string[];
  votes: number;
  aVote?: boolean;
  scoreAI?: number;
  gravite?: string;
  priorite?: string;
  imagesUrl?: string[];
  imageUrl?: string;
  citoyenId: string;
  citoyenNom?: string;
  createdAt: Date;
  updatedAt?: Date;
}

export interface CreateSignalementRequest {
  description: string;
  latitude: number;
  longitude: number;
  images?: File[];
}

export interface SignalementStats {
  total: number;
  enAttente: number;
  enCours: number;
  resolus: number;
  rejetes: number;
}
