export interface Participation {
  id: string; // Guid
  dateParticipation: Date;
  commentaire: string;
  presenceConfirmee: boolean;
  citoyenId: string; // Guid
  citoyenNom: string;
  evenementId: string; // Guid
}

export interface CreateParticipationRequest {
  commentaire?: string;
}
