import {
  CanalTransmiterePrefect, RolSemnatarDispozitie, StatusActRedactional, TipDispozitie
} from '../../shared/enums';
import { ComunicareDispozitiePrefect, DispozitieDetalii, SemnatarDispozitie } from './dispozitii.models';
import { actiuniPermise, semnatariCompletiDispozitie } from './dispozitii.permisiuni';

function semnatar(rol: RolSemnatarDispozitie, id = 1): SemnatarDispozitie {
  return {
    id, rolSemnatar: rol, persoanaId: 1, consilierId: null,
    nume: 'Test', dataSemnare: '2026-01-01', ordineAfisare: id
  };
}

function comunicare(): ComunicareDispozitiePrefect {
  return {
    id: 1, dispozitieId: 1, numarOrdineInRegistru: 1, anRegistru: 2026,
    dataTrimiteri: '2026-01-05', dataInregistrareInRegistru: '2026-01-05',
    canalTransmitere: CanalTransmiterePrefect.EmailOficial,
    nrInregistrarePrefect: null, dataConfirmarePrefect: null, obiectiiMotivate: null,
    raspunsPrefect: null, dataRaspunsPrefect: null, observatiiInterne: null,
    creatLa: '2026-01-05T00:00:00Z'
  };
}

function dispozitie(over: Partial<DispozitieDetalii> = {}): DispozitieDetalii {
  return {
    id: 1, numar: null, anNumerotare: null, tipDispozitie: TipDispozitie.Normativ,
    titlu: 'Test', continut: null, dataEmitere: '2026-01-01T12:00:00Z',
    dataIntrareInVigoare: null, status: StatusActRedactional.Draft,
    estePublicat: false, dataPublicareMol: null, aIntratInCircuit: false,
    esteSemnat: false, numeFisierSemnat: null, marimeSemnat: null, dataIncarcareSemnat: null,
    contrasemnaturaRefuzata: false, obiectieLegalitateSecretar: null, dataRefuzContrasemnare: null,
    dataInvalidare: null, motivInvalidare: null, motivInvalidareEticheta: null,
    refInvalidare: null, motivInvalidareAltulText: null,
    sedintaId: null, institutieId: 1, creatLa: '2026-01-01T12:00:00Z',
    semnatari: [
      semnatar(RolSemnatarDispozitie.Emitent, 1),
      semnatar(RolSemnatarDispozitie.SecretarContrasemnatura, 2)
    ],
    comunicari: [],
    ...over
  };
}

describe('semnatariCompletiDispozitie', () => {
  it('true cu emitent + secretar contrasemnătură', () => {
    expect(semnatariCompletiDispozitie(dispozitie())).toBe(true);
  });

  it('true pe calea de refuz: emitent + refuz motivat, fără rând de secretar', () => {
    const d = dispozitie({
      semnatari: [semnatar(RolSemnatarDispozitie.Emitent, 1)],  // rândul de secretar soft-șters la refuz
      contrasemnaturaRefuzata: true,
      obiectieLegalitateSecretar: 'Nelegal: lipsă temei legal.'
    });
    expect(semnatariCompletiDispozitie(d)).toBe(true);
  });

  it('false la refuz cu obiecție goală (doar spații)', () => {
    const d = dispozitie({
      semnatari: [semnatar(RolSemnatarDispozitie.Emitent, 1)],
      contrasemnaturaRefuzata: true,
      obiectieLegalitateSecretar: '   '
    });
    expect(semnatariCompletiDispozitie(d)).toBe(false);
  });

  it('false fără emitent', () => {
    const d = dispozitie({ semnatari: [semnatar(RolSemnatarDispozitie.SecretarContrasemnatura, 2)] });
    expect(semnatariCompletiDispozitie(d)).toBe(false);
  });
});

describe('actiuniPermise — capcane de mirror', () => {
  const adminSecretar = true;
  const admin = true;

  it('poateIncarcaSemnat cere status Semnat, nu doar lipsa fișierului', () => {
    const numerotat = dispozitie({ status: StatusActRedactional.Numerotat, esteSemnat: false });
    expect(actiuniPermise(numerotat, adminSecretar, admin).poateIncarcaSemnat).toBe(false);

    const semnat = dispozitie({ status: StatusActRedactional.Semnat, esteSemnat: false });
    expect(actiuniPermise(semnat, adminSecretar, admin).poateIncarcaSemnat).toBe(true);
  });

  it('poateIncarcaSemnat rămâne permis post-circuit (prima atașare); Înlocuiește/Șterge blocate de latch', () => {
    const semnatInCircuitFaraFisier = dispozitie({
      status: StatusActRedactional.Semnat, esteSemnat: false, aIntratInCircuit: true
    });
    expect(actiuniPermise(semnatInCircuitFaraFisier, adminSecretar, admin).poateIncarcaSemnat).toBe(true);

    const semnatInCircuitCuFisier = dispozitie({
      status: StatusActRedactional.Semnat, esteSemnat: true, aIntratInCircuit: true
    });
    const a = actiuniPermise(semnatInCircuitCuFisier, adminSecretar, admin);
    expect(a.poateInlocuiSemnat).toBe(false);
    expect(a.poateStergeSemnat).toBe(false);
  });

  it('poateDescarcaSemnat depinde de fișier (esteSemnat), nu de status', () => {
    const semnatFaraFisier = dispozitie({ status: StatusActRedactional.Semnat, esteSemnat: false });
    expect(actiuniPermise(semnatFaraFisier, adminSecretar, admin).poateDescarcaSemnat).toBe(false);
  });

  it('poateSterge = true pe invalidat chiar dacă e Semnat + Publicat (precedența „invalidat")', () => {
    const d = dispozitie({
      status: StatusActRedactional.Semnat, estePublicat: true,
      dataInvalidare: '2026-02-01T00:00:00Z'
    });
    expect(actiuniPermise(d, adminSecretar, admin).poateSterge).toBe(true);
  });

  it('poateSterge = false pe semnat ne-invalidat', () => {
    const d = dispozitie({ status: StatusActRedactional.Semnat });
    expect(actiuniPermise(d, adminSecretar, admin).poateSterge).toBe(false);
  });

  it('poateSterge = false când există comunicări (chiar invalidat)', () => {
    const d = dispozitie({ dataInvalidare: '2026-02-01T00:00:00Z', comunicari: [comunicare()] });
    expect(actiuniPermise(d, adminSecretar, admin).poateSterge).toBe(false);
  });

  it('poateSterge cere Admin, nu doar Secretar', () => {
    const draft = dispozitie({ status: StatusActRedactional.Draft });
    expect(actiuniPermise(draft, true, false).poateSterge).toBe(false);  // secretar, nu admin
    expect(actiuniPermise(draft, true, true).poateSterge).toBe(true);
  });
});
