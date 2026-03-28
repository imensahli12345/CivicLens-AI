import { Participation } from './participation.model';

export interface Evenement {
  id: string; // Guid
  titre: string;
  description: string;
  dateEvenement: Date;
  lieu: string;
  delegation?: string;
  nombreVolontairesRequis: number;
  organisateurId: string; // Guid
  organisateurNom: string;
  nombreParticipations: number;
  participations?: Participation[];

  isConfirmed?: boolean;
  confirmedAt?: Date | null;
}

export interface CreateEvenementRequest {
  titre: string;
  description: string;
  dateEvenement: Date | string;
  lieu: string;
  nombreVolontairesRequis: number;
}
