import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { ConfirmareDialog, DateConfirmare } from '../../shared/confirmare/confirmare-dialog';
import { ModificariNesalvateService } from './modificari-nesalvate.service';

export const ghidModificariNesalvate: CanDeactivateFn<unknown> = async () => {
  const serviciu = inject(ModificariNesalvateService);
  if (!serviciu.areModificariNesalvate()) return true;

  const dialog = inject(MatDialog);
  const date: DateConfirmare = {
    titlu: 'Modificări nesalvate',
    mesaj: 'Există modificări nesalvate. Dacă părăsești pagina acum, vor fi pierdute.',
    etichetaConfirmare: 'Părăsește pagina',
    periculos: true
  };

  const rezultat = await firstValueFrom(
    dialog.open(ConfirmareDialog, { data: date, width: '460px', maxWidth: '95vw' })
      .afterClosed());
  return rezultat === true;
};