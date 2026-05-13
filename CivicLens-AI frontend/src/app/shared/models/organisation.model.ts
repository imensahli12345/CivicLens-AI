import { Signalement } from './signalement.model';
import { User } from './user.model';

export type TypeOrganisation =
  | 'MINISTERE'
  | 'COLLECTIVITE_TERRITORIALE'
  | 'ENTREPRISE_PUBLIQUE'
  | 'ONG'
  | 'AUTRE';

export type Domaine =
  | 'ROUTE'
  | 'ECLAIRAGE'
  | 'EAU'
  | 'TRANSPORT'
  | 'ELECTRICITE'
  | 'INFRASTRUCTURE'
  | 'DECHETS'
  | 'AUTRE';

export interface Organisation extends User {
  nomOrganisation: string;
  typeOrganisation: TypeOrganisation;
  domaineOrganisation: Domaine;
  signalements: Signalement[];
}
