import { Injectable } from '@angular/core';

export interface ProprietarStareModificari {
  readonly id: string;
  areModificariNesalvate(): boolean;
}

@Injectable({ providedIn: 'root' })
export class ModificariNesalvateService {
  private readonly proprietari = new Map<string, ProprietarStareModificari>();

  inregistreaza(proprietar: ProprietarStareModificari): void {
    this.proprietari.set(proprietar.id, proprietar);
  }

  retragere(id: string): void {
    this.proprietari.delete(id);
  }

  areModificariNesalvate(): boolean {
    for (const p of this.proprietari.values()) {
      if (p.areModificariNesalvate()) return true;
    }
    return false;
  }
}